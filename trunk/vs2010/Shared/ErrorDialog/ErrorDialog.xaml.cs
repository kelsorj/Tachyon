using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using BioNex.Shared.IError;

namespace BioNex.Shared.ErrorHandling
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ErrorDialog : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [ DllImport( "user32.dll", SetLastError = true)]
        private static extern int GetWindowLong( IntPtr hWnd, int nIndex);
        [ DllImport( "user32.dll")]
        private static extern int SetWindowLong( IntPtr hWnd, int nIndex, int dwNewLong);
        
        public ErrorDialog( ErrorData error_data)
        {
            InitializeComponent();
            error_text.Text = error_data.ErrorMessage;
            SetDetailedError(error_data.Details);
            expander.Visibility = Visibility.Visible;
            foreach( KeyValuePair<string,ManualResetEvent> kvp in error_data.Events) {
                Button handler = new Button { Content = kvp.Key, Tag = kvp.Value };
                handler.Margin = new Thickness( 3);
                handler.Click += new RoutedEventHandler(handler_Click);
                button_panel.Children.Add( handler);
            }
            Loaded += new RoutedEventHandler( handler_Loaded);
        }

        void handler_Loaded( object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper( this).Handle;
            SetWindowLong( hwnd, GWL_STYLE, GetWindowLong( hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        void handler_Click( object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if( button == null)
                return;
            ManualResetEvent reset_event = button.Tag as ManualResetEvent;
            foreach( UIElement child in button_panel.Children)
                child.IsEnabled = false;
            reset_event.Set();
            Close();
        }

        public void SetDetailedError( string message)
        {
            detailed_error_text.Text = message;
            expander.Visibility = Visibility.Visible;
        }
    }
}
