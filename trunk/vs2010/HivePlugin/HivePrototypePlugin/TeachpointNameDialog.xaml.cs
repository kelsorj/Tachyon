using System;
using System.Collections.Generic;
using System.Windows;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for TeachpointNameDialog.xaml
    /// </summary>
    public partial class TeachpointNameDialog : Window
    {
        public string TeachpointName { get; set; }
        public bool TeachpointNameValid { get; private set; }
        private readonly List<string> ReservedNames = new List<string>();

        public TeachpointNameDialog()
        {
            TeachpointName = "My Teachpoint";
            InitializeComponent();
            DataContext = this;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            TeachpointNameValid = true;
            if( ReservedNames.Contains( TeachpointName)) {
                MessageBox.Show( String.Format( "The teachpoint name '{0}' is reserved.  Please try another name.", TeachpointName));
                TeachpointNameValid = false;
                return;
            }
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            text_teachpoint.Focus();
            text_teachpoint.SelectAll();
        }
    }
}
