using System;
using System.Windows;
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
            var result = MessageBoxResult.None;
            
            if (!_model.SetAutorunValue(true))
                result = MessageBox.Show("Программа не смогла установиться в автозапуск.", "Ошибка");
            
            if (result is MessageBoxResult.OK or MessageBoxResult.Cancel)
                Close();
        }

        private void MainWindow_OnClosed([CanBeNull]object sender, EventArgs e) => _model.WaitThread();

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) => _model.StartClientSession();

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) => _model.SetAutorunValue(false);
    }
}