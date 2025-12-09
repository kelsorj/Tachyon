using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Command;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.Shared.LabwareDatabase
{
    /// <summary>
    /// Interaction logic for AddLabwareDialog.xaml
    /// </summary>
    public partial class RenameLabwareDialog : Window
    {
        public string LabwareName { get; set; }
        private string _old_name { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
        private ILabwareDatabase _labware_database { get; set; }

        public RenameLabwareDialog( ILabwareDatabase labware_database, string old_name)
        {
            InitializeComponent();
            OkCommand = new RelayCommand( ExecuteOk, () => { return LabwareName != ""; });
            CancelCommand = new RelayCommand( ExecuteCancel);
            _labware_database = labware_database;
            _old_name = old_name;
            // also set LabwareName so we can auto-populate the field
            LabwareName = _old_name;

            DataContext = this;
        }

        private void ExecuteOk()
        {
            try {
                _labware_database.RenameLabware( _old_name, LabwareName);
                Close();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteCancel()
        {
            Close();
        }
    }
}
