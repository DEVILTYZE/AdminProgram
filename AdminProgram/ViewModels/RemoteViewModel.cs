using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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
        private readonly IPEndPoint _ourIpEndPoint;
        private RSAParameters _privateKey, _publicKey;
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

        public RemoteViewModel(Host host, IPEndPoint ourIpEndPoint)
        {
            Host = host;
            _ourIpEndPoint = ourIpEndPoint;
            GenerateNewKeys();
        }

        public void StartRemoteConnection()
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

                    for (var i = 0; i < countOfBlocks; ++i)
                        data = data.Concat(client.Receive(ref remoteIp)).ToArray();

                    datagram = Datagram.FromBytes(data);
                    data = datagram.GetData(_privateKey);
                    var pixels = ScreenMatrix.GetScreenPixelsFromBytesOrDefault(data);
                    // TODO: Доделать...
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
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void GenerateNewKeys()
        {
            var keys = RsaEngine.GetKeys();
            _privateKey = keys[0];
            _publicKey = keys[1];
        }
    }
}