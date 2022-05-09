namespace CommandLib.Commands
{
    public interface ICommand
    {
        CommandResult Execute();
        byte[] ToBytes();
    }
}