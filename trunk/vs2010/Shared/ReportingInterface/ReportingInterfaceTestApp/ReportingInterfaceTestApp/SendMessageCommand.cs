using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ReportingInterfaceTestApp
{
    class SendMessageCommand : ICommand
    {
        private readonly ViewModel _vm;

        public SendMessageCommand( ViewModel vm)
        {
            _vm = vm;
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return !string.IsNullOrEmpty( _vm.Message);
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
