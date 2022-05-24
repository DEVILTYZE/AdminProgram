using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using AdminProgramHost.Annotations;
using CommandLib.Commands.Helpers;

namespace AdminProgramHost
{
    public partial class Host : INotifyPropertyChanged
    {
        private static readonly string MacDatPath = Environment.CurrentDirectory + "\\mac.apd";
        private static readonly string LogsPath = Environment.CurrentDirectory + "\\logs" + DateTime.Now.ToString(
            "yyyyMMddHHmmss") + ".txt";

        private readonly string _name, _ipAddress, _macAddress;
        private string _mainMacAddress, _logs;
        private Thread _threadReceiveData;
        private RSAParameters _privateKey, _publicKey;
        private UdpClient _client;
        private bool _forceClose;
        private IPEndPoint _endPoint;
        private readonly List<ICommand> _savedCommands;

        public string Name
        {
            get => _name;
            init
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        
        public string IpAddress
        {
            get => _ipAddress;
            init
            {
                _ipAddress = value;
                OnPropertyChanged(nameof(IpAddress));
            }
        }
        
        public string MacAddress
        {
            get => _macAddress;
            init
            {
                _macAddress = value;
                OnPropertyChanged(nameof(MacAddress));
            }
        }

        public string Logs
        {
            get => _logs;
            set
            {
                _logs = value;
                OnPropertyChanged(nameof(Logs));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}