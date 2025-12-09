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
using SimpleInventory;

namespace InventoryTestApplication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        InventoryBackend _inventory;

        public Window1()
        {
            InitializeComponent();
            _inventory = new InventoryXML( "app_test_inventory.xml");
            List<string> teachpoints = new List<string> { "teachpoint1", "teachpoint2" };
            _inventory.TeachpointNames = teachpoints;
            UserControl inventory_view = _inventory.GetInventoryView();
            Content = inventory_view;
        }
    }
}
