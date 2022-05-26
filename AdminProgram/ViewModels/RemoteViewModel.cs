using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using AdminProgram.Annotations;
using AdminProgram.Models;
using CommandLib;
using CommandLib.Commands.Helpers;
using CommandLib.Commands.RemoteCommandItems;
using SecurityChannel;

namespace AdminProgram.ViewModels
{
    public sealed class RemoteViewModel : INotifyPropertyChanged
    {
        private readonly Host _host;
        private bool _isAliveRemoteConnection;
        private Bitmap _imageScreen;
        private int _height, _width;
        private readonly RemoteControl _currentControlState;
        private readonly IPEndPoint _ourIpEndPoint;
        private readonly RSAParameters _privateKey, _publicKey;
        private ScreenMatrix _screen;
        private Thread _remoteConnectionThread;

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

        public Bitmap ImageScreen
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

        public RemoteControl CurrentControlState
        {
            get => _currentControlState;
            init
            {
                _currentControlState = value;
                OnPropertyChanged(nameof(CurrentControlState));
            }
        }

        public RemoteViewModel(Host host, IPEndPoint ourIpEndPoint) : this()
        {
            Host = host;
            _ourIpEndPoint = ourIpEndPoint;
        }

        public RemoteViewModel()
        {
            CurrentControlState = new RemoteControl();
            _height = _width = 500; // Просто 500.
            RsaEngine.GenerateKeys(out _privateKey, out _publicKey);
        }

        public void StartRemoteConnection()
        {
            IsAliveRemoteConnection = true;
            _remoteConnectionThread = new Thread(Stream);
            _remoteConnectionThread.Start();
        }

        public void CloseRemoteConnection()
        {
            IsAliveRemoteConnection = false;
            var client = new UdpClient();
            var endPoint = new IPEndPoint(IPAddress.Parse(Host.IpAddress), NetHelper.CloseRemotePort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(client, endPoint, NetHelper.Timeout);
            var keys = RsaEngine.GetKeys();
            var command = new RemoteCommand(null, keys[1]) { Type = CommandType.Abort };
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(RemoteCommand), publicKey);
            var bytes = datagram.ToBytes();
            client.Send(bytes, bytes.Length, endPoint);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Stream()
        {
            var remoteIp = new IPEndPoint(IPAddress.Parse(Host.IpAddress), NetHelper.RemotePort);
            var client = new UdpClient(remoteIp.Port);

            try
            {
                RSAParameters? publicKey;

                do
                {
                    publicKey = NetHelper.GetPublicKeyOrDefault(client, remoteIp, NetHelper.Timeout);

                    if (!IsAliveRemoteConnection)
                        return;
                } 
                while (!publicKey.HasValue);

                var remoteObject = new RemoteObject(_ourIpEndPoint.Address.ToString(), _ourIpEndPoint.Port,
                    new RsaKey(_publicKey));
                var command = new RemoteCommand(remoteObject.ToBytes(), _publicKey);
                var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(RemoteCommand),
                    publicKey.Value);
                var datagramBytes = datagram.ToBytes();
                client.Send(datagramBytes, datagramBytes.Length, remoteIp);

                var thread = new Thread(Control);
                thread.Start(publicKey);
                
                while (IsAliveRemoteConnection)
                {
                    var data = client.Receive(ref remoteIp);
                    var countOfBlocks = data[0];
                    var height = BitConverter.ToInt32(data.AsSpan()[^1..5]);
                    var width = BitConverter.ToInt32(data.AsSpan()[^5..9]);
                    data = data[^9..]; // 1 — количество блоков, 4 — длина, 4 — ширина.

                    for (var i = 0; i < countOfBlocks - 1; ++i)
                        data = data.Concat(client.Receive(ref remoteIp)).ToArray();

                    datagram = Datagram.FromBytes(data);
                    data = datagram.GetData(_privateKey);
                    var pixels = ScreenMatrix.GetPixelsFromBytesOrDefault(data);

                    _screen ??= new ScreenMatrix(height, width);
                    _screen.UpdateScreen(pixels, ImageScreen);
                }
            }
            catch (SocketException)
            {
            }
            finally
            {
                client.Close();
            }
        }

        private void Control(object obj)
        {
            var publicKey = (RSAParameters)obj;
            var remoteIp = new IPEndPoint(IPAddress.Parse(Host.IpAddress), NetHelper.RemoteControlPort);
            var client = new UdpClient(remoteIp);

            try
            {
                while (IsAliveRemoteConnection)
                {
                    var data = CurrentControlState.ToBytes();
                    var datagram = new Datagram(data, AesEngine.GetKey(), typeof(RemoteControl), publicKey);
                    var bytes = datagram.ToBytes();
                    client.Send(bytes, bytes.Length, remoteIp);
                    CurrentControlState.ToStartState();
                }
            }
            catch (SocketException)
            {
            }
            finally
            {
                client.Close();
            }
        }
    }
}