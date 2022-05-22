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
            TransferThreads = new ThreadList { IsDead = true };
            ScanThreads = new ThreadList { IsDead = true };
            RefreshThreads = new ThreadList { IsDead = true };
            InitializeDb();
            var hostDb = _db.Hosts.FirstOrDefault();
            var host = hostDb is not null ? new Host(hostDb) : null;
            
            if (host is null || NetHelper.PortIsEnabled(host.EndPoint))
                NetHelper.SetPort(
                    _requestPath,
                    NetHelper.UdpPort,
                    NetHelper.UdpPort,
                    "UDP",
                    "192.168.0.105", 
                    1,
                    "AdminProgram",
                    0
                );
            
            // Есть ли доступ в сеть.
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            // Имя нашего ПК.
            _currentHost = Dns.GetHostEntry(Dns.GetHostName());

            // Находим остальные ПК в локальной сети и добавляем их в словарь.
            foreach (var ipAddress in
                     _currentHost.AddressList.Where(thisIp => thisIp.AddressFamily == AddressFamily.InterNetwork))
                CreateMacAddressTable(ipAddress.ToString());
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
            var host = (Host)obj;
            host.Status = HostStatus.Loading;
            
            if (host is null)
                throw new ArgumentException("Host is null");

            var client = new UdpClient();
            var publicKey = NetHelper.GetPublicKeyOrDefault(client, host.EndPoint, NetHelper.Timeout);
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
        private static void PowerOn([CanBeNull] object obj)
        {
            var host = (Host)obj;
            
            if (host is null)
                return;
            
            var client = new UdpClient();
            var magicPacket = NetHelper.GetMagicPacket(host.MacAddress);
            var endPoint = host.EndPoint;

            try
            {
                // Отправка магического пакета.
                client.Send(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, NetHelper.UdpPort));

                // Получение открытого ключа после включения ПК.
                var publicKey = NetHelper.GetPublicKeyOrDefault(client, endPoint, NetHelper.LoadTimeout);

                if (!publicKey.HasValue)
                {
                    host.Status = HostStatus.Off;
                    return;
                }

                var keys = RsaEngine.GetKeys();

                while (!SendOurMacAddress(client, endPoint, keys[0], keys[1], publicKey.Value))
                {
                }
                // TODO: Сделать так, чтобы клиент смог принять наш мак адрес.
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
            var endPoint = host.EndPoint;
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

            bytes = client.Receive(ref endPoint);
            datagram = Datagram.FromBytes(bytes);
            var result = CommandResult.FromBytes(datagram.GetData(keys[0]));

            if (result.Status != CommandResultStatus.Successed)
            {
                host.Status = HostStatus.On;
                return;
            }
            
            host.Status = HostStatus.Off;
        }

        public void TransferFiles()
        {
            var thread = new Thread(new ParameterizedThreadStart(TransferFiles));
            thread.Start((SelectedHost, string.Empty)); // TODO: Добавить PATH
            TransferThreads.Add(thread);
        }

        private void TransferFiles([CanBeNull] object obj)
        {
            if (obj is null)
                return;
            
            var (host, path) = ((Host, string))obj;
            var endPoint = host.EndPoint;
            var client = new UdpClient();
            var publicKey = NetHelper.GetPublicKeyOrDefault(client, endPoint, NetHelper.Timeout); 
            
            if (!publicKey.HasValue)
                return;

            var keys = RsaEngine.GetKeys();
            var command = new TransferFileCommand(new TransferObject(host.IpAddress, NetHelper.UdpPort, 
                    new RsaKey(keys[1]), path).ToBytes());
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(TransferFileCommand),
                publicKey.Value);
            var bytes = datagram.ToBytes();
            client.Send(bytes, bytes.Length, endPoint);

            var thread = new Thread(Transfer);
            thread.Start((endPoint, host.Name));
        }

        private void Transfer([CanBeNull] object obj)
        {
            if (obj is null)
                return;
            
            var (endPoint, hostName) = ((IPEndPoint, string))obj;
            TcpListener server = null;
            var responseList = new List<List<byte>>();
            var namesList = new List<string>();
            
            try
            {
                server = new TcpListener(endPoint);
                server.Start();
                
                var client = server.AcceptTcpClient();
                var countOfFiles = client.GetStream().ReadByte();
                
                for (var i = 0; i < countOfFiles && !TransferThreads.IsDead; ++i)
                {
                    responseList.Add(new List<byte>(NetHelper.MaxFileLength));
                    
                    using (var stream = client.GetStream())
                    {
                        var data = new byte[4];
                        stream.Read(data, 0, data.Length);
                        var length = BitConverter.ToInt32(data);
                        var name = new StringBuilder();

                        while (length > 0)
                        {
                            data = new byte[length <= 256 ? length : 256];
                            stream.Read(data, 0, data.Length);
                            length -= data.Length;
                            name.Append(Encoding.Unicode.GetString(data));
                        }
                        
                        namesList.Add(name.ToString());
                        data = new byte[4];
                        stream.Read(data, 0, data.Length);
                        length = BitConverter.ToInt32(data);
                    
                        while (length > 0)
                        {
                            data = new byte[length <= 256 ? length : 256];
                            stream.Read(data, 0, data.Length);
                            length -= data.Length;
                            responseList[^1].AddRange(data);
                        }
                        
                    }
                    
                    client.Close();
                }
            }
            catch (SocketException) { }
            finally
            {
                server?.Stop();
            }

            for (var i = 0; i < responseList.Count && !TransferThreads.IsDead; ++i)
            {
                var count = 1;
                var path = _filesDirectory + hostName + namesList[i];
                while (File.Exists(path))
                {
                    if (count == 1)
                        path += $"({count})";
                    else
                        path = path[..path.LastIndexOf('(')] + $"({count})";

                    ++count;
                }
                    
                File.WriteAllBytes(path, responseList[i].ToArray());
            }
        }

        public void WaitAllThreads()
        {
            ScanThreads.WaitThreads();
            RefreshThreads.WaitThreads();
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
            Refresh(host);

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

                        _db.Hosts.Add(new HostDb
                        {
                            Name = host.Name,
                            IpAddress = host.IpAddress,
                            MacAddress = host.MacAddress
                        });
                    }
                    else if (string.CompareOrdinal(host.IpAddress, hostInHosts.IpAddress) != 0 ||
                             string.CompareOrdinal(host.Name, hostInHosts.Name) != 0)
                    {
                        var hostInDb = _db.Hosts.ToList().FirstOrDefault(thisHost =>
                            string.CompareOrdinal(host.MacAddress, thisHost.MacAddress) == 0);

                        if (hostInDb is null)
                            return;

                        hostInDb.Name = hostInHosts.Name = host.Name;
                        hostInDb.IpAddress = hostInHosts.IpAddress = host.IpAddress;
                        _db.Entry(hostInDb).State = EntityState.Modified;
                    }

                    _db.SaveChanges();
                }));
            }
        }

        private void CreateMacAddressTable(string ipAddress)
        {
            _addresses = new Dictionary<string, string>();
            var arpProcess = new Process
            {
                StartInfo =
                {
                    FileName = "arp",
                    Arguments = "-a -N " + ipAddress,
                    StandardOutputEncoding = Encoding.Unicode,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            arpProcess.Start();

            var cmdOutput = arpProcess.StandardOutput.ReadToEnd();
            var pattern = @$"({ipAddress.Split('.')[0]}\."
                          + @"(\d{0,3}\.){2}\d{0,2}[0-4])\s+(([\da-f]{2}-){5}[\da-f][\da-e])";

            foreach (Match match in Regex.Matches(cmdOutput, pattern))
                _addresses.Add(match.Groups[1].Value, match.Groups[3].Value.Replace('-', ':'));
        }

        /// <summary>
        /// Отсылает мак-адрес текущего ПК.
        /// </summary>
        /// <param name="client">Клиент.</param>
        /// <param name="endPoint">Конечная точка доставки нашего мак-адреса.</param>
        /// <param name="publicKeyToOther">Открытый ключ для хоста, куда доставляем мак-адрес.</param>
        /// <param name="publicKey">Открытый ключ, который нам прислал ПК, которому требуется наш мак-адрес.</param>
        /// <param name="privateKey">Закрытый ключ, которым будет расшифровано сообщение результата выполнения команды.</param>
        /// <returns>True, если наш мак-адрес успешно записан у host.</returns>
        private static bool SendOurMacAddress(UdpClient client, IPEndPoint endPoint, RSAParameters privateKey, 
            RSAParameters publicKeyToOther, RSAParameters publicKey)
        {
            // Отправка мак-адреса нашего ПК.
            var dataBytes = Encoding.Unicode.GetBytes(NetHelper.GetMacAddress());
            var command = new MessageCommand(dataBytes, publicKeyToOther) { IsSystem = true };
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(MessageCommand), publicKey);
            var datagramBytes = datagram.ToBytes();
            client.Send(datagramBytes, datagramBytes.Length, endPoint);
                
            // Проверка получения с помощью получения результата выполнения команды.
            client.Client.ReceiveTimeout = NetHelper.Timeout;
            var data = client.Receive(ref endPoint);
            datagram = Datagram.FromBytes(data);
            var result = CommandResult.FromBytes(datagram.GetData(privateKey));
            
            return result.Status == CommandResultStatus.Successed;
        }

        private void InitializeDb()
        {
            try
            {
                _db = new AdminContext();
                _db.Hosts.Load();
            }
            catch (Exception)
            {
                return;
            }

            Hosts = new ObservableCollection<Host>(_db.Hosts.Local.Select(thisHost => new Host(thisHost)));
            OnPropertyChanged(nameof(Hosts));
            Refresh();
        }
    }
}