﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AdminProgram.Annotations;
using AdminProgram.Helpers;
using AdminProgram.Models;
using CommandLib;
using CommandLib.Commands;
using CommandLib.Commands.Helpers;
using CommandLib.Commands.TransferCommandItems;
using SecurityChannel;

namespace AdminProgram.ViewModels
{
    public sealed partial class HostViewModel
    {
        public HostViewModel()
        {
            LogModel = new LogViewModel();
            ScanTasks = new TaskList();
            RefreshTasks = new TaskList();
            _transferTasks = new TaskList();
            _hasDataBase = InitializeDb();
            
            // Есть ли доступ в сеть.
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            // Текущий хост.
            _currentHost = Dns.GetHostEntry(Dns.GetHostName());

            SetPorts(CurrentIpAddress.ToString());
            Scan();
            Refresh();
            LogModel.AddLog("Запуск программы.", LogStatus.Info);
        }

        public void AddHost([NotNull] Host host)
        {
            var hostInHosts = Hosts.FirstOrDefault(thisHost =>
                string.CompareOrdinal(host.MacAddress, thisHost.MacAddress) == 0);

            if (hostInHosts is null)
            {
                Hosts.Add(host);
                OnPropertyChanged(nameof(Hosts));

                if (_hasDataBase)
                    _db.Hosts.Add(new HostDb
                    {
                        Name = host.Name,
                        IpAddress = host.IpAddress,
                        MacAddress = host.MacAddress
                    });
            }
            else if ((string.CompareOrdinal(host.IpAddress, hostInHosts.IpAddress) != 0 ||
                      string.CompareOrdinal(host.Name, hostInHosts.Name) != 0) && _hasDataBase)
            {
                var hostInDb = _db.Hosts.ToList().FirstOrDefault(thisHost =>
                    string.CompareOrdinal(host.MacAddress, thisHost.MacAddress) == 0);

                if (hostInDb is null)
                    return;

                hostInDb.Name = hostInHosts.Name = host.Name;
                hostInDb.IpAddress = hostInHosts.IpAddress = host.IpAddress;
                _db.Entry(hostInDb).State = EntityState.Modified;
            }

            if (_hasDataBase)
                _db.SaveChanges();

            lock (LogModel.Locker)
                LogModel.AddLog($"Добавлен {host.IpAddress}.", LogStatus.Info);
        }

        public void RemoveHost([NotNull] Host host)
        {
            if (!Hosts.Contains(host))
                return;

            Hosts.Remove(host);
            var hostInDb = _db.Hosts.ToList().FirstOrDefault(thisHost 
                => string.CompareOrdinal(thisHost.MacAddress, host.MacAddress) == 0);

            if (hostInDb is null) 
                return;
            
            _db.Hosts.Remove(hostInDb);
            _db.SaveChanges();

            lock (LogModel.Locker)
                LogModel.AddLog($"Удалён {host.IpAddress}.", LogStatus.Info);
        }

        /// <summary>
        /// Сканирование локальной сети на новые ПК.
        /// </summary>
        /// <returns>True — если сеть просканирована без ошибок, иначе False.</returns>
        public bool Scan()
        {
            if (_currentHost is null)
                return false;

            var addresses = GetMacAddressTable();
            
            foreach (var ipAddress in addresses)
                ScanTasks.Add(Task.Run(() => AddHost(ipAddress)));

            ScanTasks.Wait();

            return true;
        }
        
