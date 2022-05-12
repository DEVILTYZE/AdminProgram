using System;
using System.Security.Cryptography;

namespace CommandLib.Commands
{
    [Serializable]
    public class StreamCommand : AbstractCommand
    {
        public StreamCommand(object data, RSAParameters publicKey) 
            : base(ConstHelper.StreamCommandId, ConstHelper.StreamCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            throw new System.NotImplementedException();
            
            // TODO: Сделать...
        }
    }
}