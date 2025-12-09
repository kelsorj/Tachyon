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
using System.Collections.ObjectModel;
//using Hardcodet.Wpf.Util;
using System.Diagnostics;
using System.ComponentModel;

namespace BioNex.Shared.GanttChart
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<GanttTaskRow> TaskList { get; set; }
        private ObservableCollection<GanttTaskRow> TaskListSnapshot { get; set; }
        // we want to make this library as easy to use as possible for the client, so
        // we keep track of all of the starts and ends, and do the calculations here.
        // associate a task name with a parent device, i.e. "tip" or "stage"
        // can have multiple tasks per parent at any point in time
        // calling TaskStart puts the task info into the dictionary
        // calling TaskEnd removes the task start info, calculates the time, and displays it
        Dictionary<string, Dictionary<string, Task>> _task_tracker = new Dictionary<string, Dictionary<string, Task>>();
        private object Lock = new object();
        /// <summary>
        /// the end of the visible part of the chart
        /// </summary>
        private DateTime VisibleEnd { get; set; }
        /// <summary>
        /// the beginning of all tasks
        /// </summary>
        private DateTime TimeOfFirstTask { get; set; }
        /// <summary>
        /// the end of all tasks
        /// </summary>
        private DateTime TimeOfLastTask { get; set; }
        static internal int PixelsPerSecond = 15;

        private int _slider_min;
        public int SliderMin
        {
            get { return _slider_min; }
            set {
                _slider_min = value;
                OnPropertyChanged( "SliderMin");
                Debug.WriteLine( "slider min value is now " + _slider_min.ToString());
            }
        }

        private int _slider_max;
        public int SliderMax
        {
            get { return _slider_max; }
            set {
                _slider_max = value;
                OnPropertyChanged( "SliderMax");
                Debug.WriteLine( "slider max value is now " + _slider_max.ToString());
            }
        }

        private int _slider_value;
        public int SliderValue
        {
            get { return _slider_value; }
            set {
                _slider_value = value;
                OnPropertyChanged( "SliderValue");
                Debug.WriteLine( "slider value is now " + _slider_value.ToString());
                ShiftChart();
            }
        }

        private string _start_text;
        public string StartText
        {
            get { return _start_text; }
            set {
                _start_text = value;
                OnPropertyChanged( "StartText");
            }
        }

        private string _end_text;
        public string EndText
        {
            get { return _end_text; }
            set {
                _end_text = value;
                OnPropertyChanged( "EndText");
            }
        }

        private string _debug_text;
        public string DebugText
        {
            get { return _debug_text; }
            set {
                _debug_text = value;
                OnPropertyChanged( "DebugText");
            }
        }

        public UserControl1()
        {
            InitializeComponent();
            TaskList = new ObservableCollection<GanttTaskRow>();
            this.DataContext = this;
            StartText = "Start";
            EndText = "End";
        }

        public void Clear()
        {
            TaskList.Clear();
            _task_tracker.Clear();
        }

        /// <summary>
        /// This copies all of the tasks added thus far into a safe area.  Now the GUI is
        /// free to have its ObservableCollection changed on the fly based on slider position
        /// </summary>
        public void Snapshot()
        {
            TaskListSnapshot = new ObservableCollection<GanttTaskRow>( TaskList);
            this.SliderMin = 0;
            this.SliderValue = 0;
            this.SliderMax = (int)(TimeOfLastTask - TimeOfFirstTask).TotalSeconds;
        }

        private void ShiftChart()
        {
            // figure out the start and end time based on the slider position and size of
            // the window.  Slider is in seconds
            DateTime new_start = TimeOfFirstTask + new TimeSpan( 0, 0, SliderValue);
            double time_shown_s = TaskColumn.ActualWidth / PixelsPerSecond;
            DateTime new_end = new_start + new TimeSpan( 0, 0, (int)time_shown_s);
            //! \todo this is not absolutely precise!  should try to handle milliseconds
            StartText = new_start.ToString();
            EndText = new_end.ToString();

            if( TaskListSnapshot == null)
                return;

            // now that we have the correct time range that's visible on-screen, we
            // need to filter the GanttRow's task list to only have the tasks
            // within this time range
            TaskList.Clear();
            foreach( GanttTaskRow gtr in TaskListSnapshot) {
                GanttTaskRow new_gtr = new GanttTaskRow();
                new_gtr.Name = gtr.Name;
                new_gtr.Start = new_start;
                new_gtr.End = new_end;
                foreach( Task task in gtr.Tasks) {
                    if( task.Start >= new_start && task.End <= new_end)
                        new_gtr.Tasks.Add( new Task( task.Name, task.Start, task.End, task.Description));
                    // this case handles tasks that are part of the visible area, but go out of bounds
                    if( task.Start <= new_start && task.End >= new_start) {
                        TimeSpan offset = new_start - task.Start;
                        new_gtr.Tasks.Add( new Task( task.Name, task.Start + offset, task.End, task.Description));
                    }
                    // this case handles tasks that go off to the right out of bounds
                    if( task.Start <= new_end && task.End >= new_end) {
                        TimeSpan offset = task.End - new_end;
                        new_gtr.Tasks.Add( new Task( task.Name, task.Start, task.End - offset, task.Description));
                    }
                }
                TaskList.Add( new_gtr);
            }
            
            UpdateLayout();
        }

        public void TaskStart( string parent_name, string task_name, string task_tooltip)
        {
            TaskStart( DateTime.Now, parent_name, task_name, task_tooltip);
        }

        public void TaskStart( DateTime timestamp, string parent_name, string task_name, string task_tooltip)
        {
            lock( Lock) {
                // if this is the first task added, set it as the initial start time.  We'll
                // still re-evaluate the start time as other tasks get added
                if( _task_tracker.Count == 0) {
                    TimeOfFirstTask = timestamp;
                    TimeOfLastTask = timestamp;
                    StartText = timestamp.ToString();
                }
 
                // make sure that this task name isn't already present for the same parent
                if( _task_tracker.ContainsKey( parent_name)) {
                    Dictionary<string, Task> parent_tasks = _task_tracker[parent_name];
                    Debug.Assert( !parent_tasks.ContainsKey(task_name));
                    if( parent_tasks.ContainsKey(task_name))
                        return;
                } else {
                    _task_tracker[parent_name] = new Dictionary<string,Task>();
                }
                _task_tracker[parent_name].Add( task_name, new Task());
                _task_tracker[parent_name][task_name].Start = timestamp;
                _task_tracker[parent_name][task_name].Description = task_tooltip;
            }
        }

        public void TaskEnd( string parent_name, string task_name)
        {
            TaskEnd( DateTime.Now, parent_name, task_name);
        }

        public void TaskEnd( DateTime timestamp, string parent_name, string task_name)
        {
            lock( Lock) {
                // ensure that the task we are trying to end was actually started to begin with
                if( _task_tracker.ContainsKey( parent_name)) {
                    Dictionary<string, Task> parent_tasks = _task_tracker[parent_name];
                    Debug.Assert( parent_tasks.ContainsKey(task_name));
                    if( !parent_tasks.ContainsKey(task_name))
                        return;
                } else {
                    _task_tracker[parent_name] = new Dictionary<string,Task>();
                }

                Task tp = _task_tracker[parent_name][task_name];
                //! \todo need to evaluate all other tasks in tracker to get real end time

                tp.End = timestamp;
                if( timestamp > TimeOfLastTask)
                    TimeOfLastTask = timestamp;

                // now we have to modify the task item in the gantt chart
                // pull the row of tasks from the TaskList (if it's not there, create a new one)
                GanttTaskRow gti;
                try {
                    gti = TaskList.First<GanttTaskRow>( x => x.Name == parent_name);
                } catch( InvalidOperationException) {
                    // couldn't find this parent (i.e. "tip" or "stage"), so add it
                    gti = new GanttTaskRow();
                    gti.Name = parent_name;
                    //! \todo this is lame -- I couldn't figure out how to get at Start and End
                    //!       in the UC, so I renamed them to _start and _end and assign
                    //!       DPs in GanttTaskInfo to the same value since I can databind OK to
                    //!       those properties.
                    gti.Start = TimeOfFirstTask;
                    gti.End = TimeOfLastTask;
                    gti.Tasks.Add( new Task( task_name, tp.Start, tp.End, tp.Description));
                    TaskList.Add( new GanttTaskRow { Name = parent_name, Tasks = gti.Tasks, Start = TimeOfFirstTask, End = TimeOfLastTask });
                    _task_tracker[parent_name].Remove( task_name);
                    return;
                }
                gti.Start = TimeOfFirstTask;
                gti.End = TimeOfLastTask;
                gti.Tasks.Add( new Task( task_name, tp.Start, tp.End, tp.Description));
                _task_tracker[parent_name].Remove( task_name);
                UpdateLayout();
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return base.MeasureOverride(constraint);
        }

        #region INotifyPropertyChanged event
        ///<summary>
        ///Occurs when a property value changes.
        ///</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for
        /// a given property.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DebugText = TaskColumn.ActualWidth.ToString();
            double time_shown_s = TaskColumn.ActualWidth / PixelsPerSecond;
            VisibleEnd = TimeOfFirstTask + new TimeSpan( 0, 0, (int)time_shown_s);
            //! \todo this is not absolutely precise!  should try to handle milliseconds
            EndText = VisibleEnd.ToString();
            ShiftChart();
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if( e.Key == Key.Up)
                PixelsPerSecond++;
            if( e.Key == Key.Down)
                PixelsPerSecond--;
            e.Handled = true;
        }
    }

    /// <summary>
    /// Since the DataGrid column's width is of a different type than that of a standard Grid's column width,
    /// I had to write an IValueConverter to do the dirty work, as suggested by the output window when I
    /// first tried to bind the two values.
    /// </summary>
    public class WidthFormatter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            System.Windows.Controls.DataGridLength temp = (System.Windows.Controls.DataGridLength)value;
            return new System.Windows.GridLength( temp.DisplayValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

    public class GanttGUIUpdate
    {
        public bool TaskStart { get; set; }
        public string ParentName { get; set; }
        public string TaskName { get; set; }
        public string TaskDescription { get; set; }

        public GanttGUIUpdate() {}
        public GanttGUIUpdate( string parent_name, string task_name, string task_description) 
        {
            TaskStart = true;
            ParentName = parent_name;
            TaskName = task_name;
            TaskDescription = task_description;
        }
        public GanttGUIUpdate( string parent_name, string task_name)
        {
            TaskStart = false;
            ParentName = parent_name;
            TaskName = task_name;
            TaskDescription = "";
        }
    }
}
