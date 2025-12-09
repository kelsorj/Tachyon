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

namespace BioNex.Shared.SimpleInventory
{
    /// <summary>
    /// Interaction logic for InventoryView.xaml
    /// </summary>
    public partial class InventoryView : UserControl
    {
        private InventoryBackend _model;
        public InventoryBackend Inventory
        {
            get { return _model; }
            set {
                _model = value;
                this.DataContext = _model;
            }
        }

        public InventoryView()
        {
            try {
                InitializeComponent();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }
    }
}
