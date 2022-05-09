namespace CommandLib.Commands
{
    public class ShutdownCommand : AbstractCommand
    {
        public ShutdownCommand(object data, int openKey) 
            : base(ConstHelper.ShutdownCommandId, ConstHelper.ShutdownCommandString, data, openKey) { }

        public override CommandResult Execute()
        {
            throw new System.NotImplementedException();
            
            // TODO: Сделать...
        }
    }
}