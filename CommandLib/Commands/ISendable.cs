using SecurityChannel;

namespace CommandLib.Commands
{
    public interface ISendable
    {
        RsaKey PublicKey { get; set; }
        byte[] ToBytes();
    }
}