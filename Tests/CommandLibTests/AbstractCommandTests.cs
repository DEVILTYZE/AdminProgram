using System.Security.Cryptography;
using System.Text;
using CommandLib.Commands;
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
    }
}