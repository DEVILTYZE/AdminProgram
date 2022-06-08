using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AdminProgram.Models;
using AdminProgram.ViewModels;
using WindowsInput.Native;

namespace AdminProgram.Views
{
    public partial class RemoteWindow
    {
        private readonly RemoteViewModel _model;
        private readonly MainWindow.ChangeStatusDelegate _changeStatus;
        private readonly object _locker = new();
        private const int MaxAbsoluteLength = 65535;

        public RemoteWindow(Host host, IPEndPoint ourIpEndPoint, MainWindow.ChangeStatusDelegate changeStatus, 
            LogViewModel logModel)
        {
            InitializeComponent();
            _changeStatus = changeStatus;
            _model = new RemoteViewModel(host, ourIpEndPoint, logModel);
            DataContext = _model;
            
            _model.ImageScreen = new BitmapImage(new Uri(_model.ImageSourcePath));
            var binding = new Binding { Path = new PropertyPath("ImageScreen") };
            ScreenImage.SetBinding(Image.SourceProperty, binding);
        }

        private void RemoteWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Title = $"Admin Program (подключено к {_model.Host.IpAddress})";
            _model.StartRemoteConnection();
        }

        private void RemoteWindow_OnClosed(object sender, EventArgs e)
        {
            if (!_model.CloseRemoteConnection())
            {
                MessageBox.Show("Что-то пошло не так при закрытии окна...", "Ошибка");
                return;
            }
            
            _changeStatus?.Invoke(true);
        }

        private void ScreenImage_OnKeyDown(object sender, KeyEventArgs e)
        {
            var code = (VirtualKeyCode)KeyInterop.VirtualKeyFromKey(e.Key);
            
            lock (_locker)
            {
                _model.CurrentControlState.KeysQueue.Enqueue((byte)code);
                _model.CurrentControlState.StatesQueue.Enqueue((byte)e.KeyStates);
            }
        } 
        
        private void ScreenImage_OnMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(ScreenImage);
            _model.CurrentControlState.MouseX = position.X * MaxAbsoluteLength / ScreenImage.ActualWidth;
            _model.CurrentControlState.MouseY = position.Y * MaxAbsoluteLength / ScreenImage.ActualHeight;
        }

        private void ScreenImage_OnMouseWheel(object sender, MouseWheelEventArgs e)
            => _model.CurrentControlState.Delta = e.Delta;

        private void ScreenImage_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _model.CurrentControlState.LeftButtonClickCount = (byte)e.ClickCount;
            _model.CurrentControlState.LeftButtonIsPressed = false;
        }
        
        private void ScreenImage_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _model.CurrentControlState.RightButtonClickCount = (byte)e.ClickCount;
            _model.CurrentControlState.RightButtonIsPressed = false;
        }

        private void ScreenImage_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _model.CurrentControlState.LeftButtonClickCount = (byte)e.ClickCount;
            _model.CurrentControlState.LeftButtonIsPressed = e.ButtonState == MouseButtonState.Pressed;
        }

        private void ScreenImage_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _model.CurrentControlState.RightButtonClickCount = (byte)e.ClickCount;
            _model.CurrentControlState.RightButtonIsPressed = e.ButtonState == MouseButtonState.Pressed;
        }
    }
}