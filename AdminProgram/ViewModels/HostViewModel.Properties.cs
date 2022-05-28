using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
        private readonly string _filesDirectory = Environment.CurrentDirectory + "\\admin_dir_files\\";
        private readonly string[] _localNetworks = { "192", "172", "10" };
        
        private Dictionary<string, string> _addresses;
        private Host _selectedHost;
        private string _transferMessage;
        private AdminContext _db;
        private readonly bool _hasDataBase;
        private readonly ThreadList _transferThreads;
        
        private IPAddress CurrentIpAddress => _currentHost.AddressList.First(ip => _localNetworks.Any(localNet
            => ip.ToString().StartsWith(localNet)));

        public ObservableCollection<Host> Hosts { get; set; }
        public ThreadList ScanThreads { get; }
        public ThreadList RefreshThreads { get; }

        public Host SelectedHost
        {
            get => _selectedHost;
            set
            {
                _selectedHost = value;
                OnPropertyChanged(nameof(SelectedHost));
            }
        }

        public string TransferMessage
        {
            get => _transferMessage;
            set
            {
                _transferMessage = value;
                OnPropertyChanged(nameof(TransferMessage));
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