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
using BioNex.SynapsisPrototype.ViewModel;
using System.ComponentModel.Composition;
using System.Windows.Threading;

namespace BioNex.SynapsisPrototype
{
    /// <summary>
    /// Interaction logic for LogPanel.xaml
    /// </summary>
    public partial class LogPanel : UserControl
    {
        private DispatcherTimer timer_ { get; set; }
        private bool autoscroll_ { get; set; }
        private bool pipette_autoscroll_ { get; set; }

        public LogPanel()
        {
            InitializeComponent();
            //DataContext = LogPanelViewModel;
            autoscroll_ = true;
            timer_ = new DispatcherTimer();
            timer_.Interval = new TimeSpan( 0, 0, 0, 0, 500);
            timer_.Tick += new EventHandler(_timer_Tick);
            timer_.Start();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            if( autoscroll_)
                ScrollToEnd( log_datagrid);
            if( pipette_autoscroll_)
                ScrollToEnd( pipette_log_datagrid);
        }

        public void ScrollToEnd( DataGrid datagrid)
        {
            if( datagrid == log_datagrid)
                autoscroll_ = true;
            else
                pipette_autoscroll_ = true;
            if( datagrid.Items.Count > 0 && VisualChildrenCount > 0) {
                var border = VisualTreeHelper.GetChild( datagrid, 0) as Decorator;
                if( border != null) {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll != null)
                        scroll.ScrollToEnd();
                }
            }
        }

        private void datagrid_KeyUp(object sender, KeyEventArgs e)
        {
            if( Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.End)
                ScrollToEnd( sender as DataGrid);
        }

        private void log_datagrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if( sender == log_datagrid)
                autoscroll_ = false;
            else
                pipette_autoscroll_ = false;
        }

        private void log_datagrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.SetBinding(DataGridRow.BackgroundProperty, new System.Windows.Data.Binding
            {
                Source = e.Row.DataContext,
                Converter = new LogPanelViewModel.RowBackgroundConverter()
            });
        }
    }
}
