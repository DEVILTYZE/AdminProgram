using System;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SecurityChannel;

namespace CommandLib.Commands
{
    [Serializable]
    public abstract class AbstractCommand : ICommand, ISendable
    {
        private static JsonSerializerOptions _options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        
        public int Id { get; set; }
        public string CommandName { get; set; }
        public RsaKey PublicKey { get; set; }

        [JsonIgnore] 
        public RSAParameters RsaPublicKey => PublicKey.GetKey();
        
        [JsonIgnore]
        public object Data { get; set; }

        [JsonConstructor]
        protected AbstractCommand() { }
        
        protected AbstractCommand(int id, string commandName, object data, RSAParameters publicKey)
        {
            Id = id;
            CommandName = commandName;
            Data = data;
            PublicKey = new RsaKey(publicKey);
        }

        public abstract CommandResult Execute();

        public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, _options);

        public static ICommand FromBytes(byte[] data, Type type) =>
            (ICommand)JsonSerializer.Deserialize(data, type, _options);
    }
}