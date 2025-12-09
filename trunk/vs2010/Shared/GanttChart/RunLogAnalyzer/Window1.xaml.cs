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
using GalaSoft.MvvmLight.Command;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace RunLogAnalyzer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        public RelayCommand SelectCSVFileCommand { get; private set; }
        public RelayCommand AnalyzeCommand { get; private set; }
        
        private string _selectedCSVFile = String.Empty;
        public string SelectedCSVFile {
            get { return _selectedCSVFile; }
            set {
                _selectedCSVFile = value;
                OnPropertyChanged( "SelectedCSVFile");
            }
        }

        public Window1()
        {
            InitializeComponent();
            this.DataContext = this;

            SelectCSVFileCommand = new RelayCommand( () => OpenCSVFile());
            AnalyzeCommand = new RelayCommand( () => AnalyzeRun(), () => IsCSVFileSpecified());
        }

        public bool IsCSVFileSpecified()
        {
            return _selectedCSVFile != String.Empty;
        }

        public void OpenCSVFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = "csv";
            dlg.Filter = "CSV files (*.csv)|*.csv";
            if( dlg.ShowDialog() == true) {
                SelectedCSVFile = dlg.FileName;
            }
        }

        public void AnalyzeRun()
        {
            chart.Clear();
            // parse the file
            Parser parser = new Parser( SelectedCSVFile);
            // update the gantt chart
            for( int i=0; i<parser.Tasks.Count; i++) {
                Task task = parser.Tasks[i];
                if( task.Name == "Undefined")
                    continue;
                chart.TaskStart( task.StartTime, task.TipPairID, task.Name, task.Description);
                chart.TaskEnd( task.EndTime, task.TipPairID, task.Name);
            }
            chart.Snapshot();
        }

        #region INotifyPropertyChanged Members

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
