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
        private byte[] _aesKey;
        private RSAParameters[] _rsaKeys;
        private Datagram _datagram;
        private string _jsonDatagram;
        
        [SetUp]
        public void Setup()
        {
            _aesKey = AesEngine.GetKey();
            _rsaKeys = RsaEngine.GetKeys();
            _datagram = new Datagram(Encoding.Unicode.GetBytes(Data), _aesKey, Data.GetType(), _rsaKeys[1]);
            _jsonDatagram = JsonSerializer.Serialize(_datagram);
        }

        [Test]
        public void CreateDatagram()
            => Assert.AreEqual(Data, Encoding.Unicode.GetString(_datagram.GetData(_rsaKeys[0])));
        
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
            var command = new MessageCommand(Encoding.Unicode.GetBytes(Data));
            var datagram = new Datagram(command.ToBytes(), null, typeof(MessageCommand));
            var bytes = datagram.ToBytes();
            datagram = Datagram.FromBytes(bytes);
            var newCommand = AbstractCommand.FromBytes(datagram.GetData(), datagram.Type);
            var result = newCommand.Execute();

            Assert.AreEqual(Data, Encoding.Unicode.GetString(result.Data));
        }
    }
}