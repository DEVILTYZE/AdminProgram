namespace CommandLib.Commands
{
    public abstract class AbstractCommand : ICommand
    {
        private readonly int _id;
        private readonly string _commandName;

        protected AbstractCommand(int id, string command)
        {
            _id = id;
            _commandName = command;
        }

        public abstract CommandResult Execute(object data);
    }
}