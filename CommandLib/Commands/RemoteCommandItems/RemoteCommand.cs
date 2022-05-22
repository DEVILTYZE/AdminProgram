using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using CommandLib.Annotations;
using CommandLib.Commands.Helpers;
using SecurityChannel;

namespace CommandLib.Commands.RemoteCommandItems
{
    [Serializable]
    public class RemoteCommand : AbstractCommand
    {
        private bool _isActive;
        private RSAParameters _publicKey;
        private ScreenMatrix _screen;

        public RemoteCommand(byte[] data, RSAParameters? publicKey = null)
            : base(ConstHelper.StreamCommandId, ConstHelper.StreamCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            IPEndPoint remoteIp;
            _isActive = true;

            try
            {
                (remoteIp, _publicKey) = ((IPEndPoint, RSAParameters))RemoteObject.FromBytes(Data, 
                    typeof(RemoteObject)).GetData();
            }
            catch (Exception)
            {
                return new CommandResult(CommandResultStatus.Failed, Encoding.Unicode.GetBytes(ConstHelper.DataError));
            }

            var thread = new Thread(StartRemoteConnection);
            thread.Start(remoteIp);

            return new CommandResult(CommandResultStatus.Successed, Array.Empty<byte>());
        }

        public override void Abort() => _isActive = false;

        private void StartRemoteConnection([CanBeNull] object obj)
        {
            var client = new UdpClient();
            var remoteIp = (IPEndPoint)obj;
            var size = DisplayTools.GetPhysicalDisplaySize();
            var image = new Bitmap(size.Width, size.Height);
            var graphics = Graphics.FromImage(image);
            _screen = new ScreenMatrix(size.Height, size.Width);

            while (_isActive)
            {
                graphics.CopyFromScreen(0, 0, 0, 0, size);
                _screen.UpdateScreen(image);
                
                var imageBytes = _screen.GetUpdatedPixelsBytes();
                var datagram = new Datagram(imageBytes, AesEngine.GetKey(), typeof(byte[]), _publicKey);
                var resultBytes = datagram.ToBytes();
                
                var countOfBlocks = resultBytes.Length % Datagram.Length == 0 
                    ? (byte)(resultBytes.Length / Datagram.Length) 
                    : (byte)(resultBytes.Length / Datagram.Length + 1);
                
                resultBytes = new[] { countOfBlocks }.Concat(BitConverter.GetBytes(size.Height)).Concat(
                    BitConverter.GetBytes(size.Width)).Concat(resultBytes).ToArray();
                
                var listBytes = countOfBlocks > 1 
                    ? CutImageBytes(resultBytes, countOfBlocks) 
                    : new List<byte[]>(new[] { resultBytes });

                foreach (var byteArray in listBytes)
                    client.Send(byteArray, byteArray.Length, remoteIp);
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
    }
}