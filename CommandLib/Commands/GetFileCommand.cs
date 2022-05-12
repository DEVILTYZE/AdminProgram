using System;
using System.Security.Cryptography;

namespace CommandLib.Commands
{
    [Serializable]
    public class GetFileCommand : AbstractCommand
    {
        public GetFileCommand(object data, RSAParameters publicKey) 
            : base(ConstHelper.GetFileCommandId, ConstHelper.GetFileCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            throw new System.NotImplementedException();
            
            // TODO: Сделать...
        }
    }
}