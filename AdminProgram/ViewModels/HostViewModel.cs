using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using AdminProgram.Annotations;
using AdminProgram.Helpers;
using AdminProgram.Models;

namespace AdminProgram.ViewModels
{
    public sealed class HostViewModel : INotifyPropertyChanged
    {
        private readonly object _locker = new();
        private readonly IPHostEntry _currentHost;
        private readonly ThreadList[] _threads = new ThreadList[2];
        
        private Dictionary<string, string> _addresses;
        private Host _selectedHost;
        private AdminContext _db;
        private bool _isScanButtonEnabled, _isRefreshButtonEnabled;
        
        public ObservableCollection<Host> Hosts { get; set; }
        
        public bool IsScanButtonEnabled 
        { 
            get => _isScanButtonEnabled;
            set
            {
                _isScanButtonEnabled = value;
                OnPropertyChanged(nameof(IsScanButtonEnabled));
            } 
        }

        public bool IsRefreshButtonEnabled
        {
            get => _isRefreshButtonEnabled;
            set
            {
                _isRefreshButtonEnabled = value;
                OnPropertyChanged(nameof(IsRefreshButtonEnabled));
            }
        }
        
        public Host SelectedHost
        {
            get => _selectedHost;
            set
            {
                _selectedHost = value;
                OnPropertyChanged(nameof(SelectedHost));
            }
        }

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

            _threads[0] = new ThreadList(this);
            
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
            _threads[1] = new ThreadList(this);
            
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
            catch (SocketException)
            {
                return false;
            }

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
            catch (Exception)
            {
                return false;
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            
            var host = new Host(hostEntry.HostName, ip, mac) { Status = NetHelper.Ping(ip) };

            lock (_locker)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Hosts.Any(thisHost => string.CompareOrdinal(host.IpAddress, thisHost.IpAddress) == 0))
                        return;

                    _db.Hosts.Add(new HostDb
                    {
                        Name = host.Name,
                        IpAddress = host.IpAddress,
                        MacAddress = host.MacAddress
                    });
                    _db.SaveChanges();
                    
                    Hosts.Add(host);
                    OnPropertyChanged(nameof(Hosts));
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

        private void WaitThreads(bool isScan) => _threads[isScan ? 0 : 1].WaitAllThreads(isScan);
    }
}