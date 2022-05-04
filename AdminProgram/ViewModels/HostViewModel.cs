using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using AdminProgram.Annotations;
using AdminProgram.Helpers;
using AdminProgram.Models;

namespace AdminProgram.ViewModels
{
    public sealed partial class HostViewModel
    {
        public HostViewModel()
        {
            InitializeDb();
            
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            _currentHost = Dns.GetHostEntry(Dns.GetHostName());
            
            foreach (var ipAddress in _currentHost.AddressList.Where(thisIp => thisIp.AddressFamily == AddressFamily.InterNetwork))
                CreateMacAddresses(ipAddress.ToString());
        }

        public bool Scan()
        {
            if (_currentHost is null)
                return false;

            IsScanButtonEnabled = false;
            _threads[0] = new ThreadList();
            
            foreach (var ipAddress in _addresses)
            {
                var thread = new Thread(AddHost);
                thread.Start(ipAddress);
                _threads[0].Add(thread);
            }
            
            WaitThreads(true);

            return true;
        }

        public void Refresh()
        {
            IsRefreshButtonEnabled = false;
            _threads[1] = new ThreadList();
            
            foreach (var host in Hosts)
            {
                var thread = new Thread(new ParameterizedThreadStart(Refresh));
                thread.Start(host);
                _threads[1].Add(thread);
            }
            
            WaitThreads(false);
        }

        public static void Refresh([NotNull] object obj)
        {
            var host = (Host)obj;

            if (host is null)
                throw new ArgumentException("Host is null");

            host.Status = NetHelper.Ping(host.IpAddress);
        }

        public bool PowerOn()
        {
            _selectedHost.Status = HostStatus.Loading;
            var client = new UdpClient();
            var magicPacket = NetHelper.GetMagicPacket(_selectedHost.MacAddress);
            
            try
            {
                client.Send(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, NetHelper.Port));
            }
            catch (SocketException) { return false; }

            return true;
        }

        public bool Shutdown()
        {
            _selectedHost.Status = HostStatus.Off;
            var shutdownProcess = new Process
            {
                StartInfo =
                {
                    FileName = "shutdown.exe",
                    Arguments = $@"-s -f -m \\{_selectedHost.IpAddress} -t 1",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                }
            };

            try
            {
                return shutdownProcess.Start();
            }
            catch (Exception) { return false; }
        }

        private void AddHost([NotNull] object obj)
        {
            var (ip, mac) = (KeyValuePair<string, string>)obj;
            IPHostEntry hostEntry;

            try
            {
                hostEntry = Dns.GetHostEntry(ip);
            }
            catch (SocketException) { return; }
            
            var host = new Host(hostEntry.HostName, ip, mac) { Status = NetHelper.Ping(ip) };

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
        
        private void CreateMacAddresses(string ipAddress)
        {
            _addresses = new Dictionary<string, string>();
            var arpProcess = new Process
            {
                StartInfo =
                {
                    FileName = "arp",
                    Arguments = "-a -N " + ipAddress,
                    StandardOutputEncoding = Encoding.UTF8,
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

        private void InitializeDb()
        {
            try
            {
                _db = new AdminContext();
                _db.Hosts.Load();
            }
            catch (Exception) { return; }

            Hosts = new ObservableCollection<Host>(_db.Hosts.Local.Select(thisHost => new Host(thisHost)));
            OnPropertyChanged(nameof(Hosts));
            Refresh();
        }

        private void WaitThreads(bool isScan)
        {
            var thread = new Thread(WaitThreads);
            thread.Start(isScan);
        }

        private void WaitThreads([CanBeNull] object obj)
        {
            if (obj is null)
                return;

            var isScan = (bool)obj;
            _threads[isScan ? 0 : 1].WaitThreads();
            
            if (isScan) 
                IsScanButtonEnabled = true;
            else 
                IsRefreshButtonEnabled = true;
        }
    }
}