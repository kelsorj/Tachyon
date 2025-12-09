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

namespace BioNex.GreenMachine
{
    /// <summary>
    /// Interaction logic for TecanPumpDiagnostics.xaml
    /// </summary>
    public partial class TecanPumpDiagnostics : UserControl, INotifyPropertyChanged
    {
        private int _pump_index;
        private IGreenMachineController _model;
        public RelayCommand InitializePumpCommand { get; set; }
        public RelayCommand StopPumpCommand { get; set; }
        public RelayCommand StartCommand { get; set; }
        public RelayCommand ClearCommand { get; set; }
        
        private double _selected_volume;
        public double SelectedVolume
        {
            get { return _selected_volume; }
            set {
                _selected_volume = value;
                OnPropertyChanged( "SelectedVolume");
            }
        }

        private int _selected_speed;
        public int SelectedSpeed
        {
            get { return _selected_speed; }
            set {
                _selected_speed = value;
                OnPropertyChanged( "SelectedSpeed");
            }
        }

        private string _selected_direct_command;
        public string SelectedDirectCommand
        {
            get { return _selected_direct_command; }
            set {
                _selected_direct_command = value;
                OnPropertyChanged( "SelectedDirectCommand");
            }
        }

        public TecanPumpDiagnostics( int pump_index, IGreenMachineController model)
        {
            InitializeComponent();
            this.DataContext = this;
            _pump_index = pump_index;
            _model = model;

            InitializePumpCommand = new RelayCommand( ExecuteInitializePump);
            StopPumpCommand = new RelayCommand( ExecuteStopPump);
            StartCommand = new RelayCommand( ExecuteStart);
            ClearCommand = new RelayCommand( ExecuteClear);
        }

        private void ExecuteInitializePump()
        {
        }

        private void ExecuteStopPump()
        {
        }

        private void ExecuteStart()
        {
        }

        private void ExecuteClear()
        {
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
