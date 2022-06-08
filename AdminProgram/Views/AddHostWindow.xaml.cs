using System;
using System.Net;
using System.Windows;
using AdminProgram.Models;
using CommandLib;

namespace AdminProgram.Views
{
    public partial class AddHostWindow
    {
        private readonly Host _newHost;
        
        public AddHostWindow(ref Host emptyHost)
        {
            InitializeComponent();
            _newHost = emptyHost;
        }

        private void AddHostButton_OnClick(object sender, RoutedEventArgs e)
        {
            _newHost.Name = "Неизвестно";

            try
            {
                _newHost.IpAddress = string.IsNullOrEmpty(IpAddressBox.Text) ? "127.0.0.1" : IpAddressBox.Text;
            }
            catch (Exception)
            {
                MessageBox.Show("Некорректный IP-адрес. Следует писать IP-адрес по такому шаблону " +
                    "X.X.X.X, где X — число от 0 до 255", "Ошибка", MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                
                return;
            }

            if (!NetHelper.IsInLocalNetwork(IPAddress.Parse(_newHost.IpAddress)))
            {
                MessageBox.Show("IP-адрес не находится в локальной подсети.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                return;
            }
            
            try
            {
                _newHost.MacAddress = string.IsNullOrEmpty(MacAddressBox.Text) ? "00-00-00-00-00-00" : MacAddressBox.Text;
            }
            catch (Exception)
            {
                MessageBox.Show("Некорректный MAC-адрес. Следует писать MAC-адрес по такому шаблону " +
                    "XX-XX-XX-XX-XX-XX, где X — число от 0 до F в шестнадцатеричной системе счисления.", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                
                return;
            }
            
            _newHost.Status = HostStatus.Unknown;
            Close();
        }
    }
}