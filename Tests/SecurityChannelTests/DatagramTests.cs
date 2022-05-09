using System.Security.Cryptography;
using System.Text;
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
        
        [SetUp]
        public void Setup()
        {
            _aesKey = AesEngine.GetKey();
            _rsaKeys = RsaEngine.GetKeys();
            _datagram = new Datagram(Encoding.UTF8.GetBytes(Data), _aesKey, _rsaKeys[1]);
        }

        [Test]
        public void CreateDatagram()
            => Assert.AreEqual(Data, Encoding.UTF8.GetString(_datagram.GetData(_rsaKeys[0])));
        
        [Test]
        public void ByteArrayDatagram()
        {
            var bytes = _datagram.ToBytes();
            var newDatagram = Datagram.FromBytes(bytes);
            
            Assert.AreEqual(_datagram, newDatagram);
        }
    }
}