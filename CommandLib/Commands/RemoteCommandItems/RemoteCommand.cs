using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommandLib.Annotations;
using CommandLib.Commands.Helpers;
using SecurityChannel;

namespace CommandLib.Commands.RemoteCommandItems
{
    [Serializable]
    public class RemoteCommand : AbstractCommand
    {
        private const int MaxSentDatagrams = 50;
        private static readonly Size LowQualitySize = new(1400, 950);
        
        private bool _isActive;
        private byte[] _currentImage;
        private RSAParameters[] _keys;
        private UdpClient _udpClient;

        public RemoteCommand() { }

        public RemoteCommand(byte[] data, RSAParameters? publicKey = null)
            : base(ConstHelper.StreamCommandId, ConstHelper.StreamCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            IPEndPoint remoteIp;
            _isActive = true;

            try
            {
                remoteIp = (IPEndPoint)RemoteObject.FromBytes(Data, typeof(RemoteObject)).GetData();
            }
            catch (Exception)
            {
                return new CommandResult(CommandResultStatus.Failed, Encoding.UTF8.GetBytes(ConstHelper.DataError));
            }

            Task.Run(() => StartRemoteConnection(remoteIp));
            Task.Run(() => RemoteControl(remoteIp));
            var size = DisplayTools.GetPhysicalDisplaySize();

            return new CommandResult(CommandResultStatus.Successed, 
                BitConverter.GetBytes(size.Height).Concat(BitConverter.GetBytes(size.Width)).ToArray());
        }

        public override void Abort()
        {
            _isActive = false;
            _udpClient.Close();
        }

        private void StartRemoteConnection([CanBeNull] object obj)
        {
            if (obj is null || !_isActive)
                return;

            var remoteIp = (IPEndPoint)obj;
            UdpClient client = null;

            try
            {
                client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(remoteIp);
                KeySwap(remoteIp.Address.ToString(), NetHelper.KeysPort);
                var size = DisplayTools.GetPhysicalDisplaySize();
                var resendScreenCount = 0;

                while (_isActive)
                {
                    var image = new Bitmap(size.Width, size.Height);
                    var graphics = Graphics.FromImage(image);
                    graphics.CopyFromScreen(0, 0, 0, 0, size);
                    image = ReduceQuality(image);

                    if (resendScreenCount == MaxSentDatagrams)
                    {
                        _currentImage = null;
                        resendScreenCount = 0;
                    }

                    var bytesImage = ByteHelper.ImageToBytes(image);
                    var data = ByteHelper.ImagesXOrCompress((byte[])bytesImage.Clone(), _currentImage);
                    _currentImage = bytesImage;

                    if (data.Length == 0)
                        continue;

                    var datagram = new Datagram(data, typeof(byte[]), _keys[1]);
                    var resultBytes = datagram.ToBytes();
                    var countOfBlocks = resultBytes.Length % Datagram.Length == 0
                        ? (byte)(resultBytes.Length / Datagram.Length)
                        : (byte)(resultBytes.Length / Datagram.Length + 1);
                    resultBytes = new[] { countOfBlocks }.Concat(resultBytes).ToArray();

                    var listBytes = countOfBlocks > 1
                        ? CutDatagramBytes(resultBytes, countOfBlocks)
                        : new List<byte[]>(new[] { resultBytes });

                    foreach (var byteArray in listBytes)
                        client.Send(byteArray, byteArray.Length, remoteIp.Address.ToString(), remoteIp.Port);

                    ++resendScreenCount;
                }
            }
            catch (SocketException)
            {
                _isActive = false;
                _udpClient?.Close();
            }
            finally
            {
                client?.Close();
            }
        }

        private void RemoteControl(IPEndPoint remoteIp)
        {
            try
            {
                _udpClient = new UdpClient(remoteIp.Port);
                var bytes = _udpClient.Receive(ref remoteIp);
                var datagram = Datagram.FromBytes(bytes);
                var remoteControl = RemoteControlObject.FromBytes(datagram.GetData(_keys[0]));
                // TODO: CONTROL>>>
            }
            catch (SocketException)
            {
                _isActive = false;
            }
            finally
            {
                _udpClient?.Close();
            }
        }

        private void KeySwap(string ipAddress, int port)
        {
            _keys = RsaEngine.GetKeys();
            var publicKey = new RsaKey(_keys[1]);
            var result = new CommandResult(CommandResultStatus.Successed, null) { PublicKey = publicKey };
            var datagram = new Datagram(result.ToBytes(), typeof(CommandResult));
            var bytes = datagram.ToBytes();
            TcpClient client = null;
            
            try
            {
                client = new TcpClient(ipAddress, port);

                using (var stream = client.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);

                    bytes = NetHelper.StreamRead(stream);
                }
                
                datagram = Datagram.FromBytes(bytes);
                result = CommandResult.FromBytes(datagram.GetData(_keys[0]));
                _keys[1] = result.PublicKey.GetKey();
            }
            catch (SocketException)
            {
                _isActive = false;
            }
            finally
            {
                client?.Close();
            }
        }

        private static List<byte[]> CutDatagramBytes(byte[] bytes, byte countOfBlocks)
        {
            var list = new List<byte[]>(countOfBlocks);
            
            for (var i = 0; i < countOfBlocks - 1; ++i)
                list.Add(bytes[(Datagram.Length * i)..(Datagram.Length * (i + 1))]);
            
            list.Add(bytes[(Datagram.Length * (countOfBlocks - 1))..]);

            return list;
        }

        private static Bitmap ReduceQuality(Image image)
        {
            var widthCoef = (float)image.Width / LowQualitySize.Width;
            var heightCoef = (float)image.Height / LowQualitySize.Height;
            var ratio = widthCoef > heightCoef ? widthCoef : heightCoef;
            int width = (int)(image.Width / ratio), height = (int)(image.Height / ratio);
            var rectangle = new Rectangle(0, 0, width, height);
            var newImage = new Bitmap(width, height);
            var g = Graphics.FromImage(newImage);
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.DrawImage(image, rectangle, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);

            return newImage;
        }
    }
}