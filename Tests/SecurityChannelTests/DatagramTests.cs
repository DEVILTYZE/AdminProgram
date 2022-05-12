using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            _datagram = new Datagram(Encoding.Default.GetBytes(Data), _aesKey, _rsaKeys[1], Data.GetType().FullName);
            _jsonDatagram = JsonSerializer.Serialize(_datagram);
        }

        [Test]
        public void CreateDatagram()
            => Assert.AreEqual(Data, Encoding.Default.GetString(_datagram.GetData(_rsaKeys[0])));
        
        [Test]
        public void ByteArrayDatagram()
        {
            var bytes = _datagram.ToBytes();
            var newJsonDatagram = JsonSerializer.Serialize(Datagram.FromBytes(bytes));
            
            Assert.AreEqual(_jsonDatagram, newJsonDatagram);
        }
    }
}