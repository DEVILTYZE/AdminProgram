using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommandLib.Commands;
using NUnit.Framework;
using SecurityChannel;

namespace Tests.CommandLibTests
{
    public class AbstractCommandTest
    {
        private RSAParameters[] _keys;
        private AbstractCommand _command;
        private string _jsonCommand;

        [SetUp]
        public void Setup()
        {
            const string str = "Sample text";
            _keys = RsaEngine.GetKeys();
            _command = new MessageCommand(Encoding.Unicode.GetBytes(str), _keys[1]);
            _jsonCommand = JsonSerializer.Serialize((MessageCommand)_command);
        }

        [Test]
        public void ByteArrayAbstractCommand()
        {
            var bytes = _command.ToBytes();
            var command = (MessageCommand)AbstractCommand.FromBytes(bytes, typeof(MessageCommand));
            var newJsonCommand = JsonSerializer.Serialize(command);
            
            Assert.AreEqual(_jsonCommand, newJsonCommand);
        }
    }
}