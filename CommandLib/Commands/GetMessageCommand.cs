namespace CommandLib.Commands
{
    public class GetMessageCommand : AbstractCommand
    {
        public GetMessageCommand() : base(ConstHelper.GetMessageCommandId, ConstHelper.GetMessageCommandString) { }

        public override CommandResult Execute(object data) => new(CommandResultStatus.Successed, data);
    }
}