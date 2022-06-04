using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AdminProgramHost.Annotations;
using CommandLib;

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

            if (!NetHelper.AddFirewallRules("AdminService", "TCP", false, true) ||
                !NetHelper.AddFirewallRules("AdminService", "UDP", false, true))
                MessageBox.Show("Не добавились правила для брандмауэра", "Ошибка", MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            
            _model = new Host();
            DataContext = _model;
            var result = MessageBoxResult.None;
            
            if (!_model.SetAutorunValue(true))
                result = MessageBox.Show("Программа не смогла установиться в автозапуск.", "Ошибка");
            
            if (result is MessageBoxResult.OK or MessageBoxResult.Cancel)
                Close();
        }

        private void MainWindow_OnClosed([CanBeNull]object sender, EventArgs e) => _model.WaitTasks();

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _model.StartClientSession();
            Task.Run(EndClose);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) => _model.SetAutorunValue(false);

        private void EndClose()
        {
            while (_model.AreRunningTasks)
                Thread.Sleep(ConstHelper.SleepTimeout / 2);

            if (Application.Current is not null)
                Application.Current.Dispatcher.Invoke(Close);
        }
    }
}