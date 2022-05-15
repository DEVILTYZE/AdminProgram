using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using CommandLib.Commands;
using NUnit.Framework;
using SecurityChannel;

namespace Tests.CommandLibTests
{
    public class CommandResultTests
    {
        private RSAParameters[] _keys;
        private CommandResult _commandResult;
        private string _jsonCommandResult;
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
            { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        
        [SetUp]
        public void Setup()
        {
            _keys = RsaEngine.GetKeys();
            _commandResult = new CommandResult(CommandResultStatus.Successed, new RsaKey(_keys[1]));
            _jsonCommandResult = JsonSerializer.Serialize(_commandResult, _options);
        }

        [Test]
        public void ByteArrayCommandResult()
        {
            var bytes = _commandResult.ToBytes();
            var newCommandResult = CommandResult.FromBytes(bytes);
            var newJsonCommandResult = JsonSerializer.Serialize(newCommandResult, _options);
            
            Assert.AreEqual(_jsonCommandResult, newJsonCommandResult);
        }
    }
}