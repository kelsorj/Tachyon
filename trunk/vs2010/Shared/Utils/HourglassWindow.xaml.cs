using System;
using System.Windows;
using System.Windows.Threading;
using System.Threading;

namespace BioNex.Shared.Utils
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class HourglassWindow : Window
	{
		public int TitleFontSize
		{
			get { return (int)GetValue(TitleFontSizeProperty); }
			set { SetValue(TitleFontSizeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TitleFontSize.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TitleFontSizeProperty =
			DependencyProperty.Register("TitleFontSize", typeof(int), typeof(HourglassWindow), new UIPropertyMetadata(12));

		public Visibility PleaseWaitVisibility
		{
			get { return (Visibility)GetValue(PleaseWaitVisibilityProperty); }
			set { 
				SetValue(PleaseWaitVisibilityProperty, value);
				AlertVisibility = value == Visibility.Hidden || value == Visibility.Collapsed ? Visibility.Visible : Visibility.Hidden;
			}
		}

		// Using a DependencyProperty as the backing store for PleaseWaitVisibility.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PleaseWaitVisibilityProperty =
			DependencyProperty.Register("PleaseWaitVisibility", typeof(Visibility), typeof(HourglassWindow), new UIPropertyMetadata(Visibility.Visible));
		
		public Visibility AlertVisibility
		{
			get { return (Visibility)GetValue(AlertVisibilityProperty); }
			set { SetValue(AlertVisibilityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AlertVisibility.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AlertVisibilityProperty =
			DependencyProperty.Register("AlertVisibility", typeof(Visibility), typeof(HourglassWindow), new UIPropertyMetadata(Visibility.Hidden));

		

		public HourglassWindow()
		{
			InitializeComponent();
			DataContext = this;
		}

        public static HourglassWindow ShowHourglassWindow( Dispatcher d, Window owner, string caption, bool show_please_wait, int fontsize=20)
        {       
            HourglassWindow hg = null;

            d.Invoke( new Action( () => {
                hg = new HourglassWindow();
                hg.PleaseWaitVisibility = show_please_wait ? Visibility.Visible : Visibility.Collapsed;
                hg.ShowInTaskbar = false;
                hg.Title = caption;
                hg.TitleFontSize = fontsize;
                hg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                hg.Owner = owner;
                hg.Show();
            }));

            return hg;
        }

        public static HourglassWindow ShowHourglassWindow( Dispatcher d, System.Windows.Controls.UserControl owner, string caption, bool show_please_wait, int fontsize=20)
        {
            return ShowHourglassWindow( d, System.Windows.Window.GetWindow(owner), caption, show_please_wait, fontsize);
        }
            
        public static void ChangeHourglassWindowCaption( Dispatcher d, HourglassWindow hg, string caption)
        {
            d.Invoke(new Action(() =>
            {
                if (hg == null)
                    return;
                hg.Title = caption;
            }));
        }

        public static void CloseHourglassWindow( Dispatcher d, HourglassWindow hg)
        {
            d.Invoke( new Action( () => {
                if( hg == null)
                    return;
                hg.Close();
                hg = null;
            }));
        }
	}
}