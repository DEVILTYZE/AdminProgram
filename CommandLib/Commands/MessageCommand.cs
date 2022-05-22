using System;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using CommandLib.Commands.Helpers;

namespace CommandLib.Commands
{
    [Serializable]
    public class MessageCommand : AbstractCommand
    {
        public bool IsSystem { get; set; }

        [JsonConstructor]
        public MessageCommand() { }

        public MessageCommand(byte[] data, RSAParameters? publicKey = null) 
            : base(ConstHelper.MessageCommandId, ConstHelper.MessageCommandString, data, publicKey) { }

        public override CommandResult Execute() => new(CommandResultStatus.Successed, Data);
    }
}