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
using System.Windows.Shapes;

namespace MicroscanLibraryTestApplication
{
    /// <summary>
    /// Interaction logic for UserControlDisplay.xaml
    /// </summary>
    public partial class UserControlDisplay : Window
    {
        public UserControlDisplay( UserControl control)
        {
            InitializeComponent();
            Content = control;
        }
    }
}
