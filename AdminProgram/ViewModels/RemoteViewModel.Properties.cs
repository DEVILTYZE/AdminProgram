using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using AdminProgram.Annotations;
using AdminProgram.Models;
using CommandLib.Commands.RemoteCommandItems;

namespace AdminProgram.ViewModels
{
    public partial class RemoteViewModel : INotifyPropertyChanged
    {
        private readonly Host _host;
        private readonly RemoteControlObject _currentControlState;
        private readonly IPEndPoint _ourIpEndPoint;

        private bool _reconnect, _isAliveRemoteConnection;
        private UdpClient _udpClient;
        private BitmapImage _imageScreen;
        private int _height, _width;
        private RSAParameters[] _keys;

        public readonly string ImageSourcePath = Environment.CurrentDirectory + "\\data\\src.jpg";

        public Host Host
        {
            get => _host;
            init
            {
                _host = value;
                OnPropertyChanged(nameof(Host));
            }
        }

        public bool IsAliveRemoteConnection
        {
            get => _isAliveRemoteConnection;
            set
            {
                _isAliveRemoteConnection = value;
                OnPropertyChanged(nameof(IsAliveRemoteConnection));
            }
        }

        public BitmapImage ImageScreen
        {
            get => _imageScreen;
            set
            {
                _imageScreen = value;
                OnPropertyChanged(nameof(ImageScreen));
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged(nameof(Height));
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged(nameof(Width));
            }
        }

        public RemoteControlObject CurrentControlState
        {
            get => _currentControlState;
            init
            {
                _currentControlState = value;
                OnPropertyChanged(nameof(CurrentControlState));
            }
        }

        public bool Reconnect
        {
            get => _reconnect;
            set
            {
                _reconnect = value;
                OnPropertyChanged(nameof(Reconnect));
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