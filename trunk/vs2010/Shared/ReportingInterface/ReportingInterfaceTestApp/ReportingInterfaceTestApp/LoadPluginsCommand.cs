using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using BioNex.ReportingInterface;

namespace ReportingInterfaceTestApp
{
    class LoadPluginsCommand : ICommand
    {
        private readonly ViewModel _vm;
        Reporter _reporter = new Reporter();

        #region ICommand Members

        public LoadPluginsCommand( ViewModel vm)
        {
            _vm = vm;
        }

        public bool CanExecute(object parameter)
        {
            return !string.IsNullOrEmpty( _vm.PluginPath);
        }

        public event EventHandler CanExecuteChanged
        {
            // remember that when we bind to an ICommand, WPF won't handle the CanExecute check,
            // so here we have to add and remove it from the registration system (CommandManager)
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _vm.LoadPlugins();
        }

        #endregion
    }
}
