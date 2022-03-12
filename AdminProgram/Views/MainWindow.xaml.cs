﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdminProgram.Models;
using AdminProgram.ViewModels;

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
        
        public MainWindow()
        {
            InitializeComponent();
            
            DataContext = _model = new HostViewModel();
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
                    if (!_model.Shutdown())
                        MessageBox.Show("On", "Status");
                    break;
                case HostStatus.Off:
                    if (!_model.PowerOn())
                        MessageBox.Show("Socket exception", "Error");
                    break;
                case HostStatus.Loading:
                    MessageBox.Show("Loading", "Status");
                    break;
                case HostStatus.Unknown:
                default:
                    MessageBox.Show("Unknown", "Status");
                    break;
            }
        }

        private static void Scan(HostViewModel hostViewModel)
        {
            hostViewModel.IsScanButtonEnabled = false;
            
            if (!hostViewModel.Scan())
                MessageBox.Show("Scan error", "Error");
        }

        private void HostList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            => RightPanel.Visibility = Visibility.Visible;
        

        private void RefreshAllButton_OnClick(object sender, RoutedEventArgs e)
        {
            _model.IsRefreshButtonEnabled = false;
            _model.Refresh();
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e) => HostViewModel.Refresh(_model.SelectedHost);

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F6 && RefreshAllButton.IsEnabled)
            {
                _model.IsRefreshButtonEnabled = false;
                _model.Refresh();
                return;
            }

            if (e.Key != Key.F5 || !ScanButton.IsEnabled) 
                return;
            
            _model.IsScanButtonEnabled = false;
            _model.Scan();
        }
    }
}