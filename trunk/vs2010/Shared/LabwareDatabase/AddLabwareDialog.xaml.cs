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
    public partial class AddLabwareDialog : Window
    {
        public string LabwareName { get; set; }
        public string Notes { get; set; }
        public string Tags { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
        private ILabwareDatabase _labware_database { get; set; }

        public AddLabwareDialog( ILabwareDatabase labware_database)
        {
            InitializeComponent();
            Notes = "";
            Tags = "";
            OkCommand = new RelayCommand( ExecuteOk, () => { return LabwareName != null && LabwareName != ""; });
            CancelCommand = new RelayCommand( ExecuteCancel);
            _labware_database = labware_database;

            DataContext = this;
        }

        private void ExecuteOk()
        {
            try {
                Labware labware = new Labware( LabwareName, Notes, Tags);
                _labware_database.AddLabware( labware);
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
