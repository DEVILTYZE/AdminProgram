using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using CommandLib.Annotations;
using SecurityChannel;

namespace CommandLib.Commands
{
    [Serializable]
    public enum CommandResultStatus
    {
        Unknown = 0,
        Successed = 1,
        Failed = -1
    }
    
    [Serializable]
    public class CommandResult : ISendable
    {
        public CommandResultStatus Status { get; }
        public byte[] Data { get; }
        public RsaKey PublicKey { get; set; }

        public CommandResult(CommandResultStatus status, [CanBeNull]byte[] data)
        {
            Status = status;
            Data = data;
        }

        public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, ConstHelper.Options);

        public static CommandResult FromBytes(byte[] data) 
            => JsonSerializer.Deserialize<CommandResult>(data, ConstHelper.Options);
    }
}