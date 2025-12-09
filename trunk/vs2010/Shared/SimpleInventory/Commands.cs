using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace BioNex.Shared.SimpleInventory
{
    public class ApplyInventoryChangesCommand : ICommand
    {
        #region ICommand Members

        private InventoryBackend _inventory;

        public ApplyInventoryChangesCommand( InventoryBackend inventory)
        {
            _inventory = inventory;
        }

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
            _inventory.Commit();
        }

        #endregion
    }
    public class AddNewPlateCommand : ICommand
    {
        #region ICommand Members

        private InventoryBackend _inventory;

        public AddNewPlateCommand( InventoryBackend inventory)
        {
            _inventory = inventory;
        }

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
            _inventory.AddPlate();
        }

        #endregion
    }
    public class ReloadInventoryCommand : ICommand
    {
        #region ICommand Members

        private InventoryBackend _inventory;

        public ReloadInventoryCommand( InventoryBackend inventory)
        {
            _inventory = inventory;
        }

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
            _inventory.Reload();
        }

        #endregion
    }
    public class DeletePlateCommand : ICommand
    {
        #region ICommand Members

        private InventoryBackend _inventory;

        public DeletePlateCommand( InventoryBackend inventory)
        {
            _inventory = inventory;
        }

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
            _inventory.DeleteSelectedPlate();
        }

        #endregion
    }

    public abstract partial class InventoryBackend
    {
        public ICommand ApplyInventoryChangesCommand { get; set; }
        public ICommand AddNewPlateCommand { get; set; }
        public ICommand ReloadInventoryCommand { get; set; }
        public ICommand DeletePlateCommand { get; set; }

        protected void InitializeCommands()
        {
            ApplyInventoryChangesCommand = new ApplyInventoryChangesCommand( this);
            AddNewPlateCommand = new AddNewPlateCommand( this);
            ReloadInventoryCommand = new ReloadInventoryCommand( this);
            DeletePlateCommand = new DeletePlateCommand( this);
        }
    }

}
