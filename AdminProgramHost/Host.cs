using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        }

        public void StartClientSession()
        {
            if (File.Exists(MacDatPath))
            {
                using var sr = new StreamReader(MacDatPath);
                _adminMacAddress = sr.ReadToEnd();
            }
            else return;
            
            AreRunningTasks = true;
            _forceClose = false;
            _restart = false;
            Logs += "Старт сессии...\r\n";

            _servers = new List<TcpListener>();
            Task.Run(() => OpenClient(NetHelper.CommandPort));
            Task.Run(() => OpenClient(NetHelper.TransferCommandPort));
            Task.Run(() => OpenClient(NetHelper.RemoteCommandPort));
        }

        public void WaitTasks()
        {
            if (_forceClose)
                return;
            
            Logs += "Закрытие клиента\r\n";
            _forceClose = true;
            
            if (_servers is not null)
                foreach(var server in _servers)
                    server.Stop();

            foreach (var command in _savedCommands)
                command.Abort();

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
            var endPoint = new IPEndPoint(IPAddress.Parse(IpAddress), port);
            TcpListener server = null;
            RsaEngine.GenerateKeys(out var privateKey, out var publicKey);
            
            try
            {
                server = new TcpListener(endPoint);
                
                lock (_locker)
                    _servers.Add(server);
                
                server.Start();
                
                while (!_forceClose)
                {
                    var client = server.AcceptTcpClient();
                    
                    lock(_locker)
                        Logs += "Открытие клиента\r\n";
                    
                    // Шаг 1: принимаем данные.
                    using var stream = client.GetStream();
                    var data = NetHelper.StreamRead(stream);
                    
                    // Шаг 2: декодируем данные в датаграмму.
                    var datagram = Datagram.FromBytes(data);

                    // Шаг 3: получаем из датаграммы команду.
                    var command = AbstractCommand.FromBytes(datagram.GetData(privateKey), datagram.Type);
                    
                    lock(_locker)
                        Logs += "Получена команда: " + datagram.Type.FullName + "\r\n";

                    // Шаг 4: выполняем команду.
                    var result = CommandProcessing(command);
                    
                    lock(_locker)
                        Logs += "Результат: " + result.Status + "\r\n";

                    // Шаг 5: обновляем ключи.
                    RsaEngine.GenerateKeys(out privateKey, out publicKey);

                    // Шаг 6: добавляем новый публичный ключ к результату.
                    result.PublicKey = new RsaKey(publicKey);

                    // Шаг 7: формируем новую датаграмму из результата.
                    var resultBytes = result.ToBytes();
                    var resultDatagram = new Datagram(resultBytes, typeof(CommandResult), command.RsaPublicKey);
                    var resultDatagramBytes = resultDatagram.ToBytes();

                    // Шаг 8: отправляем новую датаграмму.
                    stream.Write(resultDatagramBytes, 0, resultDatagramBytes.Length);

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
            catch (Exception)
            {
                lock(_locker)
                    Logs += "Простой exception\r\n";
            }
            finally
            {
                lock(_locker)
                    Logs += "Закрытие клиента (Finally блок)\r\n";
                
                if (!_forceClose)
                    server?.Stop();
            }

            AreRunningTasks = false;
        }

        private CommandResult CommandProcessing(ICommand command)
        {
            switch (command.Type)
            {
                case CommandType.Execute:
                    var result = command.Execute();
                    AddCommand(command);
                            
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

        private void AddCommand(ICommand command)
        {
            var types = new[] { typeof(RemoteCommand), typeof(TransferCommand) };

            foreach (var type in types)
                if (command.GetType().IsEquivalentTo(type))
                {
                    for (var i = 0; i < _savedCommands.Count; ++i)
                        if (command.GetType().IsEquivalentTo(_savedCommands[i].GetType()))
                        {
                            _savedCommands[i].Abort();
                            _savedCommands[i] = command;
                            return;
                        }
                    
                    _savedCommands.Add(command);
                }
        }

        private IPEndPoint GetEndPoint(int port)
        {
            lock (_locker)
                Logs += "Получение конечной точки\r\n";
            
            var arpProcess = Process.Start(new ProcessStartInfo("arp", "-a")
            {
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            
            var cmdOutput = arpProcess?.StandardOutput.ReadToEnd();

            if (string.IsNullOrEmpty(cmdOutput))
                return new IPEndPoint(IPAddress.Any, port);
            
            var pattern = @"(\d{0,3}\.){3}\d{0,3}\.+" + _adminMacAddress;
            Match match;
            
            do match = Regex.Match(cmdOutput, pattern, RegexOptions.IgnoreCase);
            while (match.Value.EndsWith("255"));

            var ip = string.IsNullOrEmpty(match.Value) ? IpAddress : match.Value;

            lock (_locker)
                Logs += "Конечная точка — " + ip + ":" + port + "\r\n";
            
            return new IPEndPoint(IPAddress.Parse(ip), port);
        }

        private static void ExportLogs(string logs)
        {
            var dirName = new FileInfo(LogsPath).DirectoryName ?? Environment.CurrentDirectory + "\\logs";

            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);
            
            using var sw = new StreamWriter(LogsPath);
            sw.Write(logs);
        }
    }
}