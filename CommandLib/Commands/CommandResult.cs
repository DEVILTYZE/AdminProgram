using System;
using System.Text;
using System.Text.Json;
using CommandLib.Annotations;

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
        public object Data { get; }
        public int PublicKey { get; set; }

        public CommandResult(CommandResultStatus status, [CanBeNull]object data)
        {
            Status = status;
            Data = data;
        }

        public byte[] ToBytes()
        {
            var json = JsonSerializer.Serialize(this);
            
            return Encoding.UTF8.GetBytes(json);
        }

        public static CommandResult FromBytes(byte[] data)
        {
            var str = Encoding.UTF8.GetString(data);

            return JsonSerializer.Deserialize<CommandResult>(str);
        }
    }
}