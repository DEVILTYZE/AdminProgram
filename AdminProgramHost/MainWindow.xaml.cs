using System;
using AdminProgramHost.Annotations;

namespace AdminProgramHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Host _model;
        
        public MainWindow()
        {
            InitializeComponent();

            _model = new Host();
            DataContext = _model;
        }

        private void MainWindow_OnClosed([CanBeNull]object sender, EventArgs e) => _model.WaitThread();
    }
}