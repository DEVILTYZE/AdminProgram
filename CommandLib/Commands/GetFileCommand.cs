using System;
using System.Security.Cryptography;

namespace CommandLib.Commands
{
    [Serializable]
    public class GetFileCommand : AbstractCommand
    {
        public GetFileCommand(byte[] data, RSAParameters? publicKey = null) 
            : base(ConstHelper.GetFileCommandId, ConstHelper.GetFileCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            throw new System.NotImplementedException();
            
            // TODO: Сделать...
        }
    }
}