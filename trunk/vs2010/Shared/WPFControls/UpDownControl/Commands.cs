using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace BioNex.Shared.WPFControls
{
    public class IncrementCommand : ICommand
    {
        UpDown _vm { get; set; }

        public IncrementCommand( UpDown viewmodel)
        {
            _vm = viewmodel;
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
 	        return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
 	        _vm.Increment();
        }

        #endregion
    }

    public class DecrementCommand : ICommand
    {
        UpDown _vm { get; set; }

        public DecrementCommand( UpDown viewmodel)
        {
            _vm = viewmodel;
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
 	        return _vm.Number > 1;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }


        public void Execute(object parameter)
        {
 	        _vm.Decrement();
        }

        #endregion
    }

    public partial class UpDown
    {
        private void InitializeCommands()
        {
            IncrementCommand = new IncrementCommand( this);
            DecrementCommand = new DecrementCommand( this);
        }
    }
}
