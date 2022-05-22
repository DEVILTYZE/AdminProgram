using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using CommandLib.Annotations;
using CommandLib.Commands.Helpers;
using CommandLib.Commands.RemoteCommandItems;
using SecurityChannel;

namespace CommandLib.Commands.TransferCommandItems
{
    [Serializable]
    public class TransferFileCommand : AbstractCommand
    {
        private RSAParameters _publicKey;
        
        public TransferFileCommand(byte[] data, RSAParameters? publicKey = null) 
            : base(ConstHelper.GetFileCommandId, ConstHelper.GetFileCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            IPEndPoint remoteIp;
            string path;
            
            try
            {
                (remoteIp, _publicKey, path) = ((IPEndPoint, RSAParameters, string))RemoteObject.FromBytes(Data, 
                    typeof(TransferObject)).GetData();
            }
            catch (Exception)
            {
                return new CommandResult(CommandResultStatus.Failed, Encoding.Unicode.GetBytes(ConstHelper.DataError));
            }

            var isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);
            var length = isDirectory 
                ? new DirectoryInfo(path).GetFiles().Sum(thisFInfo => thisFInfo.Length) 
                : new FileInfo(path).Length;

            if (length > NetHelper.MaxFileLength)
                return new CommandResult(CommandResultStatus.Failed, 
                    Encoding.Unicode.GetBytes(ConstHelper.FileLengthError));

            if (!File.Exists(path))
                return new CommandResult(CommandResultStatus.Failed, Encoding.Unicode.GetBytes(ConstHelper.FileError));

            var thread = new Thread(TransferFiles);
            thread.Start((remoteIp, path, isDirectory));

            return new CommandResult(CommandResultStatus.Successed, Array.Empty<byte>());
        }

        private void TransferFiles([CanBeNull] object obj)
        {
            if (obj is null)
                return;
            
            var (remoteIp, path, isDirectory) = ((IPEndPoint, string, bool))obj;
            var client = new TcpClient(remoteIp);
            var paths = isDirectory 
                ? Directory.GetFiles(path, "*", SearchOption.AllDirectories) 
                : new []{ path };
            
            if (paths.Length >= byte.MaxValue)
                return;

            using (var stream = client.GetStream())
            {
                stream.WriteByte((byte)paths.Length);
            }
            
            foreach (var filePath in paths)
            {
                var fInfo = new FileInfo(filePath);
                var fileData = new byte[fInfo.Length];

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    fs.Read(fileData, 0, fileData.Length);

                var datagram = new Datagram(fileData, AesEngine.GetKey(), typeof(byte[]), _publicKey);
                var fileNameByteArray = FileNameToByteArray(filePath);
                var bytes = fileNameByteArray.Concat(datagram.ToBytes()).ToArray();
                bytes = BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray();

                using (var stream = client.GetStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            
            client.Close();
        }

        private static byte[] FileNameToByteArray(string filePath)
        {
            var fileName = filePath[filePath.LastIndexOf('\\')..];
            var bytes = Encoding.Unicode.GetBytes(fileName);

            return BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray();
        }
    }
}