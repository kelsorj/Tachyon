using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using BioNex.Hive.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Command;

namespace BioNex.HivePrototypePlugin
{
    public enum PreviewType
    {
        Offset, Interpolated, Transformed
    }

    /// <summary>
    /// Interaction logic for TeachpointInterpolation.xaml
    /// </summary>
    public partial class TeachpointTransformationDialog : Window, INotifyPropertyChanged
    {
        private readonly HivePlugin _controller;

        public RelayCommand TransformTeachpointsCommand { get; private set; }
        public RelayCommand ConnectRemoteAddressCommand { get; private set; }

        public List<int> NumberOfShelvesItems { get; set; }
        public int NumberOfShelves { get; set; }
        public List<int> NumberOfRacksItems { get; set; }
        public int NumberOfRacks { get; set; }

        public ObservableCollection<InterpolatedTeachpointInfo> RefinementOffsets { get; set; }
        public ObservableCollection<InterpolatedTeachpointInfo> DisplayedTeachpoints { get; set; }
        public ObservableCollection<InterpolatedTeachpointInfo> TransformedTeachpoints { get; set; }

        // for binding the comboboxes to an event
        public ICollectionView ToDeviceView { get; private set; }
        public ICollectionView FromTeachpointView { get; private set; }
        public ICollectionView ToTeachpointView { get; private set; }
        public ICollectionView NumberOfShelvesView { get; private set; }
        public ICollectionView NumberOfRacksView { get; private set; }

        public ObservableCollection<string> ToDeviceNames { get; set; }
        public ObservableCollection<string> FromTeachpointNames { get; set; }
        public ObservableCollection<string> ToTeachpointNames { get; set; }

        string _transformationLabel;
        public string TransformationLabel
        {
            get
            {
                return _transformationLabel;
            }
            set
            {
                _transformationLabel = value;
                OnPropertyChanged("TransformationLabel");
            }
        }
        

        public string FromDevice { get; set; }

        public string RemoteAddress{ get; set; }
        TeachpointXmlRpcClient _xmlrpc_client;

        private bool _to_device_dockable_enabled;
        public bool ToDeviceDockableEnabled
        {
            get { return _to_device_dockable_enabled; }
            set {
                _to_device_dockable_enabled = value;
                OnPropertyChanged( "ToDeviceDockableEnabled");
            }
        }

        private string _to_device_dockable_barcode;
        public string ToDeviceDockableBarcode
        {
            get { return _to_device_dockable_barcode; }
            set {
                _to_device_dockable_barcode = value;
                OnPropertyChanged( "ToDeviceDockableBarcode");
                OnToDeviceChanged( this, null);
            }
        }

        private bool _from_device_dockable_enabled;
        public bool FromDeviceDockableEnabled
        {
            get { return _from_device_dockable_enabled; }
            set {
                _from_device_dockable_enabled = value;
                OnPropertyChanged( "FromDeviceDockableEnabled");
            }
        }

        private string _from_device_dockable_barcode;
        public string FromDeviceDockableBarcode
        {
            get { return _from_device_dockable_barcode; }
            set {
                _from_device_dockable_barcode = value;
                OnPropertyChanged( "FromDeviceDockableBarcode");
                OnFromDockableIDChanged( this, null);
            }
        }

        private PreviewType _the_preview_type = PreviewType.Transformed;
        public PreviewType ThePreviewType
        {
            get
            {
                return _the_preview_type;
            }
            set
            {
                _the_preview_type = value;
                OnParameterChanged(this, null);
            }
        }

        bool _canSaveTeachpoints;

        public TeachpointTransformationDialog( HivePlugin controller, string device_name)
        {
            InitializeComponent();
            try {
                _controller = controller;
                RemoteAddress = "localhost";
                TransformationLabel = "When you click Transform, the refinements will be transformed to the 'To Device' reference frame, generating these points:";

                NumberOfShelvesItems = new List<int>();
                NumberOfRacksItems = new List<int>();

                IEnumerable< string> device_names = _controller.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().Select( adi => adi.Name);
            
                ToDeviceNames = new ObservableCollection<string>(device_names);

                var names = _controller.GetTeachpointNames();
                var from_name = names.ContainsKey(device_name) ? names[device_name] : new List<string>();
                FromTeachpointNames = new ObservableCollection<string>(from_name);

                var to_name = names.ContainsKey(device_names.First()) ? names[device_names.First()] : new List<string>();
                ToTeachpointNames = new ObservableCollection<string>(to_name);
            
                RefinementOffsets = new ObservableCollection<InterpolatedTeachpointInfo>();
                DisplayedTeachpoints = new ObservableCollection<InterpolatedTeachpointInfo>();
                TransformedTeachpoints = new ObservableCollection<InterpolatedTeachpointInfo>();

                ToDeviceView = CollectionViewSource.GetDefaultView(ToDeviceNames);
                FromTeachpointView = CollectionViewSource.GetDefaultView(FromTeachpointNames);
                ToTeachpointView = CollectionViewSource.GetDefaultView(ToTeachpointNames);
                NumberOfShelvesView = CollectionViewSource.GetDefaultView(NumberOfShelvesItems);
                NumberOfRacksView = CollectionViewSource.GetDefaultView(NumberOfRacksItems);
            
                // commands
                TransformTeachpointsCommand = new RelayCommand( SaveTransformedTeachpoints, () => _canSaveTeachpoints == true);
                ConnectRemoteAddressCommand = new RelayCommand( ConnectRemoteAddress);

                FromDevice = device_name;
                OnPropertyChanged("FromDevice");

                for( int i=2; i<=24; i++)
                    NumberOfShelvesItems.Add( i);
                OnPropertyChanged( "NumberOfShelvesItems");
                NumberOfShelves = 7;

                for (int i = 2; i <= 12; ++i)
                    NumberOfRacksItems.Add(i);
                OnPropertyChanged("NumberOfRacksItems");
                NumberOfRacks = 2;

                var available_docks = _controller.DataRequestInterface.Value.GetDockablePlateStorageInterfaces();
                var matching_dock = available_docks.Where( (x) => (x as DeviceInterface).Name == FromDevice).FirstOrDefault();
                FromDeviceDockableEnabled = matching_dock != null;


                DataContext = this;

                ToDeviceView.CurrentChanged += new EventHandler(OnToDeviceChanged);
                FromTeachpointView.CurrentChanged += new EventHandler(OnParameterChanged);
                ToTeachpointView.CurrentChanged += new EventHandler(OnParameterChanged);
                NumberOfShelvesView.CurrentChanged += new EventHandler(OnParameterChanged);
                NumberOfRacksView.CurrentChanged += new EventHandler(OnParameterChanged);
            } catch( Exception ex) {
                MessageBox.Show( String.Format( "Could not initialize teachpoint transformation dialog: {0}", ex.Message));
            }
        }

        public void ConnectRemoteAddress()
        {
            try
            {
                _xmlrpc_client = new TeachpointXmlRpcClient(_controller.Name, RemoteAddress, _controller.Config.TeachpointServicePort);

                FromTeachpointNames.Clear();
                FromTeachpointView.MoveCurrentToFirst();


                // --> if FromDevice is a dockable device, we need to use some special stuff

                // determine whether or not to enable the dockable id textbox
                FromDeviceDockableEnabled = _xmlrpc_client.IsDock(FromDevice);           
                var dockable_id = FromDeviceDockableEnabled ? FromDeviceDockableBarcode : null;
                
                var new_list = _xmlrpc_client.GetTeachpointNames(FromDevice, FromDeviceDockableBarcode);
                foreach (var tp in new_list)
                    FromTeachpointNames.Add(tp);
                FromTeachpointView.MoveCurrentToFirst();
            }
            catch (Exception e)
            {
                var message = string.Format( "Failed to retrieve teachpoint list from device '{0}' at remote machine '{1}' see log for details", _controller.Name, RemoteAddress);
                HivePlugin._log.Error( message + ":" + e);
                TransformationLabel = message;
                return;
            }


        }

        private void OnToDeviceChanged(object sender, EventArgs e)
        {
            ToTeachpointNames.Clear();

            // determine whether or not to enable the dockable id textbox
            var to_device_name = ToDeviceView.CurrentItem.ToString();
            ToDeviceDockableEnabled = _controller.DataRequestInterface.Value.GetDockablePlateStorageInterfaces().Where( (x) => (x as DeviceInterface).Name == to_device_name).FirstOrDefault() != null;

            IList<string> new_list = new List<string>();
                      
            if( ToDeviceDockableEnabled) {
                _controller.Hardware.LoadTeachpoints( _controller.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == to_device_name));
                new_list = _controller.Hardware.GetTeachpointNames( to_device_name);
            } else {
                var names = _controller.GetTeachpointNames();
                if( names.ContainsKey(to_device_name))
                    new_list = names[to_device_name];
            }

            foreach (var tp in new_list)
                ToTeachpointNames.Add(tp);
            ToTeachpointView.MoveCurrentToFirst();
        }

        private HiveTeachpoint GetTeachpoint(bool local_only, string device, string dockable_barcode, string teachpoint_name)
        {
            if( local_only)
            {
                bool dockable = _controller.DataRequestInterface.Value.GetDockablePlateStorageInterfaces().Where( (x) => (x as DeviceInterface).Name == device).FirstOrDefault() != null;
                if(!dockable)
                    return _controller.Hardware.GetTeachpoint(device, teachpoint_name);
                _controller.Hardware.LoadTeachpoints( _controller.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == device));
                HiveTeachpoint teachpoint = _controller.Hardware.GetTeachpoint( device, teachpoint_name);
                if( teachpoint != null){
                    return teachpoint;
                }
                throw new TeachpointNotFoundException( teachpoint_name);
            }
            else
                return _xmlrpc_client.GetTeachpoint(device, dockable_barcode, teachpoint_name);
        }

        private void OnFromDockableIDChanged(object sender, EventArgs e)
        {
            bool local_only = _xmlrpc_client == null;
            if( !local_only)
            {
                ConnectRemoteAddress();
                return;
            }

        
            FromTeachpointNames.Clear();
            FromTeachpointView.MoveCurrentToFirst();


            // --> if FromDevice is a dockable device, we need to use some special stuff

            // determine whether or not to enable the dockable id textbox
            FromDeviceDockableEnabled = _controller.DataRequestInterface.Value.GetDockablePlateStorageInterfaces().Where( (x) => (x as DeviceInterface).Name == FromDevice).FirstOrDefault() != null;
                
            IList<string> new_list = new List<string>();

            if( FromDeviceDockableEnabled) {
                _controller.Hardware.LoadTeachpoints( _controller.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == FromDevice));
                new_list = _controller.Hardware.GetTeachpointNames( FromDevice);
            } else {

                var names = _controller.GetTeachpointNames();
                if( names.ContainsKey(FromDevice))
                    new_list = names[FromDevice];
            }

            foreach (var tp in new_list)
                FromTeachpointNames.Add(tp);
            FromTeachpointView.MoveCurrentToFirst();
        
        }

        private void OnParameterChanged( object sender, EventArgs e)
        {
            Transform();
        }

        public void Transform()
        {
            _canSaveTeachpoints = false;
            RefinementOffsets.Clear();
            DisplayedTeachpoints.Clear();
            TransformedTeachpoints.Clear();

            int racks = NumberOfRacks;
            int slots = NumberOfShelves;

            string from_top_name = FromTeachpointView.CurrentItem == null ? "" : FromTeachpointView.CurrentItem.ToString();
            int first_rack = 1;
            int first_slot = 1;
            from_top_name.GetLastTwoNumbers(ref first_rack, ref first_slot);

            string from_bottom_name = from_top_name.ReplaceLastNumberWith(slots + first_slot - 1);
            string from_bottom_right_name = from_top_name.ReplaceLastTwoNumbersWith( racks + first_rack - 1, slots + first_slot - 1);
            
            string to_device = ToDeviceView.CurrentItem == null ? "" : ToDeviceView.CurrentItem.ToString();
            string to_top_name = ToTeachpointView.CurrentItem == null ? "" : ToTeachpointView.CurrentItem.ToString();
            string to_bottom_name = to_top_name.ReplaceLastNumberWith(slots);
            string to_bottom_right_name = to_top_name.ReplaceLastTwoNumbersWith(racks, slots);

            if (FromDevice == "" || from_top_name == "" || to_device == "" || to_top_name == "")
            {
                TransformationLabel = "Can't do transform, invalid device or teachpoint selected";
                return; // can't do it!
            }

            bool local_only = _xmlrpc_client == null;

            var set_1 = (local_only) 
                ? TeachpointInterpolation.GetInterpolatedTeachpoints(_controller, FromDevice, FromDeviceDockableBarcode, from_top_name, from_bottom_name, from_bottom_right_name, slots, racks)
                : TeachpointInterpolation.GetInterpolatedTeachpoints(_xmlrpc_client, FromDevice, FromDeviceDockableBarcode, from_top_name, from_bottom_name, from_bottom_right_name, slots, racks);
            var set_2 = TeachpointInterpolation.GetInterpolatedTeachpoints(_controller, to_device, ToDeviceDockableBarcode, to_top_name, to_bottom_name, to_bottom_right_name, slots, racks);

            var refinement_offsets = new List< InterpolatedTeachpointInfo>(set_1.Count);
            var rotated_points = new List< InterpolatedTeachpointInfo>(set_2.Count);
            var transformed_points = new List< InterpolatedTeachpointInfo>(set_2.Count);
            set_1.ForEach((i) => { refinement_offsets.Add(new InterpolatedTeachpointInfo(i)); });
            set_2.ForEach((i) => { rotated_points.Add(new InterpolatedTeachpointInfo(i)); transformed_points.Add(new InterpolatedTeachpointInfo(i)); });

            if (set_1.Count == 0)
            {
                TransformationLabel = "Can't do transform, 'From' teachpoint set is empty";
                return;  // can't do it!
            }

            // step 1 -- get "refinement offsets by subtracting interpolation from actual teachpoints
            try
            {
                foreach(var i in refinement_offsets)
                {
                    var tp = GetTeachpoint(local_only, FromDevice, FromDeviceDockableBarcode, i.Name);
                    i.X = tp.X - i.X;
                    i.Y = tp.Y - i.Y;
                    i.Z = tp.Z - i.Z;
                }
            }
            catch(KeyNotFoundException )
            {
                TransformationLabel = "Can't do transform, invalid or non-existent teachpoint in set";
                return; // Can't do it, non existent teachpoint in set
            }

            // update Observed collections for UI-1
            foreach (var tp in refinement_offsets)
                RefinementOffsets.Add(tp);

            if (set_2.Count == 0)
            {
                TransformationLabel = "Can't do transform, 'To' teachpoint set is empty";
                return; // can't do it!
            }
            
            // step 2 -- Get the 3 corners of the 2 interpolation planes
            var frame1_topleft = new Vector3D(set_1[0].X, set_1[0].Y, set_1[0].Z);
            var frame1_bottomleft = new Vector3D(set_1[slots - 1].X, set_1[slots - 1].Y, set_1[slots - 1].Z);
            var frame1_bottomright = new Vector3D(set_1[slots * racks - 1].X, set_1[slots * racks - 1].Y, set_1[slots * racks - 1].Z);

            var frame2_topleft = new Vector3D(set_2[0].X, set_2[0].Y, set_2[0].Z);
            var frame2_bottomleft = new Vector3D(set_2[slots - 1].X, set_2[slots - 1].Y, set_2[slots - 1].Z);
            var frame2_bottomright = new Vector3D(set_2[slots * racks - 1].X, set_2[slots * racks - 1].Y, set_2[slots * racks - 1].Z);

            // Step 3 Translate the planes so their bottom left corner is at the origin
            frame1_topleft -= frame1_bottomleft;
            frame1_bottomright -= frame1_bottomleft;
            frame1_bottomleft -= frame1_bottomleft;

            frame2_topleft -= frame2_bottomleft;
            frame2_bottomright -= frame2_bottomleft;
            frame2_bottomleft -= frame2_bottomleft;

            // step 4 Calculate plane normals via cross product
            var frame1_normal = frame1_topleft.CrossProduct(frame1_bottomright);
            var frame2_normal = frame2_topleft.CrossProduct(frame2_bottomright);

            // step 5 Dot product of plane normals gives rotation angle (radians)
            var angle = Vector3D.AngleBetween(frame1_normal, frame2_normal);

            // step 6 Cross product of plane normals gives roation axis
            var axis = frame1_normal.CrossProduct(frame2_normal).Normalize();

            // display the angle / axis in the TransformationLabel
            TransformationLabel = string.Format(
                "When you click Transform, the refinements will be transformed (angle: {0:0.00} axis:[{1:0.00}, {2:0.00}, {3:0.00}]) to the 'To Device' reference frame, generating these points:",
                (angle * 180.0 / Math.PI), axis.X, axis.Y, axis.Z);

            // step 7 Apply transform to refinement offsets
            // step 8 add transformed refinement to frame2 teachpoint

            for (int i = 0; i < refinement_offsets.Count; ++i)
            {
                var ro = refinement_offsets[i];
                var rp = rotated_points[i];
                var tp = transformed_points[i];
                var f2 = set_2[i];

                var rotated = Vector3D.RotateByAxisAngle(new Vector3D(ro.X, ro.Y, ro.Z), axis, angle);
                rp.X = rotated.X;
                rp.Y = rotated.Y;
                rp.Z = rotated.Z;

                tp.X = f2.X + rp.X;
                tp.Y = f2.Y + rp.Y;
                tp.Z = f2.Z + rp.Z;
                tp.ApproachHeight = f2.ApproachHeight;
            }
            
            // update the preview
            switch (_the_preview_type)
            {
                case PreviewType.Interpolated:
                    foreach (var tp in set_2) DisplayedTeachpoints.Add(tp); break;
                case PreviewType.Offset:
                    foreach (var tp in rotated_points) DisplayedTeachpoints.Add(tp); break;
                case PreviewType.Transformed:
                    foreach (var tp in transformed_points) DisplayedTeachpoints.Add(tp); break;
            }

            // update final output teachpoints
            foreach (var tp in transformed_points)
                TransformedTeachpoints.Add(tp);
            _canSaveTeachpoints = true;
        }

        public void SaveTransformedTeachpoints()
        {
            Transform();
            string to_device_name = ToDeviceView.CurrentItem.ToString();


            if( !ToDeviceDockableEnabled)
            {
                AccessibleDeviceInterface to_device = _controller.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == to_device_name);
                foreach( InterpolatedTeachpointInfo iti in TransformedTeachpoints)
                    _controller.Hardware.SetTeachpoint( to_device_name, (HiveTeachpoint)iti);
                _controller.SaveTeachpointFile( to_device);
                return;
            }
         

            // otherwise this is dock based teachpoints, save them to the file... 
            // use the magic fact that the _controller knows how to load, and this sets the file path so we can then save
            _controller.Hardware.LoadTeachpoints( _controller.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == to_device_name));
            foreach( InterpolatedTeachpointInfo iti in TransformedTeachpoints)
            {
                _controller.Hardware.SetTeachpoint( to_device_name, ( HiveTeachpoint)iti);
            }
            _controller.Hardware.SaveTeachpoints( _controller.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == to_device_name));
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
