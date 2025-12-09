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
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;

namespace BioNex.SynapsisPrototype
{
    /// <summary>
    /// Interaction logic for ErrorLog.xaml
    /// </summary>
    public partial class ErrorLog : UserControl
    {
        public ErrorLog()
        {
            InitializeComponent();
        }

        #region IError Members

        public void AddError(ErrorData error)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
