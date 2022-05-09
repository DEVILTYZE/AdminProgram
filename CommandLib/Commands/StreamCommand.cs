namespace CommandLib.Commands
{
    public class StreamCommand : AbstractCommand
    {
        public StreamCommand(object data, int openKey) 
            : base(ConstHelper.StreamCommandId, ConstHelper.StreamCommandString, data, openKey) { }

        public override CommandResult Execute()
        {
            throw new System.NotImplementedException();
            
            // TODO: Сделать...
        }
    }
}