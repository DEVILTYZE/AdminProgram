using System.ComponentModel;
using System.Net;
using System.Windows;
using AdminProgram.Models;
using CommandLib;

namespace AdminProgram.Views
{
    public partial class AddHostWindow
    {
        private Host _newHost;
        
        public AddHostWindow(ref Host emptyHost)
        {
            InitializeComponent();
            _newHost = emptyHost;
        }

        private void AddHostButton_OnClick(object sender, RoutedEventArgs e)
        {
            _newHost.Name = "Неизвестно";
            _newHost.IpAddress = string.IsNullOrEmpty(IpAddressBox.Text) ? "127.0.0.1" : IpAddressBox.Text;
            
            if (!NetHelper.IsInLocalNetwork(IPAddress.Parse(_newHost.IpAddress)))
                _newHost.Name = "1";
            
            _newHost.MacAddress = string.IsNullOrEmpty(MacAddressBox.Text) ? "00-00-00-00-00-00" : MacAddressBox.Text;
            _newHost.Status = HostStatus.Unknown;
            Close();
        }

        private void AddHostWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (string.CompareOrdinal(_newHost.Name, "1") == 0)
                _newHost = null;
        }
    }
}