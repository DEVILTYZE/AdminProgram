using System;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using SecurityChannel;

namespace CommandLib.Commands
{
    [Serializable]
    public class MessageCommand : AbstractCommand
    {
        public bool IsSystem { get; set; }

        [JsonConstructor]
        public MessageCommand() { }

        public MessageCommand(object data, RSAParameters publicKey) 
            : base(ConstHelper.MessageCommandId, ConstHelper.MessageCommandString, data, publicKey) { }

        public override CommandResult Execute() => new(CommandResultStatus.Successed, Data);
    }
}