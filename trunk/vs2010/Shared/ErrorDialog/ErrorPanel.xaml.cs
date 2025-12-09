using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using GalaSoft.MvvmLight.Messaging;
using log4net;
using System;

namespace BioNex.Shared.ErrorHandling
{
    /// <summary>
    /// Interaction logic for ErrorPanel.xaml
    /// </summary>
    public partial class ErrorPanel : UserControl
    {
        private ManualResetEvent _abort_event { get; set; }
        public Rectangle Overlay { get; private set; }
        public bool Handled { get; private set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( ErrorPanel));
        private readonly string _last_error;

        public delegate void ErrorPanelActionTakenHandler(object sender);
        public event ErrorPanelActionTakenHandler ErrorActionTaken; // To notify listeners that the user has clicked a button on the error panel (so it can be removed from UI)

        public ErrorPanel( ErrorData error_data)
        {
            InitializeComponent();

            Grid grid = new Grid();
            grid.RowDefinitions.Add( new RowDefinition()); // for the error description
            grid.ColumnDefinitions.Add(new ColumnDefinition()); // for the timestamp
            grid.ColumnDefinitions.Add(new ColumnDefinition()); // for the error panel itself

            var timestamp_text = new TextBlock() { Text = error_data.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3) };
            Grid.SetRow(timestamp_text, 0);
            Grid.SetColumn(timestamp_text, 0);
            Grid.SetRowSpan(timestamp_text, 2);
            grid.Children.Add(timestamp_text);

            // new behavior -- we can have an error description and error details.  If we have details, then
            // we use an expander, and the description is the expander's Header.  If we don't have details, then
            // we just use a TextBlock.

            if( error_data.Details == "") {
                TextBlock text = new TextBlock { Text = _last_error = error_data.ErrorMessage, Margin = new Thickness(3), TextWrapping = TextWrapping.Wrap };
                Grid.SetRow( text, 0);
                Grid.SetColumn(text, 1);
                grid.Children.Add( text);
            } else {
                // set up expander content
                TextBlock text = new TextBlock { Text = error_data.Details, Margin = new Thickness(3), TextWrapping = TextWrapping.Wrap };
                Expander expander = new Expander { Header = _last_error = error_data.ErrorMessage, Margin = new Thickness(3), Content = text };
                _last_error = _last_error + ": " + error_data.Details;
                Grid.SetRow( expander, 0);
                Grid.SetColumn( expander, 1);
                grid.Children.Add( expander);
            }
            _log.DebugFormat( "Added error '{0}' to the error display window", _last_error);
            
            UniformGrid button_grid = new UniformGrid();
            button_grid.Rows = 1;
            foreach( KeyValuePair< string, ManualResetEvent> kvp in error_data.Events) {
                Button handler = new Button { Content = kvp.Key, Tag = kvp.Value };
                if( kvp.Key == "Abort"){
                    _abort_event = kvp.Value;
                }
                handler.Margin = new Thickness( 3);
                handler.Click += new RoutedEventHandler(handler_Click);
                button_grid.Children.Add( handler);
            }

            grid.RowDefinitions.Add( new RowDefinition());
            Grid.SetRow( button_grid, 1);
            Grid.SetColumn(button_grid, 1);
            grid.Children.Add( button_grid);

            // add a translucent red overlay to make error more obvious
            Overlay = new Rectangle();
            Overlay.Fill = Brushes.Red;
            Overlay.Opacity = 0.2;
            Overlay.IsHitTestVisible = false;
            Grid.SetRow( Overlay, 0);
            Grid.SetRowSpan( Overlay, 2);
            Grid.SetColumn(Overlay, 1);
            grid.Children.Add( Overlay);            
            
            stackpanel.Children.Add( grid);
        }

        /// <summary>
        /// Aborts the state machine that owns this error.  Handles Abort button click in Diagnostics, as well
        /// as dismissal of all state machine errors when a user starts a new protocol, if errors are still
        /// in the window.
        /// </summary>
        public void Abort()
        {
            if( _abort_event != null){
                _abort_event.Set();
            }
            Messenger.Default.Send< SMAbortCommand>( new SMAbortCommand());
        }

        /// <summary>
        /// Only used when shutting down the application and we need to close out all pending errors
        /// </summary>
        /// <remarks>
        /// This feature doesn't work yet.  There still needs to be a way to signal the _main_gui_abort_event.
        /// </remarks>
        public void Cancel()
        {
            if( _abort_event != null)
                _abort_event.Set();
        }

        void handler_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if( button == null)
                return;
            ManualResetEvent reset_event = button.Tag as ManualResetEvent;
            // foreach( UIElement child in stackpanel.Children)
            // child.IsEnabled = false;
            ErrorPanel ep = stackpanel.Parent as ErrorPanel;
            ep.IsEnabled = false;
            ep.Overlay.Fill = Brushes.Transparent;
            Handled = true;

            if( reset_event == _abort_event){
                Abort();
            } else{
                reset_event.Set();
            }

            // #323 log user's selection
            _log.InfoFormat( "User clicked button '{0}' for error '{1}'", button.Content, _last_error);

            // signal the main UI that this error has been handled so that it can be removed from the list
            if (ErrorActionTaken != null)
                ErrorActionTaken(this);
        }
    }
}
