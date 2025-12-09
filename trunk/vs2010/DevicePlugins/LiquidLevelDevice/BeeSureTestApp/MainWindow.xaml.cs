using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BioNex.Shared.Utils;
using System.Windows.Input;

namespace BeeSureTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SimpleRelayCommand AddBeeSureCommand { get; set; }
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            AddBeeSureCommand = new SimpleRelayCommand( AddBeeSure);
            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
            _timer.Start();

            AddBeeSure();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }        

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Close all BeeSures here
            foreach( var tab in Tabs.Items) {
                BeeSurePanel panel = ((tab as TabItem).Content) as BeeSurePanel;
                if( panel != null) // could be the Add BeeSure button
                    panel.Close();
            }

            base.OnClosing(e);
        }

        public void AddBeeSure()
        {
            TabItem tabitem = new TabItem();
            int index = Tabs.Items.Count - 1;
            BeeSurePanel panel = new BeeSurePanel( this, index);
            tabitem.Content = panel;
            tabitem.Header = String.Format( "BeeSure #{0}", index + 1);
            Tabs.Items.Insert( index, tabitem);
        }
    }
}
