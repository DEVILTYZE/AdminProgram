using System;
using System.Net;
using System.Security.Cryptography;
using CommandLib.Commands.RemoteCommandItems;
using SecurityChannel;

namespace CommandLib.Commands.TransferCommandItems
{
    [Serializable]
    public class TransferObject : RemoteObject
    {
        public string FilePath { get; set; }

        public TransferObject(string ipAddress, int port, RsaKey key, string path) : base(ipAddress, port, key)
            => FilePath = path;

        public override object GetData()
            => (new IPEndPoint(IPAddress.Parse(IpAddress), Port), Key.GetKey(), FilePath);
    }
}