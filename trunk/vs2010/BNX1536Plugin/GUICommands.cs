using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows;

namespace BioNex.BNX1536Plugin
{
    public class RunProgramCommand : ICommand
    {
        public ViewModel _vm;

        public RunProgramCommand( ViewModel vm)
        {
            _vm = vm;
        }

        #region ICommand Members

        [DebuggerStepThrough]
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
            _vm.RunSelectedProgram();
        }

        #endregion
    }

    public class RunServiceProgramCommand : ICommand
    {
        private ViewModel _vm;

        public RunServiceProgramCommand( ViewModel vm)
        {
            _vm = vm;
        }

        #region ICommand Members

        [DebuggerStepThrough]
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
            _vm.RunSelectedServiceProgram();
        }

        #endregion
    }

    public partial class ViewModel
    {
        public RunProgramCommand RunProgramCommand { get; set; }
        public RunServiceProgramCommand RunServiceProgramCommand { get; set; }

        public void InitializeCommands()
        {
            RunProgramCommand = new RunProgramCommand( this);
            RunServiceProgramCommand = new RunServiceProgramCommand( this);
        }
    }
}
