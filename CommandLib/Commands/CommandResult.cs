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
    public class CommandResult
    {
        public CommandResultStatus Result { get; }
        public object Data { get; }

        public CommandResult(CommandResultStatus result, [CanBeNull]object data)
        {
            Result = result;
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