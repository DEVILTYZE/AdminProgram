using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using CommandLib;
using CommandLib.Commands;
using CommandLib.Commands.Helpers;
using CommandLib.Commands.RemoteCommandItems;
using CommandLib.Commands.TransferCommandItems;
using Microsoft.Win32;
using SecurityChannel;

namespace AdminProgramHost
{
    public sealed partial class Host
    {
        public Host()
        {
            Logs += "Установка стартовой информации.\r\n";
            Name = Dns.GetHostName();
            _savedCommands = new List<ICommand>();

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
                IpAddress = endPoint.Address.ToString();

            MacAddress = NetHelper.GetMacAddress();
            RsaEngine.GenerateKeys(out _privateKey, out _publicKey);
        }

        public bool StartClientSession()
        {
            if (File.Exists(MacDatPath))
            {
                using var sr = new StreamReader(MacDatPath);
                _mainMacAddress = sr.ReadToEnd();
            }
            else return false;
            
            Logs += "Старт сессии...\r\n";
            
            if (_threadReceiveData is not null && _threadReceiveData.IsAlive)
                WaitThread();
            
            _threadReceiveData = new Thread(OpenClient);
            _threadReceiveData.Start();
            
            return true;
        }

        public void WaitThread()
        {
            Logs += "Закрытие клиента.\r\n";
            _forceClose = true;
            _client.Close();
        }

        public bool SetAutorunValue(bool isAutorun)
        {
            Logs += "Установка автозапуска.\r\n";
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            var path = directory.GetFiles("*.exe").FirstOrDefault();

            if (path is null)
                return false;
            
            var regCurrentUser = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run\");
            var regLocalMachine = Registry.LocalMachine.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run\");

            if (regCurrentUser is null || regLocalMachine is null)
                return false;

            try
            {
                if (isAutorun)
                {
                    regCurrentUser.SetValue(Name + "Program", path.FullName);
                    regLocalMachine.SetValue(Name + "Program", path.FullName);
                }
                else
                {
                    regCurrentUser.DeleteValue(Name + "Program");
                    regLocalMachine.DeleteValue(Name + "Program");
                }
            }
            catch
            {
                Logs += "Автозапуск не установлен.\r\n";
                return false;
            }

            Logs += "Автозапуск установлен успешно.\r\n";
            return true;
        }

        /// <summary>
        /// Метод включения клиента в режим прослушки сети.
        /// </summary>
        private void OpenClient()
        {
            _forceClose = false;
            var restart = false;
            _endPoint = GetEndPoint();
            _client = new UdpClient(NetHelper.UdpPort);

            try
            {
                while (!_forceClose)
                {
                    Logs += "Открытие клиента.\r\n";
                    // Шаг 1: принимаем данные.
                    var data = _client.Receive(ref _endPoint);

                    // Шаг 2: декодируем данные в датаграмму.
                    var datagram = Datagram.FromBytes(data);

                    // Шаг 3: получаем из датаграммы команду.
                    var command = AbstractCommand.FromBytes(datagram.GetData(_privateKey), datagram.Type);
                    Logs += "Получена команда: " + datagram.Type.FullName + "\r\n";
                    
                    // Шаг 4: выполняем команду.
                    var result = CommandProcessing(command);
                    Logs += "Результат: " + result.Status + "\r\n";

                    // Шаг 5: обновляем ключи.
                    RsaEngine.GenerateKeys(out _privateKey, out _publicKey);

                    // Шаг 6: добавляем новый публичный ключ к результату.
                    result.PublicKey = new RsaKey(_publicKey);

                    // Шаг 7: формируем новую датаграмму из результата.
                    var resultBytes = result.ToBytes();
                    var resultDatagram = new Datagram(resultBytes, AesEngine.GetKey(), typeof(CommandResult), 
                        command.RsaPublicKey);
                    var resultDatagramBytes = resultDatagram.ToBytes();

                    // Шаг 8: отправляем новую датаграмму.
                    _client.Send(resultDatagramBytes, resultDatagramBytes.Length, _endPoint);
                    Logs += "Отправка результата.\r\n";
                }
            }
            catch (SocketException)
            {
                Logs += "Socket exception.\r\n";
                restart = !_forceClose;
            }
            catch (Exception)
            {
                Logs += "Простой exception.\r\n";
                restart = true;
                var result = new CommandResult(CommandResultStatus.Failed, Array.Empty<byte>());
                var datagram = new Datagram(result.ToBytes(), null, typeof(CommandResult));
                var datagramBytes = datagram.ToBytes();

                _client.Send(datagramBytes, datagramBytes.Length, _endPoint);
            }
            finally
            {
                Logs += "Закрытие клиента. (Finally block.)\r\n";
                if (!_forceClose)
                    _client.Close();

                if (restart)
                {
                    Logs += "Рестарт программы.\r\n";
                    Process.Start(Application.ResourceAssembly.Location);
                }
            }
            
            ExportLogs(Logs);
        }

        private CommandResult CommandProcessing(ICommand command)
        {
            switch (command.Type)
            {
                case CommandType.Execute:
                    var result = command.Execute();
                            
                    if (command is RemoteCommand or TransferCommand)
                        _savedCommands.Add(command);
                            
                    return result;
                case CommandType.Abort:
                    var savedCommand = _savedCommands.FirstOrDefault(thisCommand 
                        => thisCommand.GetType() == command.GetType());
                            
                    if (savedCommand is null)
                        return new CommandResult(CommandResultStatus.Failed, null);

                    savedCommand.Abort();
                    _savedCommands.Remove(savedCommand);
                    
                    return new CommandResult(CommandResultStatus.Successed, null);
                case CommandType.System:
                default:
                    return SetMacAddress(command.Execute());
            }
        }
        
        private CommandResult SetMacAddress(CommandResult commandResult)
        {
            Logs += "Установка мак-адреса.\r\n";
            if (commandResult.Data is null || commandResult.Status == CommandResultStatus.Failed)
                return new CommandResult(CommandResultStatus.Failed, null);

            _mainMacAddress = Encoding.Unicode.GetString(commandResult.Data);
            _endPoint = GetEndPoint();

            using var sw = new StreamWriter(MacDatPath, false);
            sw.Write(_mainMacAddress);

            Logs += "Мак-адрес установлен успешно.\r\n";
            return new CommandResult(CommandResultStatus.Successed, null);
        }

        private IPEndPoint GetEndPoint()
        {
            Logs += "Получение конечной точки.\r\n";
            var arpProcess = Process.Start(new ProcessStartInfo("arp", $"-a") // | find \"{_mainMacAddress}\"
            {
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            
            var cmdOutput = arpProcess?.StandardOutput.ReadToEnd();

            if (string.IsNullOrEmpty(cmdOutput))
                return new IPEndPoint(IPAddress.Any, NetHelper.UdpPort);
            
            const string pattern = @"(\d{0,3}\.){3}\d{0,3}";
            Match match;
            
            do match = Regex.Match(cmdOutput, pattern, RegexOptions.IgnoreCase);
            while (match.Value.EndsWith("255"));
            
            Logs += "Конечная точка — " + match.Value + ":" + NetHelper.UdpPort + "\r\n";
            return new IPEndPoint(IPAddress.Parse(match.Value), NetHelper.UdpPort);
        }

        private static void ExportLogs(string logs)
        {
            using var sw = new StreamWriter(LogsPath);
            sw.Write(logs);
        }
    }
}