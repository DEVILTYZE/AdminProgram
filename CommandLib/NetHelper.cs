using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using CommandLib.Commands;
using CommandLib.Commands.Helpers;

namespace CommandLib
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
        private static readonly string RequestPath = Environment.CurrentDirectory + "\\request.apd";
        private static readonly string LocationPath = Environment.CurrentDirectory + "\\location.apd";

        public const int CommandPort = 51000;
        public const int RemotePort = 52000;
        public const int RemoteControlPort = 52001;
        public const int CloseRemotePort = 51999;
        public const int TransferPort = 49500;
        public const int CloseTransferPort = 49501;
        public const int Timeout = 2500;
        public const int LoadTimeout = 20000;
        public const int MaxFileLength = 157286400;

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
        
        public static RSAParameters? GetPublicKeyOrDefault(UdpClient client, IPEndPoint remoteIp, int receiveTimeout)
        {
            client.Client.ReceiveTimeout = receiveTimeout;
            var command = new MessageCommand(Array.Empty<byte>());
            var datagram = new Datagram(command.ToBytes(), null, typeof(MessageCommand));
            var datagramBytes = datagram.ToBytes();
            byte[] data;

            client.Send(datagramBytes, datagramBytes.Length, remoteIp);

            try
            {
                data = client.Receive(ref remoteIp);
            }
            catch (SocketException)
            {
                return null;
            }

            var receivedDatagram = Datagram.FromBytes(data);
            var result = CommandResult.FromBytes(receivedDatagram.GetData());

            if (result.Status == CommandResultStatus.Failed)
                return null;

            return result.PublicKey.GetKey();
        }

        public static bool PortIsEnabled(string ipAddress, int port)
        {
            var ip = IPAddress.Parse(ipAddress);
            var key = GetPublicKeyOrDefault(new UdpClient(), new IPEndPoint(ip, port), Timeout);

            return key is not null;
        }

        public static bool SetPort(int port, int otherPort, string protocol, string ipAddress, int enabled, 
            string infoString, int leaseInfo)
        {
            const string serviceType = "<serviceType>urn:schemas-upnp-org:service:WANIPConnection:1</serviceType>";
            const string controlUrlTag = "<controlURL>";

            if (!File.Exists(RequestPath))
                return false;

            var requestString = GetRequestString(RequestPath, port, otherPort, protocol, ipAddress, enabled, infoString,
                leaseInfo);

            string location;
            
            if (File.Exists(LocationPath))
            {
                using var sr = new StreamReader(LocationPath);
                location = sr.ReadToEnd();
                
                return AddPort(location, requestString);
            }
            
            location = GetLocation();
            var request = (HttpWebRequest)WebRequest.Create(location);
            request.Method = "GET";
            request.UserAgent = "Microsoft-Windows/6.1 UpnP/1.0";
            var response = request.GetResponse();
            string responseString; 
            
            using (var stream = response.GetResponseStream())
            {
                if (stream is null)
                    return false;
                
                using (var sr = new StreamReader(stream))
                {
                    responseString = sr.ReadToEnd();
                }
            }

            var indexServiceType = responseString.IndexOf(serviceType, StringComparison.Ordinal);

            if (indexServiceType == -1)
                return false;

            var indexControlUrl = responseString.IndexOf(controlUrlTag, indexServiceType, StringComparison.Ordinal);

            if (indexControlUrl == -1)
                return false;

            var controlUrl = responseString[(indexControlUrl + controlUrlTag.Length)..responseString.IndexOf(
                "</controlURL>", indexControlUrl, StringComparison.Ordinal)];
            location = location[..location.IndexOf('/', 8)] + controlUrl;
            response.Close();

            using (var sw = new StreamWriter(LocationPath))
            {
                sw.Write(location);
            }

            return AddPort(location, requestString);
        }

        private static string GetRequestString(string requestFilePath, int port, int port2, string protocol, 
            string ipAddress, int enabled, string infoString, int leaseInfo)
        {
            var infoBlocksDictionary = new Dictionary<string, string>
            {
                { "[PORT_INFO_1]", port.ToString() },
                { "[PROTOCOL_INFO]", protocol },
                { "[PORT_INFO_2]", port2.ToString() },
                { "[IP_ADDRESS_INFO]", ipAddress },
                { "[ENABLED_INFO]", enabled.ToString() },
                { "[INFO_STRING]", infoString },
                { "[LEASE_INFO]", leaseInfo.ToString() },
            };

            string requestString;
            
            using (var sr = new StreamReader(requestFilePath))
            {
                requestString = sr.ReadToEnd();
            }

            foreach (var (infoKey, infoValue) in infoBlocksDictionary)
                requestString = requestString.Replace(infoKey, infoValue);

            return requestString;
        }
        
        private static string GetLocation()
        {
            const string searchString = "M-SEARCH * HTTP/1.1\r\nHOST:239.255.255.250:1900\r\nMAN:\"ssdp:discover" +
                "\"\r\nST:upnp:rootdevice\r\nMX:3\r\n\r\n";
            var multicastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
            var localEndPoint = new IPEndPoint(GetLocalAddress(), 0);

            var client = new UdpClient(AddressFamily.InterNetwork);
            
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(localEndPoint);
            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, 
                new MulticastOption(multicastEndPoint.Address, IPAddress.Any));
            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);

            var data = Encoding.UTF8.GetBytes(searchString);

            for (var i = 0; i < 3; ++i)
                client.Send(data, data.Length, multicastEndPoint);

            var count = 0;
            var result = string.Empty;
            
            while (true)
            {
                if (client.Available == 0)
                {
                    Thread.Sleep(100);
                    ++count;
                    
                    if (count == 10)
                        break;
                }
                else
                {
                    const string locationString = "LOCATION: ";
                    data = client.Receive(ref multicastEndPoint);
                    var currentString = Encoding.UTF8.GetString(data);
                    var index = currentString.IndexOf(locationString, StringComparison.Ordinal);

                    if (index == -1)
                        continue;

                    result = currentString[(index + locationString.Length)..currentString.IndexOf('\r', index)];
                    break;
                }
            }
            
            client.Close();

            return result;
        }

        private static IPAddress GetLocalAddress() 
            => (from networkInterface in NetworkInterface.GetAllNetworkInterfaces() 
                select networkInterface.GetIPProperties() into properties 
                where properties.GatewayAddresses.Count != 0 
                from address in properties.UnicastAddresses 
                where address.Address.AddressFamily == AddressFamily.InterNetwork
                where address.Address.ToString().StartsWith("192")
                where !IPAddress.IsLoopback(address.Address) 
                select address.Address).FirstOrDefault();

        private static bool AddPort(string location, string requestString)
        {
            var request = (HttpWebRequest)WebRequest.Create(location);

            request.Method = "POST";
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Pragma", "no-cache");
            request.Headers.Add("SOAPAction", "\"urn:schemas-upnp-org:service:WANIPConnection:1#AddPortMapping\"");
            request.ContentType = "text/xml; charset=\"utf-8\"";
            request.UserAgent = "Microsoft-Windows/6.1 UPnP/1.0";

            var data = Encoding.UTF8.GetBytes(requestString);
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = request.GetResponse();
            string responseString;

            using (var stream = response.GetResponseStream())
            {
                if (stream is null)
                    return false;

                using (var sr = new StreamReader(stream))
                {
                    responseString = sr.ReadToEnd();
                }
            }

            return !string.IsNullOrEmpty(responseString);
        }
    }
}