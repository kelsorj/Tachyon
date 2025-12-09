using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace BeeSureTestApp
{
    /////////////////////////////////////////////////
    // commands for main window
    /////////////////////////////////////////////////
    /// <summary>
    /// This command just adds another BeeSurePanel and sets its index
    /// </summary>
    public class AddBeeSureCommand : ICommand
    {
        MainWindow _main;

        public AddBeeSureCommand( MainWindow main)
        {
            _main = main;
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
            _main.AddBeeSure();
        }

        #endregion
    }

    /////////////////////////////////////////////////
    // commands for BeeSurePanel
    /////////////////////////////////////////////////
    public class InitializeCommand : ICommand
    {
        private BeeSurePanel _panel;

        public InitializeCommand( BeeSurePanel panel)
        {
            _panel = panel;
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _panel.ExecuteInitialize();
        }

        #endregion
    }
}
