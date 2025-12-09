using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace BioNexSplash
{
    /// <summary>
    /// Interaction logic for BioNexSplashControl.xaml
    /// </summary>
    public partial class BioNexSplashControl : Window
    {
        public BioNexSplashControl()
        {
            InitializeComponent();
        }


        public void CloseAfter(Window owner, int timeout_ms)
        {
            Owner = owner;
            Topmost = false;
            var thread = new System.Threading.Thread(() =>
                {
                    System.Threading.Thread.Sleep(timeout_ms);
                    Dispatcher.Invoke( new Action( Close));
                });
            thread.IsBackground = true;
            thread.Start();
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        bool really_close = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (really_close)
                return;
            e.Cancel = true;
            var fade = (Storyboard)FindResource("close_animation"); 
            fade.Begin(this);
        }
        private void closeStoryBoard_Completed(object sender, EventArgs e)
        {
            really_close = true;
            this.Close();
        }
    }
}
