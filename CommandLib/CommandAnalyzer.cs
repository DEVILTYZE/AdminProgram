using System;
using CommandLib.Annotations;
using CommandLib.Commands;

namespace CommandLib
{
    public static class CommandAnalyzer
    {
        public static CommandResult ExecuteCommand(int commandId, [CanBeNull]object data)
        {
            ICommand command = commandId switch
            {
                ConstHelper.GetMessageCommandId => new GetMessageCommand(),
                _ => throw new Exception("Bad command.")
            };

            return command.Execute(data);
        }
    }
}