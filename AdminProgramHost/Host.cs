using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using CommandLib;
using CommandLib.Commands;
using CommandLib.Commands.Helpers;
using Microsoft.Win32;
using SecurityChannel;

namespace AdminProgramHost
{
    public sealed partial class Host
    {
        public Host()
        {
            SetStartInfo();
        }

        public void StartClientSession()
        {
            Logs += "Старт сессии...\r\n";
            
            if (_threadReceiveData is not null && _threadReceiveData.IsAlive)
                WaitThread();
            
            _threadReceiveData = new Thread(OpenClient);
            _threadReceiveData.Start();
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
            var path = Assembly.GetExecutingAssembly().Location;
            var reg = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run\");

            if (reg is null)
                return false;

            try
            {
                if (isAutorun)
                    reg.SetValue(Name + "Program", path);
                else
                    reg.DeleteValue(Name + "Program");
            }
            catch
            {
                Logs += "Автозапуск не установлен.\r\n";
                return false;
            }

            Logs += "Автозапуск установлен успешно.\r\n";
            return true;
        }

        private void SetStartInfo()
        {
            Logs += "Установка стартовой информации.\r\n";
            Name = Dns.GetHostName();

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
                IpAddress = endPoint.Address.ToString();

            if (File.Exists(MacDatPath))
            {
                using var sr = new StreamReader(MacDatPath);
                _mainMacAddress = sr.ReadToEnd();
            }

            MacAddress = NetHelper.GetMacAddress();
            RsaEngine.GenerateKeys(out _privateKey, out _publicKey);
        }

        /// <summary>
        /// Метод включения клиента в режим прослушки сети.
        /// </summary>
        private void OpenClient()
        {
            _forceClose = false;
            var restart = false;
            _remoteIp = string.IsNullOrEmpty(_mainMacAddress) 
                ? new IPEndPoint(IPAddress.Any, 0) 
                : GetEndPoint();
            _client = new UdpClient(NetHelper.UdpPort);

            try
            {
                while (true)
                {
                    Logs += "Открытие клиента.\r\n";
                    // Шаг 1: принимаем данные.
                    var data = _client.Receive(ref _remoteIp);

                    // Шаг 2: декодируем данные в датаграмму.
                    var datagram = Datagram.FromBytes(data);

                    // Шаг 3: получаем из датаграммы команду.
                    var command = AbstractCommand.FromBytes(datagram.GetData(_privateKey), datagram.Type);
                    Logs += "Получена команда: " + datagram.Type.FullName + "\r\n";
                    
                    // Шаг 4: выполняем команду.
                    var result = command is MessageCommand { IsSystem: true }
                        ? SetMacAddress(command.Execute())
                        : command.Execute();
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
                    _client.Send(resultDatagramBytes, resultDatagramBytes.Length, _remoteIp);
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

                _client.Send(datagramBytes, datagramBytes.Length, _remoteIp);
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

        private CommandResult SetMacAddress(CommandResult commandResult)
        {
            Logs += "Установка мак-адреса.\r\n";
            if (commandResult.Data is null || commandResult.Status == CommandResultStatus.Failed)
                return new CommandResult(CommandResultStatus.Failed, null);

            _mainMacAddress = Encoding.Unicode.GetString(commandResult.Data);
            _remoteIp = GetEndPoint();

            using var sw = new StreamWriter(MacDatPath, false);
            sw.Write(_mainMacAddress);

            Logs += "Мак-адрес установлен успешно.\r\n";
            return new CommandResult(CommandResultStatus.Successed, null);
        }

        private IPEndPoint GetEndPoint()
        {
            Logs += "Получение конечной точки.\r\n";
            var arpProcess = new Process
            {
                StartInfo =
                {
                    FileName = "arp",
                    Arguments = "-a | find \"" + _mainMacAddress + '\"',
                    StandardOutputEncoding = Encoding.Unicode,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            arpProcess.Start();
            
            var cmdOutput = arpProcess.StandardOutput.ReadToEnd();
            const string pattern = @"([1-2][0-5]*\.?){4}";

            var match = Regex.Match(cmdOutput, pattern);
            Logs += "Конечная точка — " + match.Value + ":" + Port + "\r\n";
            return new IPEndPoint(IPAddress.Parse(match.Value), Port);
        }

        private static void ExportLogs(string logs)
        {
            using var sw = new StreamWriter(LogsPath);
            sw.Write(logs);
        }
    }
}