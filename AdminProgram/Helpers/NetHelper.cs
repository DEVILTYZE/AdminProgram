using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using AdminProgram.Models;

namespace AdminProgram.Helpers
{
    public static class NetHelper
    {
        public const int Port = 4022;
        
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
            var buffer = Encoding.ASCII.GetBytes("abcdabcdabcdabcdabcdabcdabcdabcd");
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
    }
}