namespace CommandLib.Commands
{
    public interface ISendable
    {
        int PublicKey { get; set; }
        byte[] ToBytes();
    }
}