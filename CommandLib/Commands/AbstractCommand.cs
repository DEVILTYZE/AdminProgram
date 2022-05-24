using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLib.Commands.Helpers;
using SecurityChannel;

namespace CommandLib.Commands
{
    [Serializable]
    public abstract class AbstractCommand : ICommand, ISendable
    {
        public int Id { get; set; }
        public string CommandName { get; set; }
        public RsaKey PublicKey { get; set; }
        public byte[] Data { get; set; }
        public CommandType Type { get; set; }

        [JsonIgnore] 
        public RSAParameters? RsaPublicKey => PublicKey?.GetKey();

        [JsonConstructor]
        protected AbstractCommand() { }
        
        protected AbstractCommand(int id, string commandName, byte[] data, RSAParameters? publicKey)
        {
            Id = id;
            CommandName = commandName;
            Data = data;
            PublicKey = publicKey.HasValue ? new RsaKey(publicKey.Value) : null;
            Type = CommandType.Execute;
        }

        public abstract CommandResult Execute();

        public virtual void Abort() { /* ignored */ }

        public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, ConstHelper.Options);

        public static ICommand FromBytes(byte[] data, Type type)
            => (ICommand)JsonSerializer.Deserialize(data, type, ConstHelper.Options);
    }
}