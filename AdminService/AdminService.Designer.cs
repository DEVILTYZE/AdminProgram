using System.ComponentModel;

namespace AdminService
{
    public partial class AdminService
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components is not null)
                components.Dispose();
            
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            ServiceName = "AdminService";
        }
    }
}