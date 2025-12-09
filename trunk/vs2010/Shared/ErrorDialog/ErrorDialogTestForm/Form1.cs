using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ErrorDialog;
using System.Windows;

namespace ErrorDialogTestForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ErrorDialog.ErrorDialog dlg = new ErrorDialog.ErrorDialog();
            dlg.SetError( "The basic error text goes here");
            dlg.SetDetailedError( "The detailed error text goes here");
            dlg.AddErrorHandler<RetryHandlerInfo>( "Retry", "Click to retry", RetryHandler, new RetryHandlerInfo( "context specific message here!"));
            dlg.Show();
        }

        private class RetryHandlerInfo
        {
            string _context_specific_info;

            public RetryHandlerInfo( string info)
            {
                _context_specific_info = info;
            }

            public string Info
            {
                get { return _context_specific_info; }
            }
        }

        private void RetryHandler( object sender, RoutedEventArgs e)
        {
            ErrorDialog.ErrorDialog.ErrorButton<RetryHandlerInfo> button = sender as ErrorDialog.ErrorDialog.ErrorButton<RetryHandlerInfo>;
            System.Windows.Forms.MessageBox.Show( button.Object.Info);
        }
    }
}
