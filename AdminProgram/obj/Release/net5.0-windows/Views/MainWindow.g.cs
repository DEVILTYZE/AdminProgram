﻿#pragma checksum "..\..\..\..\Views\MainWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "889441EB50D13AD2578EB4A6B795ED6F8E272095"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using AdminProgram.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace AdminProgram.Views {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 45 "..\..\..\..\Views\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button ScanButton;
        
        #line default
        #line hidden
        
        
        #line 53 "..\..\..\..\Views\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button RefreshAllButton;
        
        #line default
        #line hidden
        
        
        #line 66 "..\..\..\..\Views\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SearchBox;
        
        #line default
        #line hidden
        
        
        #line 72 "..\..\..\..\Views\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox HostsBox;
        
        #line default
        #line hidden
        
        
        #line 94 "..\..\..\..\Views\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel RightPanel;
        
        #line default
        #line hidden
        
        
        #line 125 "..\..\..\..\Views\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button RemoteButton;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "5.0.11.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/AdminProgram;component/views/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Views\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "5.0.11.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 11 "..\..\..\..\Views\MainWindow.xaml"
            ((AdminProgram.Views.MainWindow)(target)).KeyDown += new System.Windows.Input.KeyEventHandler(this.MainWindow_OnKeyDown);
            
            #line default
            #line hidden
            
            #line 12 "..\..\..\..\Views\MainWindow.xaml"
            ((AdminProgram.Views.MainWindow)(target)).Closed += new System.EventHandler(this.MainWindow_OnClosed);
            
            #line default
            #line hidden
            return;
            case 2:
            
            #line 43 "..\..\..\..\Views\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.AddHostButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 3:
            this.ScanButton = ((System.Windows.Controls.Button)(target));
            
            #line 51 "..\..\..\..\Views\MainWindow.xaml"
            this.ScanButton.Click += new System.Windows.RoutedEventHandler(this.ScanButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 4:
            this.RefreshAllButton = ((System.Windows.Controls.Button)(target));
            
            #line 59 "..\..\..\..\Views\MainWindow.xaml"
            this.RefreshAllButton.Click += new System.Windows.RoutedEventHandler(this.RefreshAllButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 5:
            this.SearchBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 68 "..\..\..\..\Views\MainWindow.xaml"
            this.SearchBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.SearchBox_OnTextChanged);
            
            #line default
            #line hidden
            return;
            case 6:
            this.HostsBox = ((System.Windows.Controls.ListBox)(target));
            
            #line 77 "..\..\..\..\Views\MainWindow.xaml"
            this.HostsBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.HostList_OnSelectionChanged);
            
            #line default
            #line hidden
            return;
            case 7:
            this.RightPanel = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 8:
            
            #line 116 "..\..\..\..\Views\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.PowerButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 9:
            
            #line 121 "..\..\..\..\Views\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.RefreshButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 10:
            
            #line 122 "..\..\..\..\Views\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.RemoveHostButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 11:
            this.RemoteButton = ((System.Windows.Controls.Button)(target));
            
            #line 128 "..\..\..\..\Views\MainWindow.xaml"
            this.RemoteButton.Click += new System.Windows.RoutedEventHandler(this.RemoteButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 12:
            
            #line 135 "..\..\..\..\Views\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.TransferButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 13:
            
            #line 158 "..\..\..\..\Views\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ExportButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 14:
            
            #line 163 "..\..\..\..\Views\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ImportButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 15:
            
            #line 168 "..\..\..\..\Views\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ClearLogsButton_OnClick);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

