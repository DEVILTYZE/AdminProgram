using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdminProgram.ViewModels;
using CommandLib;

namespace AdminProgram.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly HostViewModel _model;
        private readonly Style _searchEmpty = Application.Current.FindResource("SearchEmpty") as Style;
        private readonly Style _searchNotEmpty = new()
            { Setters = { new Setter { Property = ContentProperty, Value = string.Empty } } };

        private Task _task;
        
        public delegate void ChangeStatusDelegate(bool isEnabled);
        public event ChangeStatusDelegate ChangeRemoteStatus;
        
        public MainWindow()
        {
            InitializeComponent();
            ChangeRemoteStatus += ChangeRemoteButtonStatus;
            
            _model = (HostViewModel)DataContext;
            Scan(_model);
        }

        private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
            => SearchLabel.Style = SearchBox.Text.Length is 0 ? _searchEmpty : _searchNotEmpty;

        private void SearchLabel_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount is 1)
                Keyboard.Focus(((Label)sender).Target);
        }

        private void ScanButton_OnClick(object sender, RoutedEventArgs e) => Scan(_model);

        private void PowerButton_OnClick(object sender, RoutedEventArgs e)
        {
            switch (_model.SelectedHost.Status)
            {
                case HostStatus.On:
                    _model.Shutdown();
                    break;
                case HostStatus.Off:
                    _model.PowerOn();
                    break;
                case HostStatus.Loading:
                case HostStatus.Unknown:
                default:
                    MessageBox.Show("Ошибка switch.");
                    break;
            }
        }

        private static void Scan(HostViewModel hostViewModel)
        {
            if (!hostViewModel.Scan())
                MessageBox.Show("Scan error", "Error");
        }

        private void HostList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            => RightPanel.Visibility = Visibility.Visible;
        
        private void RefreshAllButton_OnClick(object sender, RoutedEventArgs e) => _model.Refresh();
        
        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
            => _task = Task.Run(() => HostViewModel.Refresh(_model.SelectedHost));
        

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F6 && RefreshAllButton.IsEnabled)
            {
                _model.Refresh();
                return;
            }

            if (e.Key != Key.F5 || !ScanButton.IsEnabled) 
                return;
            
            _model.Scan();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (_task is not null && !_task.IsCompleted)
                _task.Wait();
            
            _model.WaitTasks();
        }

        private void RemoteButton_OnClick(object sender, RoutedEventArgs e)
        {
            ChangeRemoteStatus?.Invoke(false);
            var remoteWindow = new RemoteWindow(_model.SelectedHost, _model.GetOurIpEndPoint(), ChangeRemoteStatus)
            {
                Owner = this
            };
            remoteWindow.ShowDialog();
        }

        private void ChangeRemoteButtonStatus(bool isEnabled) => RemoteButton.IsEnabled = isEnabled;

        private void TransferButton_OnClick(object sender, RoutedEventArgs e)
        {
            switch (_model.SelectedHost.IsTransfers)
            {
                case false:
                    if (!_model.TransferFiles())
                        MessageBox.Show("Неверный путь до файла.");
                    
                    break;
                case true:
                    _model.CloseTransfer();
                    break;
            }
        }
    }
}