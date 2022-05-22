using System.Security.Cryptography;
using System.Text.Json;
using CommandLib;
using CommandLib.Commands;
using CommandLib.Commands.Helpers;
using NUnit.Framework;
using SecurityChannel;

namespace Tests.CommandLibTests
{
    public class CommandResultTests
    {
        private RSAParameters[] _keys;
        private CommandResult _commandResult;
        private string _jsonCommandResult;
        
        [SetUp]
        public void Setup()
        {
            _keys = RsaEngine.GetKeys();
            _commandResult = new CommandResult(CommandResultStatus.Successed, JsonSerializer.SerializeToUtf8Bytes(
                new RsaKey(_keys[1])));
            _jsonCommandResult = JsonSerializer.Serialize(_commandResult, ConstHelper.Options);
        }

        [Test]
        public void ByteArrayCommandResult()
        {
            var bytes = _commandResult.ToBytes();
            var newCommandResult = CommandResult.FromBytes(bytes);
            var newJsonCommandResult = JsonSerializer.Serialize(newCommandResult, ConstHelper.Options);
            
            Assert.AreEqual(_jsonCommandResult, newJsonCommandResult);
        }
    }
}