﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using AdminProgram.Annotations;
using AdminProgram.Models;

namespace AdminProgram.ViewModels
{
    public partial class HostViewModel : INotifyPropertyChanged
    {
        private readonly object _locker = new();
        private readonly IPHostEntry _currentHost;
        private readonly string _requestPath = Environment.CurrentDirectory + "\\request.txt";
        private readonly string _filesDirectory = Environment.CurrentDirectory + "\\admin_dir_files\\";

        private Dictionary<string, string> _addresses;
        private Host _selectedHost;
        private AdminContext _db;

        public ObservableCollection<Host> Hosts { get; set; }
        public ThreadList ScanThreads { get; }
        public ThreadList RefreshThreads { get; }
        
        public ThreadList TransferThreads { get; }

        public Host SelectedHost
        {
            get => _selectedHost;
            set
            {
                _selectedHost = value;
                OnPropertyChanged(nameof(SelectedHost));
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