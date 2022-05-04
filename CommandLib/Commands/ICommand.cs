using CommandLib.Annotations;

namespace CommandLib.Commands
{
    public interface ICommand
    {
        CommandResult Execute([CanBeNull]object data);
    }
}