using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using AdminProgram.Annotations;
using AdminProgram.Helpers;
using AdminProgram.Models;
using CommandLib;

namespace AdminProgram.ViewModels
{
    public partial class HostViewModel : INotifyPropertyChanged
    {
        private readonly object _locker = new();
        private readonly IPHostEntry _currentHost;
        private readonly string _filesDirectory = Environment.CurrentDirectory + "\\admin_transferred_files\\";
        
        private Host _selectedHost;
        private string _transferMessage;
        private AdminContext _db;
        private readonly bool _hasDataBase;
        private readonly TaskList _transferTasks;
        private LogViewModel _logModel;

        private IPAddress CurrentIpAddress => _currentHost.AddressList.First(NetHelper.IsInLocalNetwork);

        public ObservableCollection<Host> Hosts { get; set; }
        public TaskList ScanTasks { get; }
        public TaskList RefreshTasks { get; }

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
        
        public LogViewModel LogModel
        {
            get => _logModel;
            set
            {
                _logModel = value;
                OnPropertyChanged(nameof(LogModel));
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