using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AdminProgram.Helpers;
using AdminProgram.Models;
using CommandLib;
using CommandLib.Commands.Helpers;
using CommandLib.Commands.RemoteCommandItems;
using SecurityChannel;

namespace AdminProgram.ViewModels
{
    public sealed partial class RemoteViewModel 
    {
        public RemoteViewModel(Host host, IPEndPoint ourIpEndPoint, LogViewModel logModel) : this()
        {
            Host = host;
            _ourIpEndPoint = ourIpEndPoint;
            _logModel = logModel;
        }

        public RemoteViewModel()
        {
            CurrentControlState = new RemoteControlObject();
            Height = 950;
            Width = 1400;
        }

        public void StartRemoteConnection()
        {
            lock (_logModel.Locker)
                _logModel.AddLog($"{_host.IpAddress} старт удалённого подключения.", LogStatus.Info);
            
            Reconnect = false;
            IsAliveRemoteConnection = true;
            Task.Run(Stream);
        }

        public bool CloseRemoteConnection()
        {
            if (!IsAliveRemoteConnection)
                return true;
            
            IsAliveRemoteConnection = false;
            var endPoint = new IPEndPoint(IPAddress.Parse(Host.IpAddress), NetHelper.RemoteCommandPort);
            var publicKey = NetHelper.GetPublicKeyOrDefault(endPoint, NetHelper.Timeout);
            TcpClient client = null;

            if (!publicKey.HasValue)
                return true;
            
            try
            {
                client = new TcpClient(Host.IpAddress, NetHelper.RemoteCommandPort)
                    { ReceiveTimeout = NetHelper.Timeout };
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
                lock (_logModel.Locker)
                    _logModel.AddLog($"При закрытии удалённого подключения {_host.IpAddress} получена ошибка сокета.",
                        LogStatus.Error);
                
                return true;
            }
            finally
            {
                client?.Close();
                _udpClient?.Close();
            }
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
                    
                    lock (_logModel.Locker)
                        _logModel.AddLog($"Не удалось установить удалённое подключение с {_host.IpAddress}",
                            LogStatus.Error);
                    
                    return;
                }

                Height = BitConverter.ToInt32(result.Data.AsSpan()[..4]);
                Width = BitConverter.ToInt32(result.Data.AsSpan()[4..]);
                Task.Run(Control);
                
                remoteIp = null;
                _udpClient = new UdpClient(NetHelper.RemoteStreamPort);
                var nullCount = 0;

                while (IsAliveRemoteConnection)
                {
                    bytes = _udpClient.Receive(ref remoteIp);
                    var countOfBlocks = bytes[0];
                    bytes = bytes[1..]; // 1 — количество блоков, 4 — длина, 4 — ширина.

                    if (countOfBlocks >= 5) // Переподключение, если слишком много блоков.
                        continue;

                    for (var i = 0; i < countOfBlocks - 1; ++i)
                        bytes = bytes.Concat(_udpClient.Receive(ref remoteIp)).ToArray();

                    datagram = Datagram.FromBytesOrDefault(bytes);
                    
                    if (datagram is null)
                    {
                        ++nullCount;
                        
                        if (nullCount == 30)
                        {
                            Reconnect = true;
                            
                            lock (_logModel.Locker)
                                _logModel.AddLog($"Подключение с {_host.IpAddress} разорвано из-за слишком " +
                                                 $"большого количества потерянных датаграмм.", LogStatus.Error);
                            
                            break;
                        }
                        
                        continue;
                    }
                    
                    bytes = datagram.GetData(_keys[0]);
                    var data = bytes;

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var image = ByteHelper.BytesToImage(ByteHelper.DecompressArray(data));
                            ImageScreen = BitmapImageHelper.BitmapToBitmapImage(image);
                        }
                        catch (Exception)
                        {
                            lock (_logModel.Locker)
                                _logModel.AddLog("Ошибка при декодировании изображения", LogStatus.Error);
                        }
                    });
                }
            }
            catch (OutOfMemoryException)
            {
                Reconnect = false;
                IsAliveRemoteConnection = false;
                
                lock (_logModel.Locker)
                    _logModel.AddLog($"При удалённом подключении к {_host.IpAddress} возникла утечка памяти.",
                        LogStatus.Error);
            }
            catch (SocketException)
            {
                lock (_logModel.Locker)
                    _logModel.AddLog($"При удалённом подключении {_host.IpAddress} получена ошибка сокета.", 
                        LogStatus.Error);
            }
            finally
            {
                tcpClient?.Close();
                
                if (IsAliveRemoteConnection)
                    Task.Run(RefreshConnection);
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
                    CurrentControlState.ToStartCondition();
                    Thread.Sleep(10);
                }
            }
            catch (SocketException)
            {
                IsAliveRemoteConnection = false;
                _udpClient?.Close();
                
                lock (_logModel.Locker)
                    _logModel.AddLog($"При удалённом управлении {_host.IpAddress} получена ошибка сокета.", 
                        LogStatus.Error);
            }
            finally
            {
                client?.Close();
            }
        }

        private void RefreshConnection()
        {
            lock (_logModel.Locker)
                _logModel.AddLog($"Переподключение к {_host.IpAddress}.", LogStatus.Info);
            
            CloseRemoteConnection();
            StartRemoteConnection();
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
                
                lock (_logModel.Locker)
                    _logModel.AddLog($"При обмене ключами {_host.IpAddress} получена ошибка сокета.", 
                        LogStatus.Error);
            }
            finally
            {
                client?.Close();
                server?.Stop();
            }
        }
    }
}