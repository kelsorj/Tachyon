using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ReportingInterfaceTestApp
{
    public class ViewModel : DependencyObject
    {
        public ICommand LoadPluginsCommand { get; set; }
        public ICommand SendMessageCommand { get; set; }
        public ICommand SendErrorCommand { get; set; }

        // for V -> VM, can get away with simple .NET properties
        public string Message { get; set; }
        public string Error { get; set; }

        // for VM -> V, must use a DependencyProperty
        public string PluginPath
        {
            get { return (string)GetValue(PluginPathProperty); }
            set { SetValue(PluginPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PluginPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PluginPathProperty =
            DependencyProperty.Register( "PluginPath", typeof(string), typeof(ViewModel));

        public ViewModel()
        {
            PluginPath = @"C:\Code\trunk\vs2008\Shared\ReportingInterface\ReportingInterfaceTestApp\ReportingInterfaceTestApp\bin\Debug";
            this.LoadPluginsCommand = new LoadPluginsCommand( this);
            this.SendMessageCommand = new SendMessageCommand( this);
            this.SendErrorCommand = new SendErrorCommand( this);
        }

        public void LoadPlugins()
        {
            //_reporter.LoadReportingPlugins( PluginPath);
            //_reporter.Open( log_grid);
        }

    }
}
