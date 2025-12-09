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
using BioNex.Shared.Utils;
using BioNex.GreenMachine.HardwareInterfaces;

namespace BioNex.GreenMachine
{
    /// <summary>
    /// Interaction logic for TTDiagnostics.xaml
    /// </summary>
    public partial class TTDiagnostics : UserControl, INotifyPropertyChanged
    {
        private GreenMachine _device;
        private IGreenMachineController _controller;

        public RelayCommand StopCommand { get; set; }
        public RelayCommand MoveToTeachpointCommand { get; set; }
        public RelayCommand UpdateTeachpointCommand { get; set; }
        public RelayCommand HomeAllAxesCommand { get; set; }
        public RelayCommand<string> JogPositiveCommand { get; set; }
        public RelayCommand<string> JogNegativeCommand { get; set; }
        public RelayCommand<string> HomeAxisCommand { get; set; }
        public RelayCommand EnableXCommand { get; set; }
        public RelayCommand EnableYCommand { get; set; }
        public RelayCommand EnableZCommand { get; set; }

        private List<double> _increments;
        public ICollectionView XAxisIncrementItems { get; set; }
        public ICollectionView YAxisIncrementItems { get; set; }
        public ICollectionView ZAxisIncrementItems { get; set; }

        private ThreadedUpdates _updater;
        
        private double _current_x; 
        public double CurrentX
        {
            get { return _current_x; }
            set {
                _current_x = value;
                OnPropertyChanged( "CurrentX");
            }
        }

        private double _current_y;
        public double CurrentY
        {
            get { return _current_y; }
            set {
                _current_y = value;
                OnPropertyChanged( "CurrentY");
            }
        }

        private double _current_z;
        public double CurrentZ
        {
            get { return _current_z; }
            set {
                _current_z = value;
                OnPropertyChanged( "CurrentZ");
            }
        }

        private bool _x_enabled;
        public bool XEnabled
        {
            get { return _x_enabled; }
            set {
                _x_enabled = value;
                OnPropertyChanged( "XEnabled");
            }
        }

        private bool _y_enabled;
        public bool YEnabled
        {
            get { return _y_enabled; }
            set {
                _y_enabled = value;
                OnPropertyChanged( "YEnabled");
            }
        }

        private bool _z_enabled;
        public bool ZEnabled
        {
            get { return _z_enabled; }
            set {
                _z_enabled = value;
                OnPropertyChanged( "ZEnabled");
            }
        }

        public TTDiagnostics( GreenMachine device)
        {
            InitializeComponent();
            this.DataContext = this;

            _device = device;
            _controller = device._controller;
            _updater = new ThreadedUpdates( "Stage diagnostics update thread", UpdateThread, 100);
            _increments = new List<double> { 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100 };
            // use ToList on each so that the three droplists don't synchronize their item selections
            XAxisIncrementItems = CollectionViewSource.GetDefaultView( _increments.ToList());
            YAxisIncrementItems = CollectionViewSource.GetDefaultView( _increments.ToList());
            ZAxisIncrementItems = CollectionViewSource.GetDefaultView( _increments.ToList());

            InitializeCommands();
        }

        private void UpdateThread()
        {
            CurrentX = _controller.Stage.GetPositionMM( IXyz.Axes.X);
            CurrentY = _controller.Stage.GetPositionMM( IXyz.Axes.Y);
            CurrentZ = _controller.Stage.GetPositionMM( IXyz.Axes.Z);

            XEnabled = _controller.Stage.IsAxisEnabled( IXyz.Axes.X);
            YEnabled = _controller.Stage.IsAxisEnabled( IXyz.Axes.Y);
            ZEnabled = _controller.Stage.IsAxisEnabled( IXyz.Axes.Z);
        }

        private void InitializeCommands()
        {
            StopCommand = new RelayCommand( () => _controller.Stage.Stop() );
            MoveToTeachpointCommand = new RelayCommand( ExecuteMoveToTeachpoint);
            UpdateTeachpointCommand = new RelayCommand( ExecuteUpdateTeachpoint);
            HomeAllAxesCommand = new RelayCommand( ExecuteHomeAllAxes);
            HomeAxisCommand = new RelayCommand<string>( (axis_name) => ExecuteHomeAxis( axis_name));
            JogPositiveCommand = new RelayCommand<string>( (axis_name) => ExecuteJogPositive( axis_name));
            JogNegativeCommand = new RelayCommand<string>( (axis_name) => ExecuteJogNegative( axis_name));
            EnableXCommand = new RelayCommand( () => ExecuteEnableCommand( IXyz.Axes.X));
            EnableYCommand = new RelayCommand( () => ExecuteEnableCommand( IXyz.Axes.Y));
            EnableZCommand = new RelayCommand( () => ExecuteEnableCommand( IXyz.Axes.Z));
        }

        private void ExecuteMoveToTeachpoint()
        {
        }

        private void ExecuteUpdateTeachpoint()
        {
        }

        private void ExecuteHomeAllAxes()
        {
            _device.Home();
        }

        private void ExecuteHomeAxis( string axis_name)
        {
            if( axis_name.ToUpper() == "X")
                _controller.Stage.HomeAxis( HardwareInterfaces.IXyz.Axes.X, false);
            else if( axis_name.ToUpper() == "Y")
                _controller.Stage.HomeAxis( HardwareInterfaces.IXyz.Axes.Y, false);
            else if( axis_name.ToUpper() == "Z")
                _controller.Stage.HomeAxis( HardwareInterfaces.IXyz.Axes.Z, false);
        }

        private void ExecuteJogPositive( string axis_name)
        {
            if( axis_name.ToUpper() == "X")
                _controller.Stage.MoveRelative( IXyz.Axes.X, XAxisIncrementItems.CurrentItem.ToString().ToDouble(), 0, 0, 0, true);
            else if( axis_name.ToUpper() == "Y")
                _controller.Stage.MoveRelative( IXyz.Axes.Y, YAxisIncrementItems.CurrentItem.ToString().ToDouble(), 0, 0, 0, true);
            else if( axis_name.ToUpper() == "Z")
                _controller.Stage.MoveRelative( IXyz.Axes.Z, ZAxisIncrementItems.CurrentItem.ToString().ToDouble(), 0, 0, 0, true);
        }

        private void ExecuteJogNegative( string axis_name)
        {
            if( axis_name.ToUpper() == "X")
                _controller.Stage.MoveRelative( IXyz.Axes.X, -XAxisIncrementItems.CurrentItem.ToString().ToDouble(), 0, 0, 0, true);
            else if( axis_name.ToUpper() == "Y")
                _controller.Stage.MoveRelative( IXyz.Axes.Y, -YAxisIncrementItems.CurrentItem.ToString().ToDouble(), 0, 0, 0, true);
            else if( axis_name.ToUpper() == "Z")
                _controller.Stage.MoveRelative( IXyz.Axes.Z, -ZAxisIncrementItems.CurrentItem.ToString().ToDouble(), 0, 0, 0, true);
        }

        private void ExecuteEnableCommand( IXyz.Axes axis)
        {
            bool currently_enabled = _controller.Stage.IsAxisEnabled( axis);
            _controller.Stage.EnableAxis( axis, !currently_enabled);
        }
        
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _updater.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _updater.Stop();
        }
    }
}
