using System;
using System.Net;
using System.Windows;
using System.Windows.Data;
using AdminProgram.Models;
using AdminProgram.ViewModels;

namespace AdminProgram.Views
{
    public partial class RemoteWindow
    {
        private readonly RemoteViewModel _model;
        
        public RemoteWindow(Host host, IPEndPoint ourIpEndPoint)
        {
            InitializeComponent();

            _model = new RemoteViewModel(host, ourIpEndPoint);
            DataContext = _model;
        }

        private void RemoteWindow_OnLoaded(object sender, RoutedEventArgs e) => _model.StartRemoteConnection();

        private void RemoteWindow_OnClosed(object sender, EventArgs e) => _model.CloseRemoteConnection();

        private void ScreenImage_OnSourceUpdated(object sender, DataTransferEventArgs e)
        {
            var size = _model.WindowSize;
            ScreenImage.Height = size.Height;
            ScreenImage.Width = size.Width;
        }
    }
}