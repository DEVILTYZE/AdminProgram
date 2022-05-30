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
using System.Threading.Tasks;
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
            ScanTasks = new TaskList();
            RefreshTasks = new TaskList();
            _transferTasks = new TaskList();
            _hasDataBase = InitializeDb();

            // Есть ли доступ в сеть.
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            // Текущий хост.
            _currentHost = Dns.GetHostEntry(Dns.GetHostName());

            // Находим остальные ПК в локальной сети и добавляем их в словарь.
            foreach (var ipAddress in _currentHost.AddressList.Where(NetHelper.IsInLocalNetwork))
                CreateMacAddressTable(ipAddress.ToString());
            
            SetPorts(CurrentIpAddress.ToString());
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
                ScanTasks.Add(Task.Run(() => AddHost(ipAddress)));

            ScanTasks.Wait();

            return true;
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
            ThreadPool.QueueUserWorkItem(Shutdown, SelectedHost);
        }

        private static void Shutdown([CanBeNull] object obj)
        {
            var host = (Host)obj;

            if (host is null)
                return;
            
            var endPoint = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.CommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.Timeout);
            TcpClient client = null;

            try
            {
                
                client = new TcpClient(host.IpAddress, NetHelper.CommandPort) { ReceiveTimeout = NetHelper.Timeout };
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

                    bytes = NetHelper.StreamRead(stream);
                }

                datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData(keys[0]));
                host.Status = result.Status == CommandResultStatus.Successed ? HostStatus.Off : HostStatus.On;
            }
            catch (SocketException)
            {
                // ignored
            }
            finally
            {
                client?.Close();
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
            TcpClient client = null;

            try
            {
                client = new TcpClient(host.IpAddress, NetHelper.TransferCommandPort)
                {
                    ReceiveTimeout = NetHelper.Timeout
                };

                if (!publicKey.HasValue)
                    return;

                var keys = RsaEngine.GetKeys();
                var transferEndPoint = new IPEndPoint(CurrentIpAddress, 13000);
                var command = new TransferCommand(new TransferObject(transferEndPoint.Address.ToString(), 
                        transferEndPoint.Port, path).ToBytes(), keys[1]);
                var datagram = new Datagram(command.ToBytes(), typeof(TransferCommand), publicKey.Value);
                var bytes = datagram.ToBytes();

                using (var stream = client.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                    Task.Run(() => Transfer((transferEndPoint, host, keys[0])));

                    bytes = NetHelper.StreamRead(stream);
                }
                
                datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData(keys[0]));
                host.IsTransfers = result.Status == CommandResultStatus.Successed;
            }
            catch (SocketException)
            {
                // ignored
            }
            finally
            {
                client?.Close();
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
                using var stream = client.GetStream();
                var countOfFiles = stream.ReadByte();

                for (var i = 0; i < countOfFiles && host.IsTransfers; ++i)
                {
                    var data = new byte[4];
                    stream.Read(data, 0, data.Length);
                    var length = BitConverter.ToInt32(data);
                    var currentByteList = new List<byte>(NetHelper.BufferSize);

                    while (length > 0)
                    {
                        data = new byte[length <= NetHelper.BufferSize ? length : NetHelper.BufferSize];
                        stream.Read(data, 0, data.Length);
                        length -= data.Length;
                        currentByteList.AddRange(data);
                    }

                    var datagram = Datagram.FromBytes(currentByteList.ToArray());
                    data = datagram.GetData(privateKey);
                    length = BitConverter.ToInt32(new ArraySegment<byte>(data, 0, 4));
                    namesList.Add(Encoding.UTF8.GetString(new ArraySegment<byte>(data, 4, length)));
                    data = new ArraySegment<byte>(data, length - 1, data.Length - length + 1).ToArray();
                    responseList.Add(data);
                }
            }
            catch (SocketException) { }
            finally
            {
                client?.Close();
                host.TransferServer?.Stop();
            }

            for (var i = 0; i < responseList.Count && host.IsTransfers; ++i)
            {
                var count = 1;
                var path = _filesDirectory + host + namesList[i];

                if (!Directory.Exists(_filesDirectory + host + "\\"))
                    Directory.CreateDirectory(_filesDirectory + host + "\\");
                
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
            var endPoint = new IPEndPoint(IPAddress.Parse(SelectedHost.IpAddress), NetHelper.TransferCommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.Timeout);
            TcpClient client = null;

            try
            {
                client = new TcpClient(SelectedHost.IpAddress, NetHelper.TransferCommandPort)
                {
                    ReceiveTimeout = NetHelper.Timeout
                };
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
            }
            catch (SocketException)
            {
                SelectedHost.IsTransfers = false;
            }
            finally
            {
                client?.Close();
            }
        }

        public void WaitTasks()
        {
            ScanTasks.Wait();
            RefreshTasks.Wait();
            _transferTasks.Wait();
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
            Task.Run(() => Refresh(host));

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
            var arpProcess = Process.Start(new ProcessStartInfo("arp", $"-a -N \"{ipAddress}\"")
            {
                StandardOutputEncoding = Encoding.UTF8,
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
        
        private static void SetPorts(string ipAddress)
        {
            const string udpString = "UDP";
            const string tcpString = "TCP";
            const int countOfRepeat = 5;
            var ports = new[]
            {
                NetHelper.CommandPort, NetHelper.RemoteStreamPort, NetHelper.RemoteControlPort, 
                NetHelper.RemoteCommandPort, NetHelper.TransferPort, NetHelper.TransferCommandPort
            };
            var protocols = new[] { tcpString, udpString, udpString, tcpString, tcpString, tcpString, tcpString };
            
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

            Hosts = new ObservableCollection<Host>(_db.Hosts.Local.Where(thisHost => thisHost.Id == 1)
                .Select(thisHost => new Host(thisHost)));
            OnPropertyChanged(nameof(Hosts));
            
            return true;
        }
    }
}