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
            Logs += "Установка стартовой информации\r\n";
            Name = Dns.GetHostName();
            _savedCommands = new List<ICommand>();

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
                IpAddress = endPoint.Address.ToString();

            MacAddress = NetHelper.GetMacAddress();
            RsaEngine.GenerateKeys(out _privateKey, out _publicKey);
            SetPorts(IpAddress);
        }

        public bool StartClientSession()
        {
            if (File.Exists(MacDatPath))
            {
                using var sr = new StreamReader(MacDatPath);
                _mainMacAddress = sr.ReadToEnd();
            }
            else return false;
            
            _forceClose = false;
            _restart = false;
            Logs += "Старт сессии...\r\n";
            
            if (_threadReceiveDataList is not null && _threadReceiveDataList.Count > 0)
                WaitThreads();

            var threadCommand = new Thread(OpenClient);
            var threadTransfer = new Thread(OpenClient);
            var threadRemote = new Thread(OpenClient);
            threadCommand.Start(NetHelper.CommandPort);
            threadTransfer.Start(NetHelper.TransferCommandPort);
            threadRemote.Start(NetHelper.RemoteCommandPort);
            _threadReceiveDataList = new List<Thread>(new[] { threadCommand, threadTransfer, threadRemote });
            _clients = new List<UdpClient>();
            
            return true;
        }

        public void WaitThreads()
        {
            Logs += "Закрытие клиента\r\n";
            _forceClose = true;
            
            foreach(var client in _clients)
                client.Close();

            if (!_restart)
            {
                ExportLogs(Logs);
                return;
            }
            
            ExportLogs(Logs);
            lock(_locker)
                Logs += "Рестарт программы\r\n";

            var path = ProgramName;
            
            if (string.IsNullOrEmpty(path))
                return;
            
            Process.Start(path);
        }

        public bool SetAutorunValue(bool isAutorun)
        {
            Logs += isAutorun ? "Установка автозапуска\r\n" : "Удаление автозапуска\r\n";
            var path = ProgramName;

            if (string.IsNullOrEmpty(path))
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
                    regCurrentUser.SetValue(Name + "Program", path);
                    regLocalMachine.SetValue(Name + "Program", path);
                }
                else
                {
                    regCurrentUser.DeleteValue(Name + "Program");
                    regLocalMachine.DeleteValue(Name + "Program");
                }
            }
            catch
            {
                Logs += isAutorun ? "Автозапуск не установлен\r\n" : "Автозапуск не удалён\r\n";
                return false;
            }

            Logs += isAutorun ? "Автозапуск установлен успешно\r\n" : "Автозапуск удалён успешно\r\n";
            return true;
        }

        /// <summary>
        /// Метод включения клиента в режим прослушки сети.
        /// </summary>
        private void OpenClient(object obj)
        {
            var port = (int)obj;
            var endPoint = GetEndPoint(port);
            var client = new UdpClient(port);
            
            lock (_locker)
                _clients.Add(client);

            try
            {
                while (!_forceClose)
                {
                    lock(_locker)
                        Logs += "Открытие клиента\r\n";
                    
                    // Шаг 1: принимаем данные.
                    var data = client.Receive(ref endPoint);

                    // Шаг 2: декодируем данные в датаграмму.
                    var datagram = Datagram.FromBytes(data);

                    // Шаг 3: получаем из датаграммы команду.
                    var command = AbstractCommand.FromBytes(datagram.GetData(_privateKey), datagram.Type);
                    
                    lock(_locker)
                        Logs += "Получена команда: " + datagram.Type.FullName + "\r\n";

                    // Шаг 4: выполняем команду.
                    var result = CommandProcessing(command);
                    
                    lock(_locker)
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
                    client.Send(resultDatagramBytes, resultDatagramBytes.Length, endPoint);
                    
                    lock(_locker)
                        Logs += "Отправка результата\r\n";
                }
            }
            catch (SocketException)
            {
                lock(_locker)
                {
                    Logs += "Socket exception\r\n";
                    _restart &= !_forceClose;
                }
            }
            catch (ThreadInterruptedException)
            {
                lock(_locker)
                {
                    Logs += "Thread exception";
                    _restart &= !_forceClose;
                }
            }
            catch (Exception)
            {
                lock(_locker)
                    Logs += "Простой exception\r\n";
                
                var result = new CommandResult(CommandResultStatus.Failed, Array.Empty<byte>());
                var datagram = new Datagram(result.ToBytes(), null, typeof(CommandResult));
                var datagramBytes = datagram.ToBytes();

                client.Send(datagramBytes, datagramBytes.Length, endPoint);
            }
            finally
            {
                lock(_locker)
                    Logs += "Закрытие клиента (Finally блок)\r\n";
                
                if (!_forceClose)
                    client.Close();
            }
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
                default:
                    var savedCommand = _savedCommands.FirstOrDefault(thisCommand 
                        => thisCommand.GetType() == command.GetType());
                            
                    if (savedCommand is null)
                        return new CommandResult(CommandResultStatus.Failed, null);

                    savedCommand.Abort();
                    _savedCommands.Remove(savedCommand);
                    
                    return new CommandResult(CommandResultStatus.Successed, null);
            }
        }

        private IPEndPoint GetEndPoint(int port)
        {
            Logs += "Получение конечной точки\r\n";
            var arpProcess = Process.Start(new ProcessStartInfo("arp", $"-a") // | find \"{_mainMacAddress}\"
            {
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            
            var cmdOutput = arpProcess?.StandardOutput.ReadToEnd();

            if (string.IsNullOrEmpty(cmdOutput))
                return new IPEndPoint(IPAddress.Any, port);
            
            const string pattern = @"(\d{0,3}\.){3}\d{0,3}";
            Match match;
            
            do match = Regex.Match(cmdOutput, pattern, RegexOptions.IgnoreCase);
            while (match.Value.EndsWith("255"));
            
            Logs += "Конечная точка — " + match.Value + ":" + port + "\r\n";
            return new IPEndPoint(IPAddress.Parse(match.Value), port);
        }

        private static void SetPorts(string ipAddress)
        {
            const string udpString = "UDP";
            const string tcpString = "TCP";
            const int countOfRepeat = 5;
            var ports = new[]
            {
                NetHelper.CommandPort, NetHelper.RemoteStreamPort, NetHelper.RemoteControlPort, NetHelper.RemoteCommandPort, 
                NetHelper.TransferPort, NetHelper.TransferPort, NetHelper.TransferCommandPort
            };
            var protocols = new[] { udpString, udpString, udpString, udpString, udpString, tcpString, udpString };
            
            for (var i = 0; i < ports.Length; ++i)
            for (var j = 0; j < countOfRepeat; ++j)
                if (NetHelper.SetPort(
                        ports[i],
                        ports[i],
                        protocols[i],
                        ipAddress,
                        1,
                        "AdminProgramHost",
                        0
                    ))
                    break;
        }

        private static void ExportLogs(string logs)
        {
            using var sw = new StreamWriter(LogsPath);
            sw.Write(logs);
        }
    }
}