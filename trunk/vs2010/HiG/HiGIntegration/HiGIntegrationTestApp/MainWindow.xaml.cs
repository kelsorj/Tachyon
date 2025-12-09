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
using System.Windows.Threading;
using BioNex.Shared.Utils;

namespace HiGIntegrationTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ICommand AddHigCommand { get; set; }
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            
            AddHigCommand = new SimpleRelayCommand( AddHiG);
            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
            _timer.Start();

            AddHiG();
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
            // Close all HiGs here
            foreach( var tab in Tabs.Items) {
                HiGPanel panel = ((tab as TabItem).Content) as HiGPanel;
                if( panel != null) // could be the Add HiG button
                    panel.Close();
            }

            base.OnClosing(e);
        }

        public void AddHiG()
        {
            TabItem tabitem = new TabItem();
            HiGPanel panel = new HiGPanel( this);
            panel.SetHiGIndex( Tabs.Items.Count - 1);
            tabitem.Content = panel;
            tabitem.Header = String.Format( "HiG #{0}", Tabs.Items.Count);
            Tabs.Items.Insert( Tabs.Items.Count - 1, tabitem);
        }
    }
}
