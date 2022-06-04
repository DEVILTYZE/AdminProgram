using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using AdminProgram.Annotations;
using AdminProgram.Helpers;
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
        private readonly RemoteControlObject _currentControlState;
        private readonly IPEndPoint _ourIpEndPoint;

        private bool _reconnect;
        private bool _isAliveRemoteConnection;
        private UdpClient _udpClient;
        private BitmapImage _imageScreen;
        private int _height, _width;
        private RSAParameters[] _keys;
        private byte[] _imageArray;

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

        public RemoteViewModel(Host host, IPEndPoint ourIpEndPoint) : this()
        {
            Host = host;
            _ourIpEndPoint = ourIpEndPoint;
        }

        public RemoteViewModel()
        {
            CurrentControlState = new RemoteControlObject();
            Height = 950;
            Width = 1400;
        }

        public void StartRemoteConnection()
        {
            Reconnect = false;
            IsAliveRemoteConnection = true;
            Task.Run(Stream);
            //Task.Run(Control); TODO: CONTROL>>>
        }

        public bool CloseRemoteConnection()
        {
            IsAliveRemoteConnection = false;
            var endPoint = new IPEndPoint(IPAddress.Parse(Host.IpAddress), NetHelper.RemoteCommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.Timeout);
            TcpClient client = null;

            try
            {
                client = new TcpClient(Host.IpAddress, NetHelper.RemoteCommandPort);
                var keys = RsaEngine.GetKeys();
                var command = new RemoteCommand(null, keys[1]) { Type = CommandType.Abort };
                var datagram = new Datagram(command.ToBytes(), typeof(RemoteCommand), publicKey);
                var bytes = datagram.ToBytes();

                using (var stream = client.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);

                    bytes = NetHelper.StreamRead(stream);
                }
                
                datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData(keys[0]));

                return result.Status == CommandResultStatus.Successed;
            }
            catch (SocketException)
            {
                return true;
            }
            finally
            {
                client?.Close();
                _udpClient?.Close();
            }
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
                
                tcpClient = new TcpClient(Host.IpAddress, NetHelper.RemoteCommandPort);
                _keys = RsaEngine.GetKeys();
                var task = Task.Run(() => KeySwap(new IPEndPoint(_ourIpEndPoint.Address, NetHelper.KeysPort)));
                var remoteObject = new RemoteObject(_ourIpEndPoint.Address.ToString(), NetHelper.RemoteStreamPort);
                var command = new RemoteCommand(remoteObject.ToBytes(), _keys[1]);
                var datagram = new Datagram(command.ToBytes(), typeof(RemoteCommand), publicKey.Value);
                var bytes = datagram.ToBytes();
                
                using (var stream = tcpClient.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                    
                    bytes = NetHelper.StreamRead(stream);
                }
                
                task.Wait();
                datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData(_keys[0]));
                
                if (result.Status == CommandResultStatus.Failed)
                {
                    IsAliveRemoteConnection = false;
                    return;
                }

                Height = BitConverter.ToInt32(result.Data.AsSpan()[..4]);
                Width = BitConverter.ToInt32(result.Data.AsSpan()[4..]);
                
                remoteIp = null;
                _udpClient = new UdpClient(NetHelper.RemoteStreamPort);

                // Application.Current.Dispatcher.Invoke(() =>
                // {
                //     _imageArray = ByteHelper.ImageToBytes(BitmapImageHelper.BitmapImageToBitmap(ImageScreen));
                // });

                while (IsAliveRemoteConnection)
                {
                    bytes = _udpClient.Receive(ref remoteIp);
                    var countOfBlocks = bytes[0];
                    bytes = bytes[1..]; // 1 — количество блоков, 4 — длина, 4 — ширина.

                    if (countOfBlocks is < 0 or > 4) // Переподключение, если слишком много блоков.
                    {
                        Reconnect = true;
                        IsAliveRemoteConnection = false;
                        break;
                    }

                    for (var i = 0; i < countOfBlocks - 1; ++i)
                        bytes = bytes.Concat(_udpClient.Receive(ref remoteIp)).ToArray();

                    datagram = Datagram.FromBytes(bytes);
                    bytes = datagram.GetData(_keys[0]);
                    var data = bytes;
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _imageArray =  ByteHelper.ImagesXOrDecompress(data, _imageArray);
                        
                        try
                        {
                            var image = ByteHelper.BytesToImage(_imageArray);
                            ImageScreen = BitmapImageHelper.BitmapToBitmapImage(image);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    });
                }
            }
            catch (OutOfMemoryException)
            {
                Reconnect = false;
                IsAliveRemoteConnection = false;
            }
            catch (SocketException)
            {
            }
            finally
            {
                tcpClient?.Close();
                _udpClient?.Close();
            }
        }

        private void Control()
        {
            UdpClient client = null;
            
            try
            {
                client = new UdpClient();
                
                while (IsAliveRemoteConnection)
                {
                    var datagram = new Datagram(CurrentControlState.ToBytes(), typeof(RemoteControlObject), _keys[1]);
                    var bytes = datagram.ToBytes();
                    client.Send(bytes, bytes.Length, Host.IpAddress, NetHelper.RemoteControlPort);
                    CurrentControlState.ToStartState();
                }
            }
            catch (SocketException)
            {
                IsAliveRemoteConnection = false;
                _udpClient?.Close();
            }
            finally
            {
                client?.Close();
            }
        }

        private void KeySwap(IPEndPoint endPoint)
        {
            TcpListener server = null;
            TcpClient client = null;

            try
            {
                server = new TcpListener(endPoint);
                server.Start();
                client = server.AcceptTcpClient();

                using var stream = client.GetStream();
                var bytes = NetHelper.StreamRead(stream);
                var datagram = Datagram.FromBytes(bytes);
                var result = CommandResult.FromBytes(datagram.GetData());
                var publicKey = result.PublicKey.GetKey();

                result = new CommandResult(CommandResultStatus.Successed, null) { PublicKey = new RsaKey(_keys[1]) };
                datagram = new Datagram(result.ToBytes(), typeof(CommandResult), publicKey);
                bytes = datagram.ToBytes();
                stream.Write(bytes, 0, bytes.Length);
                _keys[1] = publicKey;
            }
            catch (SocketException)
            {
                IsAliveRemoteConnection = false;
            }
            finally
            {
                client?.Close();
                server?.Stop();
            }
        }
    }
}