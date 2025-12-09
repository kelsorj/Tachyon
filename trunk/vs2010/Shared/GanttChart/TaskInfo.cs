using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;

namespace BioNex.Shared.GanttChart
{
    /// <summary>
    /// When the row of the Gantt chart is displayed, it gets the task information
    /// from each instance of this class, found in the GanttTaskInfo class
    /// </summary>
    public class Task : INotifyPropertyChanged
    {
        public string Name { get; set; }
        /*
        private DateTime _start;
        public DateTime Start
        { 
            get { return _start; }
            set {
                _start = value;
                OnPropertyChanged( "Start");
            }
        }
        private DateTime _end;
        public DateTime End
        { 
            get { return _start; }
            set {
                _end = value;
                OnPropertyChanged( "End");
            }
        }
         */
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Description { get; set; }

        public Task() {}

        public Task( string name, DateTime start, DateTime end, string description)
        {
            Name = name;
            Start = start;
            End = end;
            Description = description;
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
    }

    /// <summary>
    /// GanttTaskInfo contains all of the Tasks displayed in a row of the Gantt chart,
    /// and the contents of the ItemsControl are databound to this class.
    /// </summary>
    public class GanttTaskRow : INotifyPropertyChanged
    {
        private string name = String.Empty;
        public string Name
        {
            get { return name; }
            set {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        private ObservableCollection<Task> tasks = new ObservableCollection<Task>();
        public ObservableCollection<Task> Tasks
        {
            get { return tasks; }
            set {
                tasks = value;
                OnPropertyChanged("Tasks");
            }
        }

        private DateTime _start;
        public DateTime Start
        {
            get { return _start; }
            set {
                _start = value;
                OnPropertyChanged( "Start");
            }
        }

        private DateTime _end;
        public DateTime End
        {
            get { return _end; }
            set {
                _end = value;
                OnPropertyChanged( "End");
            }
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
    }
}
