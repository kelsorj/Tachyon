using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using BioNex.Hive.Hardware;
using BioNex.Shared.BioNexGuiControls;

namespace BioNex.HivePrototypePlugin
{
    public partial class HivePlugin
    {
        public HiveWorldPoint CurrentWorldPosition { get{ return Hardware.CurrentWorldPosition; }}
        public HiveToolPoint CurrentToolPosition { get{ return Hardware.CurrentToolPosition; }}
        // ----------------------------------------------------------------------
        //! \todo figure out if there's a way to get databinding to work so that the view is
        //!       notified when a MEMBER of a class gets modified.
        // ----------------------------------------------------------------------

        // ----------------------------------------------------------------------
        // controls.
        // ----------------------------------------------------------------------
        /// <summary>
        /// used to allow the X and Z axes to move, even if the Theta axis is not in the safe position
        /// </summary>
        private bool _override_theta_check;
        public bool OverrideThetaCheck
        {
            get { return _override_theta_check; }
            set {
                _override_theta_check = value;
                OnPropertyChanged( "OverrideThetaCheck");
            }
        }
        // ----------------------------------------------------------------------
        public ObservableCollection< ReteachPreviewInfo> ReteachPreview { get; set; }
        // ----------------------------------------------------------------------
        public ObservableCollection< RackView> Racks
        {
            get { return _plate_location_manager.Racks; }
            set {
                _plate_location_manager.Racks = Racks;
            }
        }
        // ----------------------------------------------------------------------
        private Visibility engineering_tab_visibility_;
        public Visibility EngineeringTabVisibility
        {
            get { return engineering_tab_visibility_; }
            set {
                engineering_tab_visibility_ = value;
                OnPropertyChanged( "EngineeringTabVisibility");
            }
        }
        // ----------------------------------------------------------------------
        private Visibility maintenance_tab_visibility_;
        public Visibility MaintenanceTabVisibility
        {
            get { return maintenance_tab_visibility_; }
            set {
                maintenance_tab_visibility_ = value;
                OnPropertyChanged( "MaintenanceTabVisibility");
            }
        }
        // ----------------------------------------------------------------------
        // this gets modified by the user in the GUI, and the Filter delegate will use it to 
        // determine what to display in the TeachpointNames droplist
        // Primary teachpoint and Teachpoint B SHARE FILTERS!!!
        private string _teachpoint_filter;
        public string TeachpointFilter
        {
            get { return _teachpoint_filter; }
            set {
                _teachpoint_filter = value;
                OnPropertyChanged( "TeachpointFilter");
                ReloadDeviceTeachpoints();
                ReloadDeviceBTeachpoints();
            }
        }
        // ----------------------------------------------------------------------
        private string _teachpoint_a_filter;
        public string TeachpointAFilter
        {
            get { return _teachpoint_a_filter; }
            set {
                _teachpoint_a_filter = value;
                OnPropertyChanged( "TeachpointAFilter");
                ReloadDeviceATeachpoints();
            }
        }
        // ----------------------------------------------------------------------
        private bool _initialized;
        public bool Initialized
        {
            get { return _initialized; }
            set {
                _initialized = value;
                OnPropertyChanged( "Initialized");
            }
        }
        // ----------------------------------------------------------------------
        private bool _bcr_connected;
        public bool BarcodeReaderConnected
        {
            get { return _bcr_connected; }
            set {
                _bcr_connected = value;
                OnPropertyChanged( "BarcodeReaderConnected");
            }
        }
        // ----------------------------------------------------------------------
        private double _x_increment;
        public double XIncrement {
            get { return _x_increment; }
            set {
                _x_increment = value;
                OnPropertyChanged( "XIncrement");
            }
        }
        // ----------------------------------------------------------------------
        private double _y_increment;
        public double YIncrement {
            get { return _y_increment; }
            set {
                _y_increment = value;
                OnPropertyChanged( "YIncrement");
            }
        }
        // ----------------------------------------------------------------------
        private double _z_increment;
        public double ZIncrement {
            get { return _z_increment; }
            set {
                _z_increment = value;
                OnPropertyChanged( "ZIncrement");
            }
        }
        // ----------------------------------------------------------------------
        public double ThetaIncrement { get; set; }
        public double GripperIncrement { get; set; }
        // ----------------------------------------------------------------------
        private bool _telemetry_enabled;
        public bool TelemetryEnabled
        {
            get { return _telemetry_enabled; }
            set {
                _telemetry_enabled = value;
                OnPropertyChanged( "TelemetryEnabled");
            }
        }
        // ----------------------------------------------------------------------
        private List<string> _labware_names;
        public List<string> LabwareNames
        {
            get { return _labware_names; }
            set {
                _labware_names = value;
                OnPropertyChanged( "LabwareNames");
            }
        }
        // ----------------------------------------------------------------------
        private double _approach_height;
        public double ApproachHeight
        {
            get { return _approach_height; }
            set {
                _approach_height = value;
                OnPropertyChanged( "ApproachHeight");
            }
        }
        // ----------------------------------------------------------------------
        /*
        private double _cam_offset = 0.0; // cam table offset (CAMOFF var on slave), but in mm here
        public double CamOffset
        {
            get { return _cam_offset; }
            set {
                _cam_offset = value;
                OnPropertyChanged( "CamOffset");
            }
        }
        */
        // ----------------------------------------------------------------------
        /*
        private ObservableCollection<string> _z_hover_items = new ObservableCollection<string>();
        public ObservableCollection<string> ZHoverItems
        {
            get { return _z_hover_items; }
            set {
                _z_hover_items = value;
                OnPropertyChanged( "ZHoverItems");
            }
        }
        */
        // ----------------------------------------------------------------------
        private HiveTeachpoint _teachpoint_position = new HiveTeachpoint();
        public HiveTeachpoint TeachpointPosition
        {
            get { return _teachpoint_position; }
            set {
                _teachpoint_position = value;
                OnPropertyChanged( "TeachpointPosition");
            }
        }
        // ----------------------------------------------------------------------
        private double _new_approach_height;
        public double NewApproachHeight
        {
            get { return _new_approach_height; }
            set {
                _new_approach_height = value;
                OnPropertyChanged( "NewApproachHeight");
            }
        }
        // ----------------------------------------------------------------------
        private string _pickandplace_labware;
        public string PickAndPlaceLabware
        {
            get { return _pickandplace_labware; }
            set {
                _pickandplace_labware = value;
                OnPropertyChanged( "PickAndPlaceLabware");
            }
        }
        // ----------------------------------------------------------------------
        public ICollectionView TeachpointNames { get; set; }
        public ICollectionView TeachpointANames { get; set; }
        public ICollectionView TeachpointBNames { get; set; }
        public ICollectionView AccessibleDeviceView { get; set; }
        public ICollectionView DeviceAView { get; set; }
        public ICollectionView DeviceBView { get; set; }
        // ----------------------------------------------------------------------
        private string _status_text;
        public string StatusText
        {
            get { return _status_text; }
            set {
                _status_text = value;
                OnPropertyChanged( "StatusText");
            }
        }
        // ----------------------------------------------------------------------
        private AxisBoolStatus _axis_home_status = new AxisBoolStatus();
        public AxisBoolStatus AxisHomeStatus
        {
            get { return _axis_home_status; }
            set {
                _axis_home_status = value;
                OnPropertyChanged( "AxisHomeStatus");
            }
        }
        // ----------------------------------------------------------------------
        private AxisBoolStatus _servo_status = new AxisBoolStatus();
        public AxisBoolStatus ServoOnStatus
        {
            get { return _servo_status; }
            set {
                _servo_status = value;
                OnPropertyChanged( "ServoOnStatus");
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Whether or not the plate is in portrait or landscape position
        /// </summary>
        /// <remarks>
        /// landscape == 0
        /// portrait == 1
        /// </remarks>
        private HiveTeachpoint.TeachpointOrientation _plate_orientation;
        public HiveTeachpoint.TeachpointOrientation PlateOrientation
        {
            get { return _plate_orientation; }
            set {
                _plate_orientation = value;
                OnPropertyChanged( "PlateOrientation");
            }
        }
        // ----------------------------------------------------------------------
        private string _selected_teachpoint_file;
        public string SelectedTeachpointFile
        { 
            get { return _selected_teachpoint_file; }
            set {
                _selected_teachpoint_file = value;
                OnPropertyChanged( "SelectedTeachpointFile");
            }
        }
        // ----------------------------------------------------------------------
        private string _selected_tsm_setup_folder;
        public string SelectedTSMSetupFolder
        {
            get { return _selected_tsm_setup_folder; }
            set {
                _selected_tsm_setup_folder = value;
                OnPropertyChanged( "SelectedTSMSetupFolder");
            }
        }
        // ----------------------------------------------------------------------
        private string _selected_motor_settings_file;
        public string SelectedMotorSettingsFile
        {
            get { return _selected_motor_settings_file; }
            set {
                _selected_motor_settings_file = value;
                OnPropertyChanged( "SelectedMotorSettingsFile");
            }
        }
        // ----------------------------------------------------------------------
        private string _selected_inventory_file;
        public string SelectedInventoryFile
        {
            get { return _selected_inventory_file; }
            set {
                _selected_inventory_file = value;
                OnPropertyChanged( "SelectedInventoryFile");
            }
        }
        // ----------------------------------------------------------------------
        public string ReteachUserPrompt { get; set; }

        // ----------------------------------------------------------------------
        // tooltips.
        // ----------------------------------------------------------------------
        private string reinventory_button_tooltip_;
        public string ReinventoryButtonToolTip
        { 
            get { return reinventory_button_tooltip_; }
            set {
                reinventory_button_tooltip_ = value;
                OnPropertyChanged( "ReinventoryButtonToolTip");
            }
        }
        // ----------------------------------------------------------------------
        /*
        private string abort_reinventory_tooltip_;
        public string AbortReinventoryToolTip
        {
            get { return abort_reinventory_tooltip_; }
            set {
                abort_reinventory_tooltip_ = value;
                OnPropertyChanged( "AbortReinventoryToolTip");
            }
        }
        */
        // ----------------------------------------------------------------------
        private string move_to_teachpoint_tooltip_;
        public string MoveToTeachpointToolTip
        {
            get { return move_to_teachpoint_tooltip_; }
            set {
                move_to_teachpoint_tooltip_ = value;
                OnPropertyChanged( "MoveToTeachpointToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string _update_approach_height_tooltip;
        public string UpdateApproachHeightToolTip
        {
            get { return _update_approach_height_tooltip; }
            set {
                _update_approach_height_tooltip = value;
                OnPropertyChanged( "UpdateApproachHeightToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string update_teachpoint_tooltip_;
        public string UpdateTeachpointToolTip
        {
            get { return update_teachpoint_tooltip_; }
            set {
                update_teachpoint_tooltip_ = value;
                OnPropertyChanged( "UpdateTeachpointToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string save_teachpoint_tooltip_;
        public string SaveTeachpointToolTip
        {
            get { return save_teachpoint_tooltip_; }
            set {
                save_teachpoint_tooltip_ = value;
                OnPropertyChanged( "SaveTeachpointToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string grip_plate_tooltip_;
        public string GripPlateToolTip
        {
            get { return grip_plate_tooltip_; }
            set {
                grip_plate_tooltip_ = value;
                OnPropertyChanged( "GripPlateToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string ungrip_plate_tooltip_;
        public string UngripPlateToolTip
        {
            get { return ungrip_plate_tooltip_; }
            set {
                ungrip_plate_tooltip_ = value;
                OnPropertyChanged( "UngripPlateToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string reinventory_selected_tooltip_;
        public string ReinventorySelectedToolTip
        {
            get { return reinventory_selected_tooltip_; }
            set {
                reinventory_selected_tooltip_ = value;
                OnPropertyChanged( "ReinventorySelectedToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string jog_x_tooltip_;
        public string JogXToolTip
        {
            get { return jog_x_tooltip_; }
            set {
                jog_x_tooltip_ = value;
                OnPropertyChanged( "JogXToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string jog_z_tooltip_;
        public string JogZToolTip
        {
            get { return jog_z_tooltip_; }
            set {
                jog_z_tooltip_ = value;
                OnPropertyChanged( "JogZToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string jog_y_tooltip_;
        public string JogYToolTip
        {
            get { return jog_y_tooltip_; }
            set {
                jog_y_tooltip_ = value;
                OnPropertyChanged( "JogYToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string jog_theta_tooltip_;
        public string JogThetaToolTip
        {
            get { return jog_theta_tooltip_; }
            set {
                jog_theta_tooltip_ = value;
                OnPropertyChanged( "JogThetaToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string jog_gripper_tooltip_;
        public string JogGripperToolTip
        {
            get { return jog_gripper_tooltip_; }
            set {
                jog_gripper_tooltip_ = value;
                OnPropertyChanged( "JogGripperToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string park_robot_tooltip_;
        public string ParkRobotToolTip
        {
            get { return park_robot_tooltip_; }
            set {
                park_robot_tooltip_ = value;
                OnPropertyChanged( "ParkRobotToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string _home_all_axes_tooltip;
        public string HomeAllAxesToolTip
        {
            get { return _home_all_axes_tooltip; }
            set {
                _home_all_axes_tooltip = value;
                OnPropertyChanged( "HomeAllAxesToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string teachpoint_interpolation_tooptip_;
        public string TeachpointInterpolationToolTip
        {
            get { return teachpoint_interpolation_tooptip_; }
            set
            {
                teachpoint_interpolation_tooptip_ = value;
                OnPropertyChanged("TeachpointInterpolationToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string transform_teachpoint_tooptip_;
        public string TransformTeachpointToolTip
        {
            get { return transform_teachpoint_tooptip_; }
            set
            {
                transform_teachpoint_tooptip_ = value;
                OnPropertyChanged("TransformTeachpointToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string _tooltip_movetoteachpoint;
        public string ToolTipMoveToTeachpoint
        {
            get { return _tooltip_movetoteachpoint; }
            set {
                _tooltip_movetoteachpoint = value;
                OnPropertyChanged( "ToolTipMoveToTeachpoint");
            }
        }
        // ----------------------------------------------------------------------
        private string _tooltip_picka;
        public string ToolTipPickA
        {
            get { return _tooltip_picka; }
            set {
                _tooltip_picka = value;
                OnPropertyChanged( "ToolTipPickA");
            }
        }
        // ----------------------------------------------------------------------
        private string _tooltip_pickb;
        public string ToolTipPickB
        {
            get { return _tooltip_pickb; }
            set {
                _tooltip_pickb = value;
                OnPropertyChanged( "ToolTipPickB");
            }
        }
        // ----------------------------------------------------------------------
        private string _tooltip_placea;
        public string ToolTipPlaceA
        {
            get { return _tooltip_placea; }
            set {
                _tooltip_placea = value;
                OnPropertyChanged( "ToolTipPlaceA");
            }
        }
        // ----------------------------------------------------------------------
        private string _tooltip_placeb;
        public string ToolTipPlaceB
        {
            get { return _tooltip_placeb; }
            set {
                _tooltip_placeb = value;
                OnPropertyChanged( "ToolTipPlaceB");
            }
        }
        // ----------------------------------------------------------------------
        private string _tooltip_transferab;
        public string ToolTipTransferAB
        {
            get { return _tooltip_transferab; }
            set {
                _tooltip_transferab = value;
                OnPropertyChanged( "ToolTipTransferAB");
            }
        }
        // ----------------------------------------------------------------------
        private string _tooltip_transferba;
        public string ToolTipTransferBA
        {
            get { return _tooltip_transferba; }
            set {
                _tooltip_transferba = value;
                OnPropertyChanged( "ToolTipTransferBA");
            }
        }
    }
}
