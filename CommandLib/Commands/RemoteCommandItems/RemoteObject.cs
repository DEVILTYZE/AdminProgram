using System;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using SecurityChannel;

namespace CommandLib.Commands.RemoteCommandItems
{
    [Serializable]
    public class RemoteObject
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public RsaKey Key { get; set; }

        public RemoteObject() { }

        public RemoteObject(string ipAddress, int port, RsaKey key)
        {
            IpAddress = ipAddress;
            Port = port;
            Key = key;
        }

        public (IPEndPoint, RSAParameters) GetData() =>
            (new IPEndPoint(IPAddress.Parse(IpAddress), Port), Key.GetKey());

        public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, ConstHelper.Options);

        public static RemoteObject FromBytes(byte[] data) =>
            JsonSerializer.Deserialize<RemoteObject>(data, ConstHelper.Options);
    }
}