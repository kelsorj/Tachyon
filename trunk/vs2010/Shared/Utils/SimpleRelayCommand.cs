using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace BioNex.Shared.Utils
{
    public class SimpleRelayCommand : ICommand
    {
        private Action _execute;
        private Func<bool> _can_execute;

        public SimpleRelayCommand( Action execute, Func<bool> can_execute=null)
        {
            _execute = execute;
            _can_execute = can_execute;
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            if( _can_execute == null)
                return true;
            return _can_execute();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        #endregion
    }
}
