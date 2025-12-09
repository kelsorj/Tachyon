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
using BioNex.Shared.GanttChart;

namespace GanttChartTestApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            try {
                InitializeComponent();
            } catch( Exception ex) {
                Console.WriteLine( ex.InnerException);
                // hopefully we can catch the error here!
            }
        }

        private void button_mark_start_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            text_start.Text = now.ToString();
            chart.TaskStart( combo_parent.Text, text_task_name.Text, text_task_description.Text);
        }

        private void button_mark_end_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            text_end.Text = now.ToString();
            chart.TaskEnd( combo_parent.Text, text_task_name.Text);
        }
    }
}
