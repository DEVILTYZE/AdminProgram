using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SecurityChannel;

namespace CommandLib.Commands
{
    [Serializable]
    public abstract class AbstractCommand : ICommand, ISendable
    {
        [JsonIgnore]
        private Type CommandType => Type.GetType(CommandTypeName) ?? typeof(AbstractCommand);
        
        public int Id { get; set; }
        public string CommandName { get; set; }
        public string CommandTypeName { get; set; }
        public RsaKey PublicKey { get; set; }
        
        [JsonIgnore]
        public RSAParameters RsaPublicKey { get; set; }
        [JsonIgnore]
        public object Data { get; }

        [JsonConstructor]
        protected AbstractCommand() { }

        protected AbstractCommand(int id, string commandName, string commandTypeName, object data, RsaKey publicKey)
        {
            Id = id;
            CommandName = commandName;
            CommandTypeName = commandTypeName;
            Data = data;
            PublicKey = publicKey;
        }
        
        protected AbstractCommand(int id, string commandName, object data, RSAParameters publicKey)
        {
            Id = id;
            CommandName = commandName;
            CommandTypeName = GetType().FullName;
            Data = data;
            PublicKey = new RsaKey(publicKey);
        }

        public virtual CommandResult Execute() => new(CommandResultStatus.Failed, null);

        public byte[] ToBytes()
        {
            var json = JsonSerializer.Serialize(this);

            return Encoding.Default.GetBytes(json);
        }

        public static ICommand FromBytes(byte[] data, Type type)
        {
            var json = Encoding.Default.GetString(data);
            var command = (ICommand)JsonSerializer.Deserialize(json, type);
            
            return command;
        }
    }
}