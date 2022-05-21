using System;
using System.Security.Cryptography;

namespace CommandLib.Commands
{
    [Serializable]
    public class ShutdownCommand : AbstractCommand
    {
        public ShutdownCommand(byte[] data, RSAParameters? publicKey = null) 
            : base(ConstHelper.ShutdownCommandId, ConstHelper.ShutdownCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            throw new System.NotImplementedException();
            
            // TODO: Сделать...
        }
    }
}