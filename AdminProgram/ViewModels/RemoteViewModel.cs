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
using CommandLib.Commands.RemoteCommandItems;
using SecurityChannel;

namespace AdminProgram.ViewModels
{
    public sealed class RemoteViewModel : INotifyPropertyChanged
    {
        private readonly Host _host;
        private bool _isAliveRemoteConnection;
        private Bitmap _imageScreen;
        private readonly IPEndPoint _ourIpEndPoint;
        private RSAParameters _privateKey, _publicKey;
        private ScreenMatrix _screen;
        private Thread _remoteConnectionThread;
        private readonly int _height, _width;

        public Size WindowSize => new(_width, _height);

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

        public RemoteViewModel(Host host, IPEndPoint ourIpEndPoint) : this()
        {
            Host = host;
            _ourIpEndPoint = ourIpEndPoint;
        }

        public RemoteViewModel()
        {
            _height = _width = 500; // Просто 500.
            GenerateNewKeys();
        }

        public void StartRemoteConnection()
        {
            IsAliveRemoteConnection = true;
            _remoteConnectionThread = new Thread(RemoteConnection);
            _remoteConnectionThread.Start();
        }

        public void CloseRemoteConnection() => IsAliveRemoteConnection = false;
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RemoteConnection()
        {
            var remoteIp = Host.RouteIp;
            var client = new UdpClient();
            
            try
            {
                RSAParameters? publicKey;
                
                do
                {
                    publicKey = NetHelper.GetPublicKeyOrDefault(client, remoteIp, NetHelper.Timeout);
                } 
                while (!publicKey.HasValue);
                
                var command = new RemoteCommand(_ourIpEndPoint, _publicKey);
                var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), publicKey.Value,
                    typeof(RemoteCommand).FullName);
                var datagramBytes = datagram.ToBytes();
                client.Send(datagramBytes, datagramBytes.Length, remoteIp);

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
            catch (SocketException) { }
            finally
            {
                client.Close();
            }
        }

        private void GenerateNewKeys()
        {
            var keys = RsaEngine.GetKeys();
            _privateKey = keys[0];
            _publicKey = keys[1];
        }
    }
}