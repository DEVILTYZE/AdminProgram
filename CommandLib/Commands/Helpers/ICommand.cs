using System.Security.Cryptography;

namespace CommandLib.Commands.Helpers
{
    public enum CommandType
    {
        Execute = 1,
        Abort = -1
    }
    
    public interface ICommand
    {
        RSAParameters? RsaPublicKey { get; }
        byte[] Data { get; }
        CommandType Type { get; set; }
        CommandResult Execute();
        void Abort();
        byte[] ToBytes();
    }
}