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
        
        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}