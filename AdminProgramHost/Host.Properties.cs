using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using AdminProgramHost.Annotations;

namespace AdminProgramHost
{
    public partial class Host : INotifyPropertyChanged
    {
        private const int Port = 8001;
        private static readonly string MacDatPath = Environment.SpecialFolder.UserProfile + "\\mac.dat";

        private string _name;
        private string _ipAddress;
        private string _macAddress;
        
        private string _mainMacAddress;
        private Thread _threadReceiveData;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                OnPropertyChanged(nameof(IpAddress));
            }
        }
        
        public string MacAddress
        {
            get => _macAddress;
            set
            {
                _macAddress = value;
                OnPropertyChanged(nameof(MacAddress));
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