using System;
using System.Net;
using System.Text.Json.Serialization;
using CommandLib.Commands.RemoteCommandItems;

namespace CommandLib.Commands.TransferCommandItems
{
    [Serializable]
    public class TransferObject : RemoteObject
    {
        public string Path { get; set; }

        [JsonConstructor]
        public TransferObject() { }

        public TransferObject(string ipAddress, int port, string path) : base(ipAddress, port) => Path = path;

        public override object GetData() => (new IPEndPoint(IPAddress.Parse(IpAddress), Port), Path);
    }
}