using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ReportingInterfaceTestApp
{
    class SendErrorCommand : ICommand
    {
        private readonly ViewModel _vm;

        public SendErrorCommand( ViewModel vm)
        {
            _vm = vm;
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return !string.IsNullOrEmpty( _vm.Error);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            
        }

        #endregion
    }
}
