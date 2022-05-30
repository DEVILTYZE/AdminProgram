using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommandLib;
using CommandLib.Commands;
using NUnit.Framework;
using SecurityChannel;

namespace Tests.SecurityChannelTests
{
    public class DatagramTests
    {
        private const string Data = "SECRET_DATA";
        private RSAParameters[] _rsaKeys;
        private Datagram _datagram;
        private string _jsonDatagram;
        
        [SetUp]
        public void Setup()
        {
            _rsaKeys = RsaEngine.GetKeys();
            _datagram = new Datagram(Encoding.UTF8.GetBytes(Data), Data.GetType(), _rsaKeys[1]);
            _jsonDatagram = JsonSerializer.Serialize(_datagram);
        }

        [Test]
        public void CreateDatagram()
            => Assert.AreEqual(Data, Encoding.UTF8.GetString(_datagram.GetData(_rsaKeys[0])));
        
        [Test]
        public void ByteArrayDatagram()
        {
            var bytes = _datagram.ToBytes();
            var newJsonDatagram = JsonSerializer.Serialize(Datagram.FromBytes(bytes));
            
            Assert.AreEqual(_jsonDatagram, newJsonDatagram);
        }
        
        [Test]
        public void ByteArrayDatagramNull()
        {
            var command = new MessageCommand(Encoding.UTF8.GetBytes(Data));
            var datagram = new Datagram(command.ToBytes(), typeof(MessageCommand));
            var bytes = datagram.ToBytes();
            datagram = Datagram.FromBytes(bytes);
            var newCommand = AbstractCommand.FromBytes(datagram.GetData(), datagram.Type);
            var result = newCommand.Execute();

            Assert.AreEqual(Data, Encoding.UTF8.GetString(result.Data));
        }
    }
}