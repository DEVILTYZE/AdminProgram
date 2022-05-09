namespace CommandLib.Commands
{
    public class MessageCommand : AbstractCommand
    {
        public bool IsSystem { get; set; }
        
        public MessageCommand(object data, int openKey) 
            : base(ConstHelper.MessageCommandId, ConstHelper.MessageCommandString, data, openKey) { }

        public override CommandResult Execute() => new(CommandResultStatus.Successed, Data);
    }
}