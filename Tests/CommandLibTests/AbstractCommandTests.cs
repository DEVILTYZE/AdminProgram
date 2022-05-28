using System.Security.Cryptography;
using System.Text;
using CommandLib;
using CommandLib.Commands;
using CommandLib.Commands.TransferCommandItems;
using NUnit.Framework;
using SecurityChannel;

namespace Tests.CommandLibTests
{
    public class AbstractCommandTest
    {
        private const string Str = "Sample text";
        private RSAParameters[] _keys;
        private AbstractCommand _command;

        [SetUp]
        public void Setup()
        {
            _keys = RsaEngine.GetKeys();
            _command = new MessageCommand(Encoding.Unicode.GetBytes(Str), _keys[1]);
        }

        [Test]
        public void ByteArrayAbstractCommand()
        {
            var bytes = _command.ToBytes();
            var command = AbstractCommand.FromBytes(bytes, typeof(MessageCommand));
            
            Assert.AreEqual(Str, Encoding.Unicode.GetString(command.Data));
        }

        [Test]
        public void TransferCommandTest()
        {
            var command = new TransferCommand(new TransferObject("1", 1, "1").ToBytes(), _keys[1]);
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(TransferCommand), _keys[1]);
            var bytes = datagram.ToBytes();
            datagram = Datagram.FromBytes(bytes);
            var newCommand = AbstractCommand.FromBytes(datagram.GetData(_keys[0]), datagram.Type);
            
            Assert.AreEqual(command.Data, newCommand.Data);
        }
        
        [Test]
        public void ShutdownCommandTest()
        {
            var command = new ShutdownCommand(null);
            var datagram = new Datagram(command.ToBytes(), AesEngine.GetKey(), typeof(ShutdownCommand), _keys[1]);
            var bytes = datagram.ToBytes();
            var str = Encoding.UTF8.GetString(bytes);
            datagram = Datagram.FromBytes(bytes);
            var newCommand = AbstractCommand.FromBytes(datagram.GetData(_keys[0]), datagram.Type);
            
            Assert.AreEqual(command.Data, newCommand.Data);
        }
    }
}