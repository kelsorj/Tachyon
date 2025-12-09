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
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;

namespace BioNex.IgenicaGUI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(ICustomerGUI))]
    public partial class CustomerGUI : UserControl, ICustomerGUI, INotifyPropertyChanged
    {
        public CustomerGUI()
        {
            InitializeComponent();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion

        #region ICustomerGUI Members

        public event EventHandler ProtocolComplete;

        public string GUIName
        {
            get { return "Igenica GUI"; }
        }

        public bool Busy
        {
            get { return false; }
        }

        public string BusyReason
        {
            get { return "Not busy"; }
        }

        public bool CanExecuteStart(out IEnumerable<string> failure_reasons)
        {
            failure_reasons = new List<string>();
            return true;
        }

        public bool ExecuteStart()
        {
            return true;
        }

        public void Close()
        {
            
        }

        #endregion
    }
}
