using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
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

        private void Screen_OnKeyDown(object sender, KeyEventArgs e)
        {
            _model.CurrentControlState.Key = (byte)e.Key;
        }

        private void Screen_OnMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            _model.CurrentControlState.MouseX = position.X;
            _model.CurrentControlState.MouseY = position.Y;
        }

        private void Screen_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _model.CurrentControlState.Delta = e.Delta;
        }

        private void Screen_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _model.CurrentControlState.LeftButtonClickCount = (byte)e.ClickCount;
        }

        private void Screen_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _model.CurrentControlState.RightButtonClickCount = (byte)e.ClickCount;
        }
    }
}