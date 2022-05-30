using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
        private RSAParameters[] _keys;
        private ScreenMatrix _screen;

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
        }

        public void StartRemoteConnection()
        {
            IsAliveRemoteConnection = true;
            Task.Run(Stream);
        }

        public bool CloseRemoteConnection()
        {
            IsAliveRemoteConnection = false;
            var endPoint = new IPEndPoint(IPAddress.Parse(Host.IpAddress), NetHelper.RemoteCommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.Timeout);
            TcpClient client = null;

            try
            {
                client = new TcpClient(endPoint);
                var keys = RsaEngine.GetKeys();
                var command = new RemoteCommand(null, keys[1]) { Type = CommandType.Abort };
                var datagram = new Datagram(command.ToBytes(), typeof(RemoteCommand), publicKey);
                var bytes = datagram.ToBytes();

                using (var stream = client.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);

                    do
                    {
                        bytes = new byte[NetHelper.BufferSize];
                        stream.Read(bytes, 0, bytes.Length);
                    } 
                    while (stream.DataAvailable);
                }
                
                datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData(keys[0]));

                return result.Status == CommandResultStatus.Successed;
            }
            catch (SocketException)
            {
            }
            finally
            {
                client?.Close();
            }

            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Stream()
        {
            var remoteIp = new IPEndPoint(IPAddress.Parse(Host.IpAddress), NetHelper.RemoteCommandPort);
            TcpClient tcpClient = null;
            UdpClient udpClient = null;

            try
            {
                RSAParameters? publicKey;

                do
                {
                    publicKey = NetHelper.GetPublicKeyOrDefault(remoteIp, NetHelper.Timeout);

                    if (!IsAliveRemoteConnection)
                        return;
                } 
                while (!publicKey.HasValue);
                
                tcpClient = new TcpClient(remoteIp);
                _keys = RsaEngine.GetKeys();
                var remoteObject = new RemoteObject(_ourIpEndPoint.Address.ToString(), NetHelper.RemoteStreamPort);
                var command = new RemoteCommand(remoteObject.ToBytes(), _keys[1]);
                var datagram = new Datagram(command.ToBytes(), typeof(RemoteCommand), publicKey.Value);
                var bytes = datagram.ToBytes();

                using (var stream = tcpClient.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);

                    do
                    {
                        bytes = new byte[NetHelper.BufferSize];
                        stream.Read(bytes, 0, bytes.Length);
                    } 
                    while (stream.DataAvailable);
                }
                
                KeySwap(new IPEndPoint(_ourIpEndPoint.Address, NetHelper.RemoteCommandPort));
                datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData(_keys[0]));
                
                if (result.Status == CommandResultStatus.Failed)
                {
                    IsAliveRemoteConnection = false;
                    return;
                }
                
                Task.Run(() => Control(publicKey));
                remoteIp = new IPEndPoint(remoteIp.Address, NetHelper.RemoteStreamPort);
                udpClient = new UdpClient(remoteIp.Port);
                
                while (IsAliveRemoteConnection)
                {
                    var data = udpClient.Receive(ref remoteIp);
                    var countOfBlocks = data[0];
                    data = data[^1..]; // 1 — количество блоков, 4 — длина, 4 — ширина.

                    for (var i = 0; i < countOfBlocks - 1; ++i)
                        data = data.Concat(udpClient.Receive(ref remoteIp)).ToArray();

                    datagram = Datagram.FromBytes(data);
                    data = datagram.GetData(_keys[0]);
                    var height = BitConverter.ToInt32(data.AsSpan()[..4]);
                    var width = BitConverter.ToInt32(data.AsSpan()[^4..8]);
                    var pixels = ScreenMatrix.GetPixelsFromBytesOrDefault(data[^8..]);

                    _screen ??= new ScreenMatrix(height, width);
                    _screen.UpdateScreen(pixels, ImageScreen);
                }
            }
            catch (SocketException)
            {
            }
            finally
            {
                tcpClient?.Close();
                udpClient?.Close();
            }
        }

        private void Control(object obj)
        {
            var publicKey = (RSAParameters)obj;
            var remoteIp = new IPEndPoint(IPAddress.Parse(Host.IpAddress), NetHelper.RemoteControlPort);
            var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(remoteIp);

            try
            {
                while (IsAliveRemoteConnection)
                {
                    var datagram = new Datagram(CurrentControlState.ToBytes(), typeof(RemoteControl), publicKey);
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

        private void KeySwap(IPEndPoint endPoint)
        {
            TcpListener server = null;
            TcpClient client = null;
            _keys = RsaEngine.GetKeys();

            try
            {
                server = new TcpListener(endPoint);
                client = new TcpClient(endPoint);
                byte[] bytes;
                
                
                using (var stream = client.GetStream())
                {
                    do
                    {
                        bytes = new byte[NetHelper.BufferSize];
                        stream.Read(bytes, 0, bytes.Length);
                    } 
                    while (stream.DataAvailable);
                }
                
                var datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData());
                var publicKey = result.PublicKey.GetKey();

                result = new CommandResult(CommandResultStatus.Successed, null) { PublicKey = new RsaKey(_keys[1]) };
                datagram = new Datagram(result.ToBytes(), typeof(CommandResult), publicKey);
                bytes = datagram.ToBytes();

                using (var stream = client.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
                
                _keys[1] = publicKey;
            }
            catch (SocketException)
            {
            }
            finally
            {
                client?.Close();
                server?.Stop();
            }
        }
    }
}