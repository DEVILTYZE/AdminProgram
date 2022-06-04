using System;
using System.Collections.Generic;
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

            if (!File.Exists(path) && !Directory.Exists(path))
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
                client = new TcpClient(remoteIp.Address.ToString(), remoteIp.Port);

                var paths = isDirectory
                    ? Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    : new[] { path };

                if (paths.Length >= byte.MaxValue)
                {
                    client.Close();
                    return;
                }

                using var stream = client.GetStream();
                stream.WriteByte((byte)paths.Length);
                
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
                    var listOfBytes = CutDatagramBytes(bytes);

                    foreach (var currentBytes in listOfBytes)
                        stream.Write(currentBytes, 0, currentBytes.Length);
                }
            }
            catch (SocketException)
            {
                _isActive = false;
            }
            catch (Exception)
            {
                _isActive = false;
            }
            finally
            {
                client?.Close();
            }
        }

        private static byte[] FileNameToByteArray(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var bytes = Encoding.UTF8.GetBytes(fileName);

            return BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray();
        }

        private static List<byte[]> CutDatagramBytes(byte[] bytes)
        {
            if (bytes.Length <= Datagram.TcpLength)
                return new List<byte[]> { bytes };
            
            var countOfBlocks = bytes.Length % Datagram.TcpLength == 0
                ? bytes.Length / Datagram.TcpLength
                : bytes.Length / Datagram.TcpLength + 1;
            var list = new List<byte[]>(countOfBlocks);
            
            for (var i = 0; i < countOfBlocks - 1; ++i)
                list.Add(bytes[(Datagram.TcpLength * i)..(Datagram.TcpLength * (i + 1))]);
            
            list.Add(bytes[(Datagram.TcpLength * (countOfBlocks - 1))..]);

            return list;
        }
    }
}