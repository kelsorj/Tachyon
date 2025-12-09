using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BioNex.HiGIntegration
{
    /// <summary>
    /// Interaction logic for Diagnostics.xaml
    /// </summary>
    public partial class Diagnostics : Window
    {
        private readonly DispatcherTimer _timer;
        private HiG _viewmodel;

        internal Diagnostics( HiG viewmodel)
        {
            InitializeComponent();
            DataContext = viewmodel;
            _viewmodel = viewmodel;

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
            _timer.Start();
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
    }
}
