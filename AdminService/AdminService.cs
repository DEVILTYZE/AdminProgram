using System.ServiceProcess;

namespace AdminService
{
    public partial class AdminService : ServiceBase
    {
        private readonly Host _model;
        
        public AdminService()
        {
            InitializeComponent();
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = true;
            _model = new Host();
        }

        protected override void OnStart(string[] args) => _model.StartClientSession();
        
        protected override void OnContinue() => _model.StartClientSession();

        protected override void OnStop()
        {
            if (_model.AreRunningTasks)
                _model.WaitTasks();
        }

        protected override void OnShutdown()
        {
            if (_model.AreRunningTasks)
                _model.WaitTasks();
        }

        protected override void OnPause()
        {
            if (_model.AreRunningTasks)
                _model.WaitTasks();
        }
    }
}