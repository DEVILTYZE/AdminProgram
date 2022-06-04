using System.ComponentModel;
using System.ServiceProcess;

namespace AdminService
{
    [RunInstaller(true)]
    public partial class AdminServiceInstaller
    {
        public AdminServiceInstaller()
        {
            InitializeComponent();
            var processInstaller = new ServiceProcessInstaller { Account = ServiceAccount.LocalSystem };
            var serviceInstaller = new ServiceInstaller
            {
                StartType = ServiceStartMode.Automatic,
                ServiceName = "AdminService"
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}