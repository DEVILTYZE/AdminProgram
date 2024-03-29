﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using AdminProgramHost.Annotations;
using CommandLib.Commands.Helpers;

namespace AdminProgramHost
{
    public partial class Host : INotifyPropertyChanged
    {
        private static readonly string LogsPath = Environment.CurrentDirectory + "\\logs\\log" + DateTime.Now.ToString(
            "yyyyMMddHHmmss") + ".txt";

        private readonly object _locker = new();
        private readonly string _name, _ipAddress, _macAddress;
        private string _logs, _adminMacAddress;
        private List<TcpListener> _servers;
        private bool _forceClose, _restart;
        private readonly List<ICommand> _savedCommands;

        private static string ProgramName
        {
            get
            {
                var directory = new DirectoryInfo(Environment.CurrentDirectory);
                return directory.GetFiles("*.exe").FirstOrDefault()?.FullName;
            }
        }

        public bool AreRunningTasks;
        
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