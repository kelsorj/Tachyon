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

namespace LogGridReportingPlugin
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class LogControl : UserControl, BioNex.ReportingInterface.ReportingInterface
    {
        public LogControl()
        {
            InitializeComponent();
        }

        public string Name
        {
            get
            {
                return "Grid Log Reporting Plugin";
            }
        }

        public void LogMessage( object properties)
        {

        }

        public void LogError( object properties)
        {

        }

        public void EnableMessages( bool enable)
        {

        }

        public void EnableErrors( bool enable)
        {

        }

        public void Open( System.Windows.Controls.Panel parent_element)
        {
            parent_element.Children.Add( log_listview);
        }

        public void Close()
        {

        }

        public void ShowSetup()
        {
    
        }
    }
}
