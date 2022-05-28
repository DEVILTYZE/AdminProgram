using System;
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
using System.Windows;
using AdminProgram.Annotations;
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
            ScanThreads = new ThreadList();
            RefreshThreads = new ThreadList();
            _transferThreads = new ThreadList();
            _hasDataBase = InitializeDb();

            // Есть ли доступ в сеть.
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            // Текущий хост.
            _currentHost = Dns.GetHostEntry(Dns.GetHostName());

            // Находим остальные ПК в локальной сети и добавляем их в словарь.
            foreach (var ipAddress in
                     _currentHost.AddressList.Where(thisIp => thisIp.AddressFamily == AddressFamily.InterNetwork))
                CreateMacAddressTable(ipAddress.ToString());
            
            Refresh();
        }

        /// <summary>
        /// Сканирование локальной сети на новые ПК.
        /// </summary>
        /// <returns>True — если сеть просканирована без ошибок, иначе False.</returns>
        public bool Scan()
        {
            if (_currentHost is null)
                return false;
            
            foreach (var ipAddress in _addresses)
            {
                var thread = new Thread(AddHost);
                thread.Start(ipAddress);
                ScanThreads.Add(thread);
            }

            var threadOfThreadList = new Thread(ScanThreads.WaitThreads);
            threadOfThreadList.Start();

            return true;
        }

        /// <summary>
        /// Обновление списка ПК в программе.
        /// </summary>
        public void Refresh()
        {
            foreach (var host in Hosts)
            {
                var thread = new Thread(new ParameterizedThreadStart(Refresh));
                thread.Start(host);
                RefreshThreads.Add(thread);
            }

            var threadOfThreadList = new Thread(RefreshThreads.WaitThreads);
            threadOfThreadList.Start();
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
            var client = new UdpClient();
            var endPoint = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.CommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(client, endPoint, NetHelper.Timeout);
            host.Status = publicKey.HasValue ? HostStatus.On : NetHelper.Ping(host.IpAddress);
        }

        public void PowerOn()
        {
            SelectedHost.Status = HostStatus.Loading;

            var thread = new Thread(new ParameterizedThreadStart(PowerOn));
            thread.Start(SelectedHost);
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
            var magicPacket = NetHelper.GetMagicPacket(host.MacAddress);
            var endPoint = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.CommandPort);

            try
            {
                // Отправка магического пакета.
                var currentIp = CurrentIpAddress.ToString();
                var broadcastIp = currentIp[..currentIp.LastIndexOf('.')] + ".255";
                client.Send(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Parse(broadcastIp), NetHelper.CommandPort));

                // Получение открытого ключа после включения ПК.
                var publicKey = NetHelper.GetPublicKeyOrDefault(client, endPoint, NetHelper.LoadTimeout);

                if (publicKey.HasValue) 
                    return;
                
                host.Status = HostStatus.Off;
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                client.Close();
            }
        }

        public void Shutdown()
        {
            SelectedHost.Status = HostStatus.Loading;
            
            var thread = new Thread(new ParameterizedThreadStart(Shutdown));
            thread.Start(SelectedHost);
        }

        private static void Shutdown([CanBeNull] object obj)
        {
            var host = (Host)obj;

            if (host is null)
                return;
            
            var client = new UdpClient();
            var endPoint = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.CommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(client, endPoint, NetHelper.Timeout);

            if (!publicKey.HasValue)
            {
                host.Status = HostStatus.On;
                return;
            }

            var keys = RsaEngine.GetKeys();
            var command = new ShutdownCommand(null, keys[1]);
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(ShutdownCommand), 
                publicKey.Value);
            var bytes = datagram.ToBytes();
            client.Send(bytes, bytes.Length, endPoint);

            client.Client.ReceiveTimeout = NetHelper.Timeout;
            bytes = client.Receive(ref endPoint);
            datagram = Datagram.FromBytes(bytes);
            var result = CommandResult.FromBytes(datagram.GetData(keys[0]));
            host.Status = result.Status == CommandResultStatus.Successed ? HostStatus.Off : HostStatus.On;
        }

        public bool TransferFiles()
        {
            if (string.IsNullOrEmpty(TransferMessage))
                return false;
            
            var thread = new Thread(TransferFiles);
            thread.Start((SelectedHost, TransferMessage));
            _transferThreads.Add(thread);

            return true;
        }

        private void TransferFiles([CanBeNull] object obj)
        {
            if (obj is null)
                return;
            
            var (host, path) = ((Host, string))obj;
            var endPoint = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.TransferCommandPort);
            var client = new UdpClient();
            var publicKey = NetHelper.GetPublicKeyOrDefault(client, endPoint, NetHelper.Timeout); 
            
            if (!publicKey.HasValue)
                return;

            Thread.Sleep(1000);
            var keys = RsaEngine.GetKeys();
            var command = new TransferCommand(new TransferObject(host.IpAddress, NetHelper.TransferCommandPort, path)
                .ToBytes(), keys[1]);
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(TransferCommand),
                publicKey.Value);
            var bytes = datagram.ToBytes();
            client.Send(bytes, bytes.Length, endPoint);
            
            var thread = new Thread(Transfer);
            thread.Start((new IPEndPoint(endPoint.Address, NetHelper.TransferPort), host, keys[0]));

            client.Client.ReceiveTimeout = NetHelper.Timeout;
            bytes = client.Receive(ref endPoint);
            datagram = Datagram.FromBytes(bytes);
            var result = CommandResult.FromBytes(datagram.GetData(keys[0]));
            host.IsTransfers = result.Status == CommandResultStatus.Successed;
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
                const int maxCountOfTries = 10;
                var currentTry = 0;

                while (currentTry < maxCountOfTries)
                {
                    if (host.TransferServer.Pending())
                    {
                        client = host.TransferServer.AcceptTcpClient();
                        break;
                    }

                    Thread.Sleep(150);
                    ++currentTry;
                }
                
                if (client is null)
                {
                    host.TransferServer.Stop();
                    
                    return;
                }
                
                client.Client.ReceiveTimeout = NetHelper.Timeout;
                var countOfFiles = client.GetStream().ReadByte();
                
                for (var i = 0; i < countOfFiles && host.IsTransfers && _transferThreads.IsAlive; ++i)
                {
                    using var stream = client.GetStream();
                    const int maxLength = 256;
                    var data = new byte[4];
                    stream.Read(data, 0, data.Length);
                    var length = BitConverter.ToInt32(data);
                    var currentByteList = new List<byte>(maxLength);

                    while (length > 0)
                    {
                        data = new byte[length <= maxLength ? length : maxLength];
                        stream.Read(data, 0, data.Length);
                        length -= data.Length;
                        currentByteList.AddRange(data);
                    }
                        
                    var datagram = Datagram.FromBytes(currentByteList.ToArray());
                    data = datagram.GetData(privateKey);
                    length = BitConverter.ToInt32(new ArraySegment<byte>(data, 0, 4));
                    data = new ArraySegment<byte>(data, 4, length).ToArray();
                    namesList.Add(Encoding.Unicode.GetString(data));
                    data = new ArraySegment<byte>(data, length - 1, data.Length - length + 1).ToArray();
                    responseList.Add(data);
                }
            }
            catch (SocketException) { }
            finally
            {
                client?.Close();
                
                if (!host.IsTransfers)
                    host.TransferServer?.Stop();
            }

            for (var i = 0; i < responseList.Count && host.IsTransfers && _transferThreads.IsAlive; ++i)
            {
                var count = 1;
                var path = _filesDirectory + host.Name + namesList[i];
                while (File.Exists(path))
                {
                    if (count == 1)
                        path += $"({count})";
                    else
                        path = path[..path.LastIndexOf('(')] + $"({count})";

                    ++count;
                }
                    
                File.WriteAllBytes(path, responseList[i]);
            }

            host.IsTransfers = false;
        }

        public void CloseTransfer()
        {
            var client = new UdpClient();
            var endPoint = new IPEndPoint(IPAddress.Parse(SelectedHost.IpAddress), NetHelper.TransferCommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(client, endPoint, NetHelper.Timeout);
            var keys = RsaEngine.GetKeys();
            var command = new TransferCommand(null, keys[1]) { Type = CommandType.Abort };
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(TransferCommand), publicKey);
            var bytes = datagram.ToBytes();
            client.Send(bytes, bytes.Length, endPoint);

            bytes = client.Receive(ref endPoint);
            datagram = Datagram.FromBytes(bytes);
            var result = CommandResult.FromBytes(datagram.GetData(keys[0]));
            SelectedHost.IsTransfers = result.Status != CommandResultStatus.Successed;
            SelectedHost.TransferServer.Stop();
        }

        public void WaitAllThreads()
        {
            ScanThreads.WaitThreads();
            RefreshThreads.WaitThreads();
            _transferThreads.WaitThreads();
        }

        public IPEndPoint GetOurIpEndPoint() => new(CurrentIpAddress, NetHelper.CommandPort);

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
            var thread = new Thread(new ParameterizedThreadStart(Refresh));
            thread.Start(host);

            lock (_locker)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
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
                }));
            }
        }

        private void CreateMacAddressTable(string ipAddress)
        {
            _addresses = new Dictionary<string, string>();
            var arpProcess = Process.Start(new ProcessStartInfo("arp", "a -N " + ipAddress)
            {
                    StandardOutputEncoding = Encoding.Unicode,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
            });

            var cmdOutput = arpProcess?.StandardOutput.ReadToEnd();
            
            if (string.IsNullOrEmpty(cmdOutput))
                return;
            
            var pattern = @$"({ipAddress.Split('.')[0]}\."
                          + @"(\d{0,3}\.){2}\d{0,2}[0-4])\s+(([\da-f]{2}-){5}[\da-f][\da-e])";

            foreach (Match match in Regex.Matches(cmdOutput, pattern))
                _addresses.Add(match.Groups[1].Value, match.Groups[3].Value);
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
                return false;
            }

            Hosts = new ObservableCollection<Host>(_db.Hosts.Local.Select(thisHost => new Host(thisHost)));
            OnPropertyChanged(nameof(Hosts));
            
            return true;
        }
    }
}