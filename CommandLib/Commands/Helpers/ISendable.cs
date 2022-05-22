using SecurityChannel;

namespace CommandLib.Commands.Helpers
{
    public interface ISendable
    {
        RsaKey PublicKey { get; set; }
        byte[] ToBytes();
    }
}