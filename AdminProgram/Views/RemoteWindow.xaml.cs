using System;
using System.Net;
using System.Windows;
using AdminProgram.Models;
using AdminProgram.ViewModels;

namespace AdminProgram.Views
{
    public partial class RemoteWindow
    {
        private readonly RemoteViewModel _model;
        private readonly MainWindow.ChangeStatusDelegate _changeStatus;
        
        public RemoteWindow(Host host, IPEndPoint ourIpEndPoint, MainWindow.ChangeStatusDelegate changeStatus)
        {
            InitializeComponent();
            _changeStatus = changeStatus;

            _model = new RemoteViewModel(host, ourIpEndPoint);
            DataContext = _model;
        }

        private void RemoteWindow_OnLoaded(object sender, RoutedEventArgs e) => _model.StartRemoteConnection();

        private void RemoteWindow_OnClosed(object sender, EventArgs e)
        {
            _model.CloseRemoteConnection();
            _changeStatus?.Invoke(true);
        }
    }
}