using System.Security.Cryptography;

namespace CommandLib.Commands.Helpers
{
    public interface ICommand
    {
        RSAParameters? RsaPublicKey { get; }
        byte[] Data { get; }
        CommandResult Execute();
        void Abort();
        byte[] ToBytes();
    }
}