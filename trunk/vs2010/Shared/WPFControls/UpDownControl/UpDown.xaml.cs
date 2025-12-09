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
using System.ComponentModel;

namespace BioNex.Shared.WPFControls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UpDown : UserControl
    {
        public ICommand IncrementCommand { get; set; }
        public ICommand DecrementCommand { get; set; }

        public static readonly DependencyProperty NumberProperty = DependencyProperty.Register( "Number", typeof( int), typeof( UpDown));
        public int Number
        {
            get { return (int)GetValue(NumberProperty); }
            set { SetValue( NumberProperty, value); }
        }

        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register( "Index", typeof( int), typeof( UpDown));
        public int Index
        {
            get { return (int)GetValue(IndexProperty); }
            set { SetValue( IndexProperty, value); }
        }

        public UpDown()
        {
            InitializeComponent();
            InitializeCommands();
            LayoutRoot.DataContext = this;
        }

        public void Increment()
        {
            Number++;
        }

        public void Decrement()
        {
            Number--;
        }
    }
}
