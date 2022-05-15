using System.Security.Cryptography;

namespace CommandLib.Commands
{
    public interface ICommand
    {
        RSAParameters RsaPublicKey { get; }
        object Data { set; }
        CommandResult Execute();
        byte[] ToBytes();
    }
}