using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using BioNex.Hive.Hardware;
using BioNex.Shared.DeviceInterfaces;
using GalaSoft.MvvmLight.Command;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for TeachpointInterpolation.xaml
    /// </summary>
    public partial class TeachpointInterpolationDialog : Window, INotifyPropertyChanged
    {
        private readonly HivePlugin _controller;

        public RelayCommand InterpolateTeachpointsCommand { get; set; }

        public List<int> NumberOfShelvesItems { get; set; }
        public int NumberOfShelves { get; set; }
        public List<int> NumberOfRacksItems { get; set; }
        public int NumberOfRacks { get; set; }
        
        public ObservableCollection<InterpolatedTeachpointInfo> InterpolatedTeachpoints { get; set; }

        // for binding the comboboxes to an event
        public ICollectionView TopTeachpointView { get; private set; }
        public ICollectionView BottomTeachpointView { get; private set; }
        public ICollectionView BottomRightTeachpointView { get; private set; }
        public ICollectionView NumberOfShelvesView { get; private set; }
        public ICollectionView NumberOfRacksView { get; private set; }

        public ObservableCollection<string> TopTeachpointNames { get; set; }
        public ObservableCollection<string> BottomTeachpointNames { get; set; }
        public ObservableCollection<string> BottomRightTeachpointNames { get; set; }

        /// <summary>
        /// the device whose locations we want to interpolate
        /// </summary>
        private DeviceInterface _device { get; set; }

        public TeachpointInterpolationDialog( HivePlugin controller, DeviceInterface device)
        {
            InitializeComponent();
            _controller = controller;
            _device = device;

            NumberOfShelvesItems = new List<int>();
            NumberOfRacksItems = new List<int>();
            TopTeachpointNames = new ObservableCollection<string>(_controller.GetTeachpointNames()[_device.Name]);
            BottomTeachpointNames = new ObservableCollection<string>( _controller.GetTeachpointNames()[_device.Name]);
            BottomRightTeachpointNames = new ObservableCollection<string>(_controller.GetTeachpointNames()[_device.Name]);
            
            // this is how you deal with notifications in MVVM for updating combobox selections
            InterpolatedTeachpoints = new ObservableCollection<InterpolatedTeachpointInfo>();
            TopTeachpointView = CollectionViewSource.GetDefaultView( TopTeachpointNames);
            TopTeachpointView.CurrentChanged += new EventHandler( OnParameterChanged);
            BottomTeachpointView = CollectionViewSource.GetDefaultView(BottomTeachpointNames);
            BottomTeachpointView.CurrentChanged += new EventHandler(OnParameterChanged);
            BottomRightTeachpointView = CollectionViewSource.GetDefaultView(BottomRightTeachpointNames);
            BottomRightTeachpointView.CurrentChanged += new EventHandler(OnParameterChanged);
            NumberOfShelvesView = CollectionViewSource.GetDefaultView(NumberOfShelvesItems);
            NumberOfShelvesView.CurrentChanged += new EventHandler(OnParameterChanged);
            NumberOfRacksView = CollectionViewSource.GetDefaultView(NumberOfRacksItems);
            NumberOfRacksView.CurrentChanged += new EventHandler(OnParameterChanged);
            
            // commands
            InterpolateTeachpointsCommand = new RelayCommand( SaveInterpolatedTeachpoints);

            for( int i=2; i<=26; i++)
                NumberOfShelvesItems.Add( i);
            OnPropertyChanged( "NumberOfShelvesItems");
            NumberOfShelves = 7;

            for (int i = 1; i <= 18; ++i)
                NumberOfRacksItems.Add(i);
            OnPropertyChanged("NumberOfRacksItems");
            NumberOfRacks = 1;

            DataContext = this;
        }

        private void OnParameterChanged( object sender, EventArgs e)
        {
            BottomRightTeachpointLabel.IsEnabled = BottomRightTeachpointComboBox.IsEnabled = NumberOfRacks > 1;
            Interpolate();
        }

        public void Interpolate()
        {

            string top_name = TopTeachpointView.CurrentItem == null ? "" : TopTeachpointView.CurrentItem.ToString();
            string bottom_name = BottomTeachpointView.CurrentItem == null ? "" : BottomTeachpointView.CurrentItem.ToString();
            string bottom_right_name = BottomRightTeachpointView.CurrentItem == null ? "" : BottomRightTeachpointView.CurrentItem.ToString();

            var new_list = TeachpointInterpolation.GetInterpolatedTeachpoints(_controller, _device.Name, "", top_name, bottom_name, bottom_right_name, NumberOfShelves, NumberOfRacks);
            InterpolatedTeachpoints.Clear();
            foreach (var tp in new_list)
                InterpolatedTeachpoints.Add(tp);
        }

        public void SaveInterpolatedTeachpoints()
        {
            Interpolate();
            foreach( InterpolatedTeachpointInfo iti in InterpolatedTeachpoints) 
                _controller.Hardware.SetTeachpoint(_device.Name, (HiveTeachpoint)iti);
            _controller.SaveTeachpointFile( _device as AccessibleDeviceInterface);
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
