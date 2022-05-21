using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Diagnostics;
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
using SecurityChannel;

namespace AdminProgram.ViewModels
{
    public sealed partial class HostViewModel
    {
        public HostViewModel()
        {
            ScanThreads = new ThreadList { IsDead = true };
            RefreshThreads = new ThreadList { IsDead = true };
            InitializeDb();

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

            ScanThreads.IsDead = false;

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
            RefreshThreads.IsDead = false;

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
            var publicKey = NetHelper.GetPublicKeyOrDefault(client, host.RouteIp, NetHelper.Timeout);
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
        /// <returns>True — если ПК влючился без ошибок, иначе False.</returns>
        private static void PowerOn([CanBeNull] object obj)
        {
            var host = (Host)obj;
            
            if (host is null)
                return;
            
            var client = new UdpClient();
            var magicPacket = NetHelper.GetMagicPacket(host.MacAddress);
            var remoteIp = host.RouteIp;

            try
            {
                // Отправка магического пакета.
                client.Send(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, NetHelper.UdpPort));
                
                // Получение открытого ключа после включения ПК.
                var publicKey = NetHelper.GetPublicKeyOrDefault(client, remoteIp, NetHelper.LoadTimeout);

                if (!publicKey.HasValue)
                {
                    host.Status = HostStatus.Off;
                    return;
                }

                host.GenerateKeys();
                var hostPrivateKey = host.PrivateKey;

                while (!SendOurMacAddress(client, remoteIp, publicKey.Value, hostPrivateKey, host)) { }
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
            var remoteIp = new IPEndPoint(IPAddress.Parse(host.IpAddress), NetHelper.UdpPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(client, remoteIp, NetHelper.Timeout);

            if (!publicKey.HasValue)
            {
                host.Status = HostStatus.On;
                return;
            }
            
            host.GenerateKeys();
            var hostPrivateKey = host.PrivateKey;
            var hostPublicKey = host.PublicKey;

            var command = new ShutdownCommand(null, hostPublicKey);
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(ShutdownCommand), 
                publicKey.Value);
            var bytes = datagram.ToBytes();
            client.Send(bytes, bytes.Length, remoteIp);

            bytes = client.Receive(ref remoteIp);
            datagram = Datagram.FromBytes(bytes);
            var result = CommandResult.FromBytes(datagram.GetData(hostPrivateKey));

            if (result.Status != CommandResultStatus.Successed)
            {
                host.Status = HostStatus.On;
                return;
            }
            
            host.Status = HostStatus.Off;

            // var shutdownProcess = new Process
            // {
            //     StartInfo =
            //     {
            //         FileName = "shutdown.exe",
            //         Arguments = $@"-s -f -m \\{SelectedHost.IpAddress} -t 1",
            //         WindowStyle = ProcessWindowStyle.Hidden,
            //         CreateNoWindow = true
            //     }
            // };
            //
            // try
            // {
            //     return shutdownProcess.Start();
            // }
            // catch (Exception)
            // {
            //     return false;
            // }
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

        private static bool SendOurMacAddress(UdpClient client, IPEndPoint remoteIp, RSAParameters publicKey,
            RSAParameters privateKey, Host host)
        {
            // Отправка мак-адреса нашего ПК.
            var dataBytes = Encoding.Unicode.GetBytes(NetHelper.GetMacAddress());
            var command = new MessageCommand(dataBytes, host.PublicKey) { IsSystem = true };
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(MessageCommand), publicKey);
            var datagramBytes = datagram.ToBytes();
            client.Send(datagramBytes, datagramBytes.Length, remoteIp);
                
            // Проверка получения с помощью получения результата выполнения команды.
            client.Client.ReceiveTimeout = NetHelper.Timeout;
            var data = client.Receive(ref remoteIp);
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