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

namespace BioNex.IOPlugin
{
    /// <summary>
    /// Interaction logic for IOView.xaml
    /// </summary>
    public partial class InputLEDView : UserControl, INotifyPropertyChanged
    {
        /*
        private Int32 index_;
        public Int32 Index
        {
            get { return index_; }
            set {
                index_ = value;
                OnPropertyChanged( "Index");
            }
        }
         */

        private string _input_label;
        public string InputLabel
        {
            get { return _input_label; }
            set {
                _input_label = value;
                OnPropertyChanged( "InputLabel");
            }
        }

        private Brush color_;
        public Brush InputColor
        {
            get { return color_; }
            set {
                color_ = value;
                OnPropertyChanged( "InputColor");
            }
        }

        private Boolean state_;
        public Boolean State
        {
            get { return state_; }
            set {
                state_ = value;
                if( value) {
                    InputColor = Brushes.Green;
                } else {
                    InputColor = Brushes.Transparent;
                }
            }
        }

        public InputLEDView()
        {
            InitializeComponent();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