        private void AddHost([NotNull] object obj)
        {
            var (ip, mac) = (KeyValuePair<string, string>)obj;
            IPHostEntry hostEntry;

            try
            {
                hostEntry = Dns.GetHostEntry(ip);
            }
            catch (SocketException)
            {
                return;
            }

            var host = new Host(hostEntry.HostName, ip, mac);
            Task.Run(() => Refresh(host));

            lock (_locker)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => { AddHost(host); }));
            }
        }

        /// <summary>
        /// Обновление списка ПК в программе.
        /// </summary>
        public void Refresh()
        {
            foreach (var host in Hosts)
                RefreshTasks.Add(Task.Run(() => Refresh(host)));

            RefreshTasks.Wait();
        }

        /// <summary>
        /// Обновление статуса выбранного ПК в программе.
        /// </summary>
        public static void Refresh([NotNull] object obj)
        {
            if (obj is null)
                throw new ArgumentException("Host is null");

            var host = (Host)obj;
            host.Status = HostStatus.Loading;
            var endPoint = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.CommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.Timeout);
            host.Status = publicKey.HasValue ? HostStatus.On : NetHelper.Ping(host.IpAddress);
        }

        public void PowerOn()
        {
            SelectedHost.Status = HostStatus.Loading;
            ThreadPool.QueueUserWorkItem(PowerOn, SelectedHost);
        }
        
        /// <summary>
        /// Удалённое включение ПК.
        /// </summary>
        private void PowerOn([CanBeNull] object obj)
        {
            var host = (Host)obj;
            
            if (host is null)
                return;
            
            var client = new UdpClient();
            client.EnableBroadcast = true;
            var magicPacket = NetHelper.GetMagicPacket(host.MacAddress);
            var endPoint = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.CommandPort);
            var currentIp = CurrentIpAddress.ToString();
            var broadcastIp = currentIp[..currentIp.LastIndexOf('.')] + ".255";

            try
            {
                // Отправка магического пакета.
                client.Send(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Parse(broadcastIp), NetHelper.CommandPort));

                // Получение открытого ключа после включения ПК.
                var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.LoadTimeout);

                if (publicKey.HasValue)
                {
                    lock (LogModel.Locker)
                        LogModel.AddLog($"Включение {host.IpAddress}.", LogStatus.Send);
                    return;
                }
                
                lock (LogModel.Locker)
                    LogModel.AddLog($"{host.IpAddress} не включился.", LogStatus.Error);
                host.Status = HostStatus.Off;
            }
            catch (Exception)
            {
                lock (LogModel.Locker)
                    LogModel.AddLog($"При включении {host.IpAddress} получена ошибка сокета.", LogStatus.Error);
            }
            finally
            {
                client.Close();
            }
        }

        public void Shutdown()
        {
            SelectedHost.Status = HostStatus.Loading;
            ThreadPool.QueueUserWorkItem(Shutdown, SelectedHost);
        }

        private void Shutdown([CanBeNull] object obj)
        {
            var host = (Host)obj;

            if (host is null)
                return;
            
            var endPoint = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.CommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.Timeout);
            var client = new TcpClient();

            try
            {
                if (!NetHelper.Connect(ref client, host.IpAddress, NetHelper.CommandPort, NetHelper.Timeout, 5))
                    return;
                
                client.ReceiveTimeout = NetHelper.Timeout;
                
                if (!publicKey.HasValue)
                {
                    host.Status = HostStatus.On;
                    return;
                }

                var keys = RsaEngine.GetKeys();
                var command = new ShutdownCommand(null, keys[1]);
                var datagram = new Datagram(command.ToBytes(), typeof(ShutdownCommand), publicKey.Value);
                var bytes = datagram.ToBytes();

                using (var stream = client.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                Refresh(host);
                
                lock (LogModel.Locker)
                {
                    if (host.Status == HostStatus.Off)
                        LogModel.AddLog($"{host.IpAddress} не выключился.", LogStatus.Error);
                    else
                        LogModel.AddLog($"{host.IpAddress} успешно выключен.", LogStatus.Info);
                }
            }
            catch (SocketException)
            {
                lock (LogModel.Locker)
                    LogModel.AddLog($"При выключении {host.IpAddress} получена ошибка сокета.", LogStatus.Error);
            }
            finally
            {
                if (client is not null && client.Connected)
                    client.Close();
            }
        }

        public bool TransferFiles()
        {
            if (string.IsNullOrEmpty(TransferMessage))
                return false;

            _transferTasks.Add(Task.Run(() => TransferFiles((SelectedHost, TransferMessage))));

            return true;
        }

        private void TransferFiles([CanBeNull] object obj)
        {
            if (obj is null)
                return;
            
            var (host, path) = ((Host, string))obj;
            host.IsTransfers = true;
            var endPoint = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.TransferCommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.Timeout);
            var client = new TcpClient();

            try
            {
                if (!NetHelper.Connect(ref client, host.IpAddress, NetHelper.TransferCommandPort, NetHelper.Timeout, 5))
                    return;
                
                client.ReceiveTimeout = NetHelper.Timeout;

                if (!publicKey.HasValue)
                {
                    lock (LogModel.Locker)
                        LogModel.AddLog($"Передача файлов, {host.IpAddress}, не был получен открытый ключ.", 
                            LogStatus.Error);
                    
                    return;
                }

                var keys = RsaEngine.GetKeys();
                var transferEndPoint = new IPEndPoint(CurrentIpAddress, NetHelper.TransferPort);
                var command = new TransferCommand(new TransferObject(transferEndPoint.Address.ToString(), 
                        transferEndPoint.Port, path).ToBytes(), keys[1]);
                var datagram = new Datagram(command.ToBytes(), typeof(TransferCommand), publicKey.Value);
                var bytes = datagram.ToBytes();
                Task.Run(() => Transfer((transferEndPoint, host, keys[0])));

                using (var stream = client.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);

                    bytes = NetHelper.StreamRead(stream);
                }
                
                datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData(keys[0]));
                
                lock (LogModel.Locker)
                {
                    if (result.Status == CommandResultStatus.Failed)
                    {
                        LogModel.AddLog($"Передача файлов, {host.IpAddress}, хост прислал неуспешный статус.",
                            LogStatus.Error);
                        host.IsTransfers = false;
                    }
                    else
                        LogModel.AddLog($"Передача файлов, {host.IpAddress}, хост прислал успешный статус.",
                            LogStatus.Receive);
                }
                // TODO: доделать логи...
            }
            catch (SocketException)
            {
                lock (LogModel.Locker)
                    LogModel.AddLog($"При передаче файла {host.IpAddress} получена ошибка сокета.", LogStatus.Error);
            }
            finally
            {
                if (client is not null && client.Connected)
                    client.Close();
            }
        }

        private void Transfer([CanBeNull] object obj)
        {
            if (obj is null)
                return;

            var (endPoint, host, privateKey) = ((IPEndPoint, Host, RSAParameters))obj;
            var responseList = new List<byte[]>();
            var namesList = new List<string>();
            TcpClient client = null;

            try
            {
                host.TransferServer = new TcpListener(endPoint);
                host.TransferServer.Start();

                client = host.TransferServer.AcceptTcpClient();
                client.Client.ReceiveTimeout = NetHelper.Timeout;
                var mainLength = 0;
                using var stream = client.GetStream();
                var countOfFiles = stream.ReadByte();

                for (var i = 0; i < countOfFiles && host.IsTransfers; ++i)
                {
                    var data = new byte[4];
                    stream.Read(data, 0, data.Length);
                    var length = BitConverter.ToInt32(data);
                    mainLength += length;
                    var currentByteList = new List<byte>(NetHelper.BufferSize);

                    if (mainLength > NetHelper.MaxFileLength)
                    {
                        host.IsTransfers = false;
                        
                        lock (LogModel.Locker)
                            LogModel.AddLog($"{host.IpAddress} передал файлы общим весом больше, чем возможно.", LogStatus.Error);
                        
                        return;
                    }

                    while (length > 0)
                    {
                        data = new byte[length <= NetHelper.BufferSize ? length : NetHelper.BufferSize];
                        stream.Read(data, 0, data.Length);
                        length -= data.Length;
                        currentByteList.AddRange(data);
                    }

                    var datagram = Datagram.FromBytes(currentByteList.ToArray());
                    data = datagram.GetData(privateKey);
                    length = BitConverter.ToInt32(data.AsSpan()[..4]);
                    namesList.Add(Encoding.UTF8.GetString(data.AsSpan()[4..(length + 4)]));
                    data = data.AsSpan()[(length + 4)..].ToArray();
                    responseList.Add(data);
                    
                    lock (LogModel.Locker)
                        LogModel.AddLog($"{host.IpAddress} передал {i}-й из {countOfFiles} файлов.", LogStatus.Receive);
                }
            }
            catch (SocketException)
            {
                lock (LogModel.Locker)
                    LogModel.AddLog($"При передаче файла {host.IpAddress} получена ошибка сокета.", LogStatus.Error);
            }
            finally
            {
                client?.Close();
                host.TransferServer?.Stop();
            }

            if (responseList.Sum(bytes => bytes.Length) > NetHelper.MaxFileLength)
            {
                host.IsTransfers = false;
                
                lock (LogModel.Locker)
                    LogModel.AddLog($"{host.IpAddress} передал файлы общим весом больше, чем возможно.", LogStatus.Error);
                
                return;
            }
            
            for (var i = 0; i < responseList.Count && host.IsTransfers; ++i)
            {
                var count = 1;
                var path = _filesDirectory + host + "\\";
                var extension = Path.GetExtension(namesList[i]);
                var fullName = path + Path.GetFileNameWithoutExtension(namesList[i]);

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                path = fullName + extension;
                
                while (File.Exists(path))
                {
                    path = fullName + $"({count})" + extension;
                    ++count;
                }
                
                File.WriteAllBytes(path, responseList[i]);
                
                lock (LogModel.Locker)
                    LogModel.AddLog($"Новый файл от {host.IpAddress} по пути \"{path}\".", LogStatus.Info);
            }

            host.IsTransfers = false;
        }

        public void CloseTransfer()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(SelectedHost.IpAddress), NetHelper.TransferCommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.Timeout);
            var client = new TcpClient();
            var host = SelectedHost;

            try
            {
                if (!NetHelper.Connect(ref client, host.IpAddress, NetHelper.TransferCommandPort, NetHelper.Timeout, 3))
                    return;
                
                client.ReceiveTimeout = NetHelper.Timeout;
                var keys = RsaEngine.GetKeys();
                var command = new TransferCommand(null, keys[1]) { Type = CommandType.Abort };
                var datagram = new Datagram(command.ToBytes(), typeof(TransferCommand), publicKey);
                var bytes = datagram.ToBytes();

                using (var stream = client.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);

                    bytes = NetHelper.StreamRead(stream);
                }
                
                datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData(keys[0]));
                SelectedHost.IsTransfers = result.Status != CommandResultStatus.Successed;
                SelectedHost.TransferServer?.Stop();
                
                lock (LogModel.Locker)
                    LogModel.AddLog($"Передача файлов от {host.IpAddress} успешно отменена.", LogStatus.Info);
            }
            catch (SocketException)
            {
                SelectedHost.IsTransfers = false;
                
                lock (LogModel.Locker)
                    LogModel.AddLog($"При передаче файла {host.IpAddress} получена ошибка сокета.", LogStatus.Error);
            }
            finally
            {
                if (client is not null && client.Connected)
                    client.Close();
            }
        }

        public void WaitTasks()
        {
            ScanTasks.Wait();
            RefreshTasks.Wait();
            _transferTasks.Wait();
        }

        public IPEndPoint GetOurIpEndPoint() => new(CurrentIpAddress, NetHelper.CommandPort);

        private Dictionary<string, string> GetMacAddressTable()
        {
            return (from ip in _currentHost.AddressList.Where(NetHelper.IsInLocalNetwork)
                select ip.ToString() 
                into ipAddress 
                let arpProcess = Process.Start(new ProcessStartInfo("arp", $"-a -N \"{ipAddress}\"")
                {
                    StandardOutputEncoding = Encoding.UTF8, 
                    UseShellExecute = false, 
                    RedirectStandardOutput = true, 
                    CreateNoWindow = true
                }) 
                let cmdOutput = arpProcess?.StandardOutput.ReadToEnd() 
                where !string.IsNullOrEmpty(cmdOutput) 
                let pattern = @$"({ipAddress.Split('.')[0]}\." 
                              + @"(\d{0,3}\.){2}\d{0,2}[0-4])\s+(([\da-f]{2}-){5}[\da-f][\da-e])" 
                from Match match in Regex.Matches(cmdOutput, pattern) 
                select match).ToDictionary(match => match.Groups[1].Value, match => match.Groups[3].Value);
        }
        
        private void SetPorts(string ipAddress)
        {
            const string udpString = "UDP";
            const string tcpString = "TCP";
            const int countOfRepeat = 5;
            var currentPort = 0;
            var ports = new[]
            {
                NetHelper.CommandPort, NetHelper.RemoteStreamPort, NetHelper.RemoteControlPort, 
                NetHelper.RemoteCommandPort, NetHelper.TransferPort, NetHelper.TransferCommandPort, NetHelper.KeysPort
            };
            var protocols = new[]
            {
                tcpString, udpString, udpString, tcpString, tcpString, tcpString, tcpString, tcpString
            };

            try
            {
                for (var i = 0; i < ports.Length; ++i)
                for (var j = 0; j < countOfRepeat; ++j)
                {
                    currentPort = ports[i];
                    
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
            }
            catch (Exception)
            {
                lock (LogModel.Locker)
                    LogModel.AddLog($"Порт {currentPort} не удалось открыть.", LogStatus.Error);
            }
        }

        private bool InitializeDb()
        {
            try
            {
                _db = new AdminContext();
                _db.Hosts.Load();
            }
            catch (Exception)
            {
                Hosts = new ObservableCollection<Host>();
                return false;
            }

            Hosts = new ObservableCollection<Host>(_db.Hosts.Local.Select(thisHost => new Host(thisHost)));
            OnPropertyChanged(nameof(Hosts));
            
            return true;
        }
    }
}