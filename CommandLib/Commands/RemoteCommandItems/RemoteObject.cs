using System;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using CommandLib.Commands.Helpers;
using SecurityChannel;

namespace CommandLib.Commands.RemoteCommandItems
{
    [Serializable]
    public class RemoteObject : ICommandObject
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

        public virtual object GetData() =>
            (new IPEndPoint(IPAddress.Parse(IpAddress), Port), Key.GetKey());

        public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, ConstHelper.Options);

        public static ICommandObject FromBytes(byte[] data, Type type) =>
            (ICommandObject)JsonSerializer.Deserialize(data, type, ConstHelper.Options);
    }
}