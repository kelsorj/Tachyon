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

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for InventoryView.xaml
    /// </summary>
    public partial class InventoryView : UserControl
    {
        private HivePlugin Controller { get; set; }

        public InventoryView( HivePlugin controller)
        {
            InitializeComponent();
            Controller = controller;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Controller.UpdateStaticInventoryView();
        }
    }
}
