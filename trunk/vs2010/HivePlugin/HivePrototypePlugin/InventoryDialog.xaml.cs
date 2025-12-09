using System.Windows;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for InventoryDialog.xaml
    /// </summary>
    public partial class InventoryDialog : Window
    {
        public InventoryDialog( HivePlugin controller)
        {
            InitializeComponent();

            Title = controller.Name + " Inventory View";
            InventoryView inventory_view = new InventoryView( controller);
            DataContext = controller;
            Content = inventory_view;
        }
    }
}
