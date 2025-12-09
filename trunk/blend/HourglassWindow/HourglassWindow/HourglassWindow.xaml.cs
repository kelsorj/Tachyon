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
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.ComponentModel;

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
                SetValue(AlertVisibilityProperty, value == Visibility.Hidden || value == Visibility.Collapsed ? Visibility.Visible : Visibility.Hidden);
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
			this.InitializeComponent();
            this.DataContext = this;
		}
    }
}