using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLib.Commands.Helpers;

namespace CommandLib.Commands.RemoteCommandItems
{
    [Serializable]
    public class RemoteObject : ICommandObject
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }

        [JsonConstructor]
        public RemoteObject() { }

        public RemoteObject(string ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        public virtual object GetData() => new IPEndPoint(IPAddress.Parse(IpAddress), Port);

        public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, GetType(), ConstHelper.Options);

        public static ICommandObject FromBytes(byte[] data, Type type) 
            => (ICommandObject)JsonSerializer.Deserialize(data, type, ConstHelper.Options);
    }
}