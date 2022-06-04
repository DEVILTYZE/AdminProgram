using System.ComponentModel;
using System.Configuration.Install;

namespace AdminService
{
    public partial class AdminServiceInstaller : Installer
    {
        private IContainer components = null;
 
        private void InitializeComponent() => components = new Container();
    }
}