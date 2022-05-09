using System;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace CommandLib.Commands
{
    [Serializable]
    public abstract class AbstractCommand : ICommand, ISendable
    {
        private readonly int _id;
        private readonly string _commandName;
        private readonly Type _commandType;
        
        public object Data { get; }
        public int PublicKey { get; set; }

        protected AbstractCommand(int id, string command, object data, int openKey)
        {
            _id = id;
            _commandName = command;
            _commandType = GetType();
            Data = data;
            PublicKey = openKey;
        }

        public abstract CommandResult Execute();
        
        public byte[] ToBytes()
        {
            var json = JsonSerializer.Serialize(this);

            return Encoding.UTF8.GetBytes(json);
        }

        public static ICommand FromBytes(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var command = JsonSerializer.Deserialize<AbstractCommand>(json);
            
            return command?.ToOriginalType();
        }
        
        protected ICommand ToOriginalType() 
            => (ICommand)TypeDescriptor.GetConverter(_commandType).ConvertTo(this, _commandType);
    }
}