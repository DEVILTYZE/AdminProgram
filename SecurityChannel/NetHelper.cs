using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace SecurityChannel
{
    public enum HostStatus
    {
        On,
        Off,
        Loading,
        Unknown
    }
    
    public static class NetHelper
    {
        public const int Port = 4022;
        public const int MessageCommandPort = 3702;
        public const int Timeout = 5000;
        public const int LoadTimeout = 60000;
        
        public static byte[] GetMagicPacket(string mac)
        {
            var macAddress = PhysicalAddress.Parse(mac);
            var header = Enumerable.Repeat(byte.MaxValue, 6);
            var body = Enumerable.Repeat(macAddress.GetAddressBytes(), 16).SelectMany(thisMac => thisMac);

            return header.Concat(body).ToArray();
        }

        public static HostStatus Ping(string ipAddress)
        {
            var pingSender = new Ping();
            var buffer = Encoding.Unicode.GetBytes("abcdabcdabcdabcdabcdabcdabcdabcd");
            var reply = pingSender.Send(ipAddress, 120, buffer, new PingOptions { DontFragment = true });

            if (reply is null)
                return HostStatus.Unknown;

            return reply.Status switch
            {
                IPStatus.Success => HostStatus.On,
                IPStatus.TimedOut => HostStatus.Off,
                _ => HostStatus.Unknown
            };
        }
        
        public static string GetMacAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var adapter in interfaces)
            {
                var mac = adapter.GetPhysicalAddress().ToString();

                if (!string.IsNullOrEmpty(mac))
                    return mac;
            }

            throw new Exception("Мак адрес не существует");
        }
    }
}