using System;
using System.Net;
using CommandLib.Commands.RemoteCommandItems;
using SecurityChannel;

namespace CommandLib.Commands.TransferCommandItems
{
    [Serializable]
    public class TransferObject : RemoteObject
    {
        public string Path { get; set; }

        public TransferObject(string ipAddress, int port, RsaKey key, string path) : base(ipAddress, port, key)
            => Path = path;

        public override object GetData()
            => (new IPEndPoint(IPAddress.Parse(IpAddress), Port), Key.GetKey(), Path);
    }
}