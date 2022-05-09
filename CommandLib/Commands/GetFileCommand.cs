namespace CommandLib.Commands
{
    public class GetFileCommand : AbstractCommand
    {
        public GetFileCommand(object data, int openKey) 
            : base(ConstHelper.GetFileCommandId, ConstHelper.GetFileCommandString, data, openKey) { }

        public override CommandResult Execute()
        {
            throw new System.NotImplementedException();
            
            // TODO: Сделать...
        }
    }
}