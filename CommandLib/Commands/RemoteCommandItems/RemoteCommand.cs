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
        private static readonly Size LowQualitySize = new(900, 900);
        
        private bool _isActive;
        private ScreenMatrix _screen;
        private RSAParameters[] _keys;

        public RemoteCommand() { }

        public RemoteCommand(byte[] data, RSAParameters? publicKey = null)
            : base(ConstHelper.StreamCommandId, ConstHelper.StreamCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            IPEndPoint remoteIp;
            _isActive = true;

            try
            {
                remoteIp = (IPEndPoint)RemoteObject.FromBytes(Data, 
                    typeof(RemoteObject)).GetData();
            }
            catch (Exception)
            {
                return new CommandResult(CommandResultStatus.Failed, Encoding.UTF8.GetBytes(ConstHelper.DataError));
            }

            Task.Run(() => StartRemoteConnection(remoteIp));

            return new CommandResult(CommandResultStatus.Successed, Array.Empty<byte>());
        }

        public override void Abort() => _isActive = false;

        private void StartRemoteConnection([CanBeNull] object obj)
        {
            if (obj is null || !_isActive)
                return;

            var remoteIp = (IPEndPoint)obj;
            var client = new UdpClient(remoteIp);
            KeySwap(new IPEndPoint(remoteIp.Address, NetHelper.RemoteCommandPort));
            var size = DisplayTools.GetPhysicalDisplaySize();
            var image = new Bitmap(size.Width, size.Height);
            var graphics = Graphics.FromImage(image);
            _screen = new ScreenMatrix();

            Task.Run(() => RemoteControl(_keys[0]));

            while (_isActive)
            {
                graphics.CopyFromScreen(0, 0, 0, 0, size);
                image = ReduceQuality(image);
                _screen.UpdateScreen(image);

                var imageBytes = BitConverter.GetBytes(size.Height).Concat(BitConverter.GetBytes(size.Width))
                    .Concat(_screen.GetUpdatedPixelsBytes()).ToArray();
                var datagram = new Datagram(imageBytes, typeof(byte[]), RsaPublicKey);
                var resultBytes = datagram.ToBytes();
                
                var countOfBlocks = resultBytes.Length % Datagram.Length == 0 
                    ? (byte)(resultBytes.Length / Datagram.Length) 
                    : (byte)(resultBytes.Length / Datagram.Length + 1);
                
                resultBytes = new[] { countOfBlocks }.Concat(resultBytes).ToArray();
                
                var listBytes = countOfBlocks > 1 
                    ? CutImageBytes(resultBytes, countOfBlocks) 
                    : new List<byte[]>(new[] { resultBytes });

                foreach (var byteArray in listBytes)
                    client.Send(byteArray, byteArray.Length, remoteIp);
            }
        }

        private void RemoteControl(object obj)
        {
            // TODO: Доделать...
        }

        private void KeySwap(IPEndPoint endPoint)
        {
            _keys = RsaEngine.GetKeys();
            var publicKey = new RsaKey(_keys[1]);
            var result = new CommandResult(CommandResultStatus.Successed, null) { PublicKey = publicKey };
            var datagram = new Datagram(result.ToBytes(), typeof(CommandResult));
            var bytes = datagram.ToBytes();
            TcpClient client = null;
            
            try
            {
                client = new TcpClient(endPoint);

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
                result = CommandResult.FromBytes(datagram.GetData(_keys[0]));
                _keys[1] = result.PublicKey.GetKey();
            }
            catch (SocketException)
            {
            }
            finally
            {
                client?.Close();
            }
        }

        private static List<byte[]> CutImageBytes(byte[] bytes, byte countOfBlocks)
        {
            var list = new List<byte[]>(countOfBlocks);
            
            for (var i = 0; i < countOfBlocks - 2; ++i)
                list.Add(bytes[(Datagram.Length * i)..(Datagram.Length * (i + 1))]);
            
            list.Add(bytes[(Datagram.Length * (countOfBlocks - 2))..]);

            return list;
        }

        private static Bitmap ReduceQuality(Image image)
        {
            var widthCoef = (float)image.Width / LowQualitySize.Width;
            var heightCoef = (float)image.Height / LowQualitySize.Height;
            var ratio = widthCoef > heightCoef ? widthCoef : heightCoef;
            int width = (int)(ratio * image.Width), height = (int)(ratio * image.Height);
            var rectangle = new Rectangle(0, 0, width, height);
            var newImage = new Bitmap(width, height);
            var g = Graphics.FromImage(newImage);
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.DrawImage(image, rectangle, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);

            return newImage;
        }
    }
}