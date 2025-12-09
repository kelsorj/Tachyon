using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using BioNex.Hive.Executor;
using BioNex.Hive.Hardware;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace BioNex.HivePrototypePlugin
{
    public partial class HivePlugin
    {
        // ----------------------------------------------------------------------
        public RelayCommand< bool> ConnectCommand { get; set; }
        public RelayCommand ReloadMotorSettingsCommand { get; set; }
        public RelayCommand< bool> ConnectBarcodeReaderCommand { get; set; }

        public RelayCommand< ServoOnOffAxisParameter> ServoAxisCommand { get; set; }
        public RelayCommand ServosOnCommand { get; set; }
        public RelayCommand ServosOffCommand { get; set; }

        public RelayCommand HomeXCommand { get; set; }
        public RelayCommand HomeZCommand { get; set; }
        public RelayCommand HomeThetaCommand { get; set; }
        public RelayCommand HomeGripperCommand { get; set; }
        public RelayCommand HomeAllAxesCommand { get; set; }

        public RelayCommand JogXNegativeCommand { get; set; }
        public RelayCommand JogXPositiveCommand { get; set; }
        public RelayCommand JogZDownCommand { get; set; }
        public RelayCommand JogZUpCommand { get; set; }
        public RelayCommand JogThetaAwayFromPlateCommand { get; set; }
        public RelayCommand JogThetaTowardsPlateCommand { get; set; }
        public RelayCommand JogGripperCloseCommand { get; set; }
        public RelayCommand JogGripperOpenCommand { get; set; }
        public RelayCommand JogYInCommand { get; set; }
        public RelayCommand JogYOutCommand { get; set; }

        public RelayCommand GripPlateCommand { get; set; }
        public RelayCommand UngripPlateCommand { get; set; }
        public RelayCommand ZUp10RetractCommand { get; set; }
        public RelayCommand GoHomeXCommand { get; set; }
        public RelayCommand GoHomeZCommand { get; set; }
        public RelayCommand GoHomeTCommand { get; set; }
        public RelayCommand GoHomeGCommand { get; set; }
        public RelayCommand ParkRobotCommand { get; set; }
        public RelayCommand MoveToTeachpointCommand { get; set; }

        public RelayCommand PickACommand { get; set; }
        public RelayCommand PickBCommand { get; set; }
        public RelayCommand PlaceACommand { get; set; }
        public RelayCommand PlaceBCommand { get; set; }
        public RelayCommand TransferABCommand { get; set; }
        public RelayCommand TransferBACommand { get; set; }

        public RelayCommand ReinventorySelectedRacksCommand { get; set; }
        public RelayCommand ReinventoryAllRacksCommand { get; set; }
        public RelayCommand AbortReinventoryCommand { get; set; }
        public RelayCommand UpdateInventoryViewCommand { get; set; }
        
        public RelayCommand UpdateApproachHeightCommand { get; set; }
        public RelayCommand ReloadTeachpointsCommand { get; set; }
        public RelayCommand UpdateTeachpointCommand { get; set; }
        public RelayCommand SaveTeachpointCommand { get; set; }
        public RelayCommand TeachpointInterpolationCommand { get; set; }
        public RelayCommand TransformTeachpointCommand { get; set; }
        public RelayCommand ReteachTeachpointCommand { get; set; }

        public RelayCommand StopAllCommand { get; set; }
        public RelayCommand ResetInterlocksCommand { get; set; }
        // ----------------------------------------------------------------------
        private void InitializeCommands()
        {
            // connection.
            ConnectCommand = new RelayCommand< bool>( Connect);                                                                                                // !!!Connect used elsewhere...
            ReloadMotorSettingsCommand = new RelayCommand( ExecuteReloadMotorSettings);
            ConnectBarcodeReaderCommand = new RelayCommand< bool>( ExecuteConnectBcr, CanExecuteConnectBcr);

            // enable/disable.
            ServoAxisCommand = new RelayCommand< ServoOnOffAxisParameter>( ExecuteServoAxis, p => CanExecuteServoOnOrOff());
            ServosOnCommand = new RelayCommand( ExecuteServoOn, CanExecuteServoOnOrOff);
            ServosOffCommand = new RelayCommand( ExecuteServoOff, CanExecuteServoOnOrOff);

            // homing.
            HomeXCommand = new RelayCommand( ExecuteHomeX, CanExecuteHomeX);
            HomeZCommand = new RelayCommand( ExecuteHomeZ, CanExecuteHomeZ);
            HomeThetaCommand = new RelayCommand( ExecuteHomeT, CanExecuteHomeT);
            HomeGripperCommand = new RelayCommand( ExecuteHomeG, CanExecuteHomeG);
            HomeAllAxesCommand = new RelayCommand( ExecuteHomeAllAxes, CanExecuteHomeAllAxes);

            // jogs.
            JogXNegativeCommand = new RelayCommand( () => ExecuteJogX( false), CanExecuteJogX);
            JogXPositiveCommand = new RelayCommand( () => ExecuteJogX( true), CanExecuteJogX);
            JogZDownCommand = new RelayCommand( () => ExecuteJogZ( false), CanExecuteJogZ);
            JogZUpCommand = new RelayCommand( () => ExecuteJogZ( true), CanExecuteJogZ);
            JogThetaAwayFromPlateCommand = new RelayCommand( () => ExecuteJogTheta( false), CanExecuteJogTheta);
            JogThetaTowardsPlateCommand = new RelayCommand( () => ExecuteJogTheta( true), CanExecuteJogTheta);
            JogGripperCloseCommand = new RelayCommand( () => ExecuteJogGripper( false), CanExecuteJogGripper);
            JogGripperOpenCommand = new RelayCommand( () => ExecuteJogGripper( true), CanExecuteJogGripper);
            JogYInCommand = new RelayCommand( () => ExecuteJogY( false), CanExecuteJogY);
            JogYOutCommand = new RelayCommand( () => ExecuteJogY( true), CanExecuteJogY);

            // gross moves.
            GripPlateCommand = new RelayCommand( ExecuteGripPlate, CanExecuteGripOrUngripPlate);
            UngripPlateCommand = new RelayCommand( ExecuteUngripPlate, CanExecuteGripOrUngripPlate);
            ZUp10RetractCommand = new RelayCommand( ExecuteZUp10Retract, CanExecuteZUp10Retract);
            GoHomeXCommand = new RelayCommand( ExecuteGoHomeX, CanExecuteGoHomeX);
            GoHomeZCommand = new RelayCommand( ExecuteGoHomeZ, CanExecuteGoHomeZ);
            GoHomeTCommand = new RelayCommand( ExecuteGoHomeT, CanExecuteGoHomeT);
            GoHomeGCommand = new RelayCommand( ExecuteGoHomeG, CanExecuteGoHomeG);
            ParkRobotCommand = new RelayCommand( ExecuteParkRobot, CanExecuteParkRobot);
            MoveToTeachpointCommand = new RelayCommand( ExecuteMoveToTeachpoint, CanExecuteMoveToTeachpoint);

            // plate-transfer moves.
            PickACommand = new RelayCommand( ExecutePickA, CanExecutePickOrPlaceA);
            PickBCommand = new RelayCommand( ExecutePickB, CanExecutePickOrPlaceB);
            PlaceACommand = new RelayCommand( ExecutePlaceA, CanExecutePickOrPlaceA);
            PlaceBCommand = new RelayCommand( ExecutePlaceB, CanExecutePickOrPlaceB);
            TransferABCommand = new RelayCommand( ExecuteTransferAB, CanExecuteTransferABorBA);
            TransferBACommand = new RelayCommand( ExecuteTransferBA, CanExecuteTransferABorBA);

            // reinventory.
            ReinventorySelectedRacksCommand = new RelayCommand( ExecuteReinventorySelectedRacks, CanExecuteReinventorySelectedRacks);
            ReinventoryAllRacksCommand = new RelayCommand( () => ReinventoryAllRacks( update_gui: true, park_robot_after: true), CanExecuteReinventoryAllRacks);                    // !!!ReinventoryAllRacks used elsewhere...
            AbortReinventoryCommand = new RelayCommand( ExecuteAbortReinventory, CanExecuteAbortReinventory);
            UpdateInventoryViewCommand = new RelayCommand( UpdateStaticInventoryView);                                                                                              // !!!UpdateStaticInventoryView used elsewhere...

            // teaching.
            UpdateApproachHeightCommand = new RelayCommand( UpdateApproachHeight, CanExecuteUpdateApproachHeight);
            ReloadTeachpointsCommand = new RelayCommand( () => ReloadAllDeviceTeachpoints(true), CanExecuteReloadTeachpoints);                                                      // !!!ReloadAllDeviceTeachpoints used elsewhere...
            UpdateTeachpointCommand = new RelayCommand( () => ExecuteUpdateTeachpoint( SelectedTeachpointName), CanExecuteUpdateTeachpoint);
            SaveTeachpointCommand = new RelayCommand( ExecuteSaveTeachpoint, CanExecuteSaveTeachpoint);
            TeachpointInterpolationCommand = new RelayCommand( ExecuteInterpolateTeachpoints, CanExecuteInterpolateTeachpoints);
            TransformTeachpointCommand = new RelayCommand( ExecuteTransformTeachpoints, CanExecuteTransformTeachpoints);
            ReteachTeachpointCommand = new RelayCommand( ExecuteReteachTeachpoint, CanExecuteReteachTeachpoint);

            // stop/reset.
            StopAllCommand = new RelayCommand( ExecuteStopAll, CanExecuteStopAll);
            ResetInterlocksCommand = new RelayCommand( ExecuteResetInterlocks);
        }
        // ----------------------------------------------------------------------
        private ILabware SelectedLabware { get { return LabwareDatabase.GetLabware( PickAndPlaceLabware); }}
        private AccessibleDeviceInterface SelectedDevice { get { return ( AccessibleDeviceView == null || AccessibleDeviceView.CurrentItem == null) ? null : AccessibleDeviceView.CurrentItem as AccessibleDeviceInterface; }}
        private AccessibleDeviceInterface SelectedDeviceA { get { return ( DeviceAView == null || DeviceAView.CurrentItem == null) ? null : DeviceAView.CurrentItem as AccessibleDeviceInterface; }}
        private AccessibleDeviceInterface SelectedDeviceB { get { return ( DeviceBView == null || DeviceBView.CurrentItem == null) ? null : DeviceBView.CurrentItem as AccessibleDeviceInterface; }}
        private string SelectedTeachpointName { get { return ( TeachpointNames == null || TeachpointNames.CurrentItem == null) ? null : TeachpointNames.CurrentItem.ToString(); }}
        private string SelectedTeachpointAName { get { return ( TeachpointANames == null || TeachpointANames.CurrentItem == null) ? null : TeachpointANames.CurrentItem.ToString(); }}
        private string SelectedTeachpointBName { get { return ( TeachpointBNames == null || TeachpointBNames.CurrentItem == null) ? null : TeachpointBNames.CurrentItem.ToString(); }}
        private HiveTeachpoint SelectedTeachpoint { get { return ( SelectedDevice.Name == null || SelectedTeachpointName == null) ? null : Hardware.GetTeachpoint( SelectedDevice.Name, SelectedTeachpointName); }}
        private HiveTeachpoint SelectedTeachpointA { get { return ( SelectedDeviceA.Name == null || SelectedTeachpointAName == null) ? null : Hardware.GetTeachpoint( SelectedDeviceA.Name, SelectedTeachpointAName); }}
        private HiveTeachpoint SelectedTeachpointB { get { return ( SelectedDeviceB.Name == null || SelectedTeachpointBName == null) ? null : Hardware.GetTeachpoint( SelectedDeviceB.Name, SelectedTeachpointBName); }}
        private bool CompatibleTeachpointOrientations{ get { return ( SelectedTeachpointA == null || SelectedTeachpointB == null) ? false : SelectedTeachpointA.Orientation == SelectedTeachpointB.Orientation; }}
        // ----------------------------------------------------------------------
        private string CheckLabwareSelected { get { return PickAndPlaceLabware != null ? null : "Please make a labware selection"; }}
        private string CheckDeviceSelected { get { return ( AccessibleDeviceView != null && AccessibleDeviceView.CurrentItem != null) ? null : "Please select a device"; }}
        private string CheckThisDeviceSelected { get { return ( AccessibleDeviceView != null && AccessibleDeviceView.CurrentItem != null && SelectedDevice == this) ? null : String.Format( "Please select device '{0}'", Name); }}
        private string CheckDeviceASelected { get { return ( DeviceAView != null && DeviceAView.CurrentItem != null) ? null : "Please make a selection for Device A"; }}
        private string CheckDeviceBSelected { get { return ( DeviceBView != null && DeviceBView.CurrentItem != null) ? null : "Please make a selection for Device B"; }}
        private string CheckTeachpointSelected { get { return ( TeachpointNames != null && TeachpointNames.CurrentItem != null) ? null : "Please select a teachpoint"; }}
        private string CheckTeachpointASelected { get { return ( TeachpointANames != null && TeachpointANames.CurrentItem != null) ? null : "Please make a selection for Teachpoint A"; }}
        private string CheckTeachpointBSelected { get { return ( TeachpointBNames != null && TeachpointBNames.CurrentItem != null) ? null : "Please make a selection for Teachpoint B"; }}
        private string CheckValidTeachpointSelected { get { return ( SelectedTeachpointName == null || SelectedTeachpoint != null) ? null : "Selected teachpoint has not been taught"; }}
        private string CheckValidTeachpointASelected { get { return ( SelectedTeachpointAName == null || SelectedTeachpointA != null) ? null : "Teachpoint A has not been taught"; }}
        private string CheckValidTeachpointBSelected { get { return ( SelectedTeachpointBName == null || SelectedTeachpointB != null) ? null : "Teachpoint B has not been taught"; }}
        private string CheckCompatibleTeachpointOrientations { get { return CompatibleTeachpointOrientations ? null : "Teachpoint A and Teachpoint B have incompatible teachpoint orientations"; }}
        private string CheckInitialized { get { return Initialized ? null : "Device not initialized"; }}
        private string CheckBarcodeReaderConnected { get { return ( SimulatingBarcodeReader || ( BarcodeReader != null && BarcodeReader.Connected)) ? null : "Barcode reader not connected"; }}
        private string CheckHomed { get { return IsHomed ? null : "Device not homed"; }}
        private string CheckHomedX { get { return AxisHomeStatus.X ? null : "X axis not homed"; }}
        private string CheckHomedZ { get { return AxisHomeStatus.Z ? null : "Z axis not homed"; }}
        private string CheckHomedT { get { return AxisHomeStatus.T ? null : "Theta axis not homed"; }}
        private string CheckHomedG { get { return AxisHomeStatus.G ? null : "Gripper axis not homed"; }}
        private string CheckThetaSafe { get { return Hardware.CurrentWorldPosition.T < ( Config.ThetaSafe + 0.5) ? null : "Theta axis is not safely tucked"; }}
        private string CheckConditionalThetaSafe { get { return OverrideThetaCheck ? null : CheckThetaSafe; }}
        private string CheckThetaSafeForLargerZMoves { get { return ( ZIncrement <= 10.0 || CheckThetaSafe == null) ? null : "Theta must be tucked to jog Z more than 10mm"; }}
        private string CheckSafeToMove { get { return DataRequestInterface.Value.SafeToMove( this) ? null : "Not safe to move -- check laser curtain or BPS140"; }}
        private string CheckDiagnosticsExecutorIdle { get { return !DiagnosticsExecutor.Busy ? null : "Diagnostics command in progress"; }}
        private string CheckProtocolExecutorNotRunning { get { return !ProtocolExecutor.Running ? null : "Protocol command in progress"; }}
        // ----------------------------------------------------------------------
        private IEnumerable< string> CannotMoveReasons
        {
            get{
                return new[]{
                    CheckInitialized,
                    CheckDiagnosticsExecutorIdle,
                    CheckProtocolExecutorNotRunning,
                };
            }
        }
        // ----------------------------------------------------------------------
        private bool CheckTopAndBottomSlotTeachpoints( IEnumerable< int> selected_racks)
        {
            if( StaticInventoryView.Count == 0)
                return false;
            // create teachpoint names for the top and bottom slots for all of the racks
            List<string> location_names = new List<string>();
            var rackviews_to_check = StaticInventoryView.Where( x => selected_racks.Contains( x.RackNumber));
            foreach( RackView rack in rackviews_to_check) {
                int slot_count = rack.SlotIndexes.Count();
                if( slot_count == 0)
                    continue;
                HivePlateLocation top_slot = new HivePlateLocation( rack.RackNumber, rack.SlotIndexes.Count());
                location_names.Add( top_slot.ToString());
                if( slot_count == 1)
                    continue;
                HivePlateLocation bottom_slot = new HivePlateLocation( rack.RackNumber, 1);
                location_names.Add( bottom_slot.ToString());
            }
            // now make sure all of those teachpoints actually exist
            IList< string> teachpoint_names = Hardware.GetTeachpointNames( Name);
            return location_names.Except( teachpoint_names).Count() == 0;
        }
        // ----------------------------------------------------------------------
        /*
        private bool CanExecuteTemplate()
        {
            IEnumerable< string> cannot_reasons = ( new[]{
                // CheckLabwareSelected,
                // CheckDeviceSelected,
                // CheckDeviceASelected,
                // CheckDeviceBSelected,
                // CheckTeachpointSelected,
                // CheckTeachpointASelected,
                // CheckTeachpointBSelected,
                // CheckValidTeachpointSelected,
                // CheckValidTeachpointASelected,
                // CheckValidTeachpointBSelected,
                // CheckCompatibleTeachpointOrientations,
                // CheckInitialized,
                // CheckHomed,
                // CheckHomedX,
                // CheckHomedZ,
                // CheckHomedT,
                // CheckHomedG,
                // CheckThetaSafe,
                // CheckConditionalThetaSafe,
                // CheckThetaSafeForLargerZMoves,
                // CheckSafeToMove,
                // CheckDiagnosticsExecutorIdle,
                // CheckProtocolExecutorNotRunning,
            }).Where( cannot_reason => cannot_reason != null);
            // tool_tip = ( cannot_reasons.Count() == 0 ? "success_tool_tip" : String.Join( "\n", cannot_reasons));
            return cannot_reasons.Count() == 0;
        }
        */
        // ----------------------------------------------------------------------
        // Connect used elsewhere.
        // ----------------------------------------------------------------------
        private void ExecuteReloadMotorSettings()
        {
            try {
                StatusCache.Stop();
                ReloadMotorSettings();
                StatusCache.Start();
            } catch( Exception ex) {
                MessageBox.Show( "Could not reload motor settings: " + ex.Message);
            }
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteConnectBcr( bool connect)
        {
            // DKM 2012-01-18 not sure if this is the right way, but for some reason the BCR comes up as disconnected after connecting to the Hive,
            //                even though I have confirmed that it did connect successfully
            BarcodeReaderConnected = BarcodeReader == null ? false : BarcodeReader.Connected;
            return true;
        }
        // ----------------------------------------------------------------------
        private void ExecuteConnectBcr( bool connect)
        {
            if( connect){
                Hardware.ConnectBcr();
            } else{
                Hardware.DisconnectBcr();
            }
            // allow the GUI to update based on connection status.
            BarcodeReaderConnected = BarcodeReader == null ? false : BarcodeReader.Connected;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteServoOnOrOff()
        {
            try
            {
                IEnumerable<string> cannot_reasons = (new[]{
                CheckInitialized,
                CheckProtocolExecutorNotRunning,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteServoAxis( ServoOnOffAxisParameter p)
        {
            try {
                if( p == null)
                    return;
                byte axis_id = byte.Parse( p.ID);
                if( p.ServoOn)
                    ServoOn( axis_id);
                else
                    ServoOff( axis_id);
            } catch( Exception ex) {
                MessageBox.Show( String.Format( "Could not servo {0} axis {1}: {2}", (p.ServoOn ? "on" : "off"), p.ID, ex.Message));
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteServoOn()
        {
            new EnableAllAxesJob( DiagnosticsExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private void ExecuteServoOff()
        {
            new DisableAllAxesJob( DiagnosticsExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteHomeX()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedT,
                CheckThetaSafe,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteHomeX()
        {
            new HomeXJob( DiagnosticsExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteHomeZ()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedT,
                CheckThetaSafe,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteHomeZ()
        {
            new HomeZJob( DiagnosticsExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteHomeT()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteHomeT()
        {
            new HomeTJob( DiagnosticsExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteHomeG()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteHomeG()
        {
            new HomeGJob( DiagnosticsExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteHomeAllAxes()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                HomeAllAxesToolTip = (cannot_reasons.Count() == 0 ? "Home all axes" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteHomeAllAxes()
        {
            new HomeAllAxesJob( DiagnosticsExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteJogX()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedX,
                CheckHomedT,
                CheckConditionalThetaSafe,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                JogXToolTip = (cannot_reasons.Count() == 0 ? "Jog X axis" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteJogX( bool positive)
        {
            new JogXJob( DiagnosticsExecutor, XIncrement, positive).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteJogZ()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedZ,
                CheckHomedT,
                CheckConditionalThetaSafe,
                CheckThetaSafeForLargerZMoves,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                JogZToolTip = (cannot_reasons.Count() == 0 ? "Jog Z axis" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteJogZ( bool up)
        {
            new JogZJob( DiagnosticsExecutor, ZIncrement, up).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteJogTheta()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedT,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                JogThetaToolTip = (cannot_reasons.Count() == 0 ? "Jog theta axis" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteJogTheta( bool towards)
        {
            new JogTJob( DiagnosticsExecutor, ThetaIncrement, towards).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteJogGripper()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedG,
            }).Where(cannot_reason => cannot_reason != null);
                JogGripperToolTip = (cannot_reasons.Count() == 0 ? "Jog gripper" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteJogGripper( bool positive)
        {
            new JogGJob( DiagnosticsExecutor, GripperIncrement, positive).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteJogY()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedZ,
                CheckHomedT,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                JogYToolTip = (cannot_reasons.Count() == 0 ? "Jog Y axis" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteJogY( bool away)
        {
            new JogYJob( DiagnosticsExecutor, YIncrement, away).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteGripOrUngripPlate()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckLabwareSelected,
                CheckHomedG,
            }).Where(cannot_reason => cannot_reason != null);
                GripPlateToolTip = (cannot_reasons.Count() == 0 ? "Moves the gripper to the selected pick and place labware's minimum gripper position" : String.Join("\n", cannot_reasons));
                UngripPlateToolTip = (cannot_reasons.Count() == 0 ? "Moves the gripper to the selected pick and place labware's maximum gripper position" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteGripPlate()
        {
            new MoveGJob( DiagnosticsExecutor, SelectedLabware[ PlateOrientation == HiveTeachpoint.TeachpointOrientation.Portrait ? LabwarePropertyNames.MinPortraitGripperPos : LabwarePropertyNames.MinLandscapeGripperPos].ToDouble()).Dispatch();
        }
        // ----------------------------------------------------------------------
        private void ExecuteUngripPlate()
        {
            new MoveGJob( DiagnosticsExecutor, SelectedLabware[ PlateOrientation == HiveTeachpoint.TeachpointOrientation.Portrait ? LabwarePropertyNames.MaxPortraitGripperPos : LabwarePropertyNames.MaxLandscapeGripperPos].ToDouble()).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteZUp10Retract()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedZ,
                CheckHomedT,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteZUp10Retract()
        {
            new JogZJob( DiagnosticsExecutor, 10, true).Dispatch();
            new TuckYJob( DiagnosticsExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteGoHomeX()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedX,
                CheckHomedT,
                CheckThetaSafe,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteGoHomeX()
        {
            new MoveXJob( DiagnosticsExecutor, 0.0).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteGoHomeZ()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedZ,
                CheckHomedT,
                CheckThetaSafe,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteGoHomeZ()
        {
            new MoveZJob( DiagnosticsExecutor, 0.0).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteGoHomeT()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedT,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteGoHomeT()
        {
            new MoveTJob( DiagnosticsExecutor, Config.ThetaSafe).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteGoHomeG()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedG,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteGoHomeG()
        {
            new MoveGJob( DiagnosticsExecutor, 0.0).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteParkRobot()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckHomedX,
                CheckHomedZ,
                CheckHomedT,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                ParkRobotToolTip = (cannot_reasons.Count() == 0 ? "Park robot" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteParkRobot()
        {
            new ParkJob( DiagnosticsExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteMoveToTeachpoint()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckLabwareSelected,
                CheckDeviceSelected,
                CheckTeachpointSelected,
                CheckValidTeachpointSelected,
                CheckHomed,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                MoveToTeachpointToolTip = (cannot_reasons.Count() == 0 ? "Moves the selected plate to the selected teachpoint" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteMoveToTeachpoint()
        {
            new PickAndOrPlaceJob( DiagnosticsExecutor, null, PickAndOrPlaceJob.PickAndOrPlaceJobOption.MoveToTeachpointWithPlate, SelectedTeachpoint, SelectedLabware).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecutePickOrPlaceA()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckLabwareSelected,
                CheckDeviceASelected,
                CheckTeachpointASelected,
                CheckValidTeachpointASelected,
                CheckHomed,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                ToolTipPickA = (cannot_reasons.Count() == 0 ? String.Format("Pick plate from {0}", SelectedTeachpointAName) : String.Join("\n", cannot_reasons));
                ToolTipPlaceA = (cannot_reasons.Count() == 0 ? String.Format("Place plate at {0}", SelectedTeachpointAName) : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private bool CanExecutePickOrPlaceB()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                    CheckLabwareSelected,
                    CheckDeviceBSelected,
                    CheckTeachpointBSelected,
                    CheckValidTeachpointBSelected,
                    CheckHomed,
                    CheckSafeToMove,
                }).Where(cannot_reason => cannot_reason != null);
                ToolTipPickB = (cannot_reasons.Count() == 0 ? String.Format("Pick plate from {0}", SelectedTeachpointBName) : String.Join("\n", cannot_reasons));
                ToolTipPlaceB = (cannot_reasons.Count() == 0 ? String.Format("Place plate at {0}", SelectedTeachpointBName) : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            { 
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteTransferABorBA()
        {
            try
            {
                IEnumerable<string> cannot_reasons = CannotMoveReasons.Union(new[]{
                CheckLabwareSelected,
                CheckDeviceASelected,
                CheckTeachpointASelected,
                CheckValidTeachpointASelected,
                CheckDeviceBSelected,
                CheckTeachpointBSelected,
                CheckValidTeachpointBSelected,
                CheckCompatibleTeachpointOrientations,
                CheckHomed,
                CheckSafeToMove,
            }).Where(cannot_reason => cannot_reason != null);
                ToolTipTransferAB = (cannot_reasons.Count() == 0 ? String.Format("Pick plate from {0} and place at {1}", SelectedTeachpointAName, SelectedTeachpointBName) : String.Join("\n", cannot_reasons));
                ToolTipTransferBA = (cannot_reasons.Count() == 0 ? String.Format("Pick plate from {0} and place at {1}", SelectedTeachpointBName, SelectedTeachpointAName) : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecutePickA()
        {
            new PickAndOrPlaceJob( DiagnosticsExecutor, null, PickAndOrPlaceJob.PickAndOrPlaceJobOption.PickOnly, SelectedDeviceA, SelectedTeachpointA, SelectedLabware, new MutableString()).Dispatch();
        }
        // ----------------------------------------------------------------------
        private void ExecutePickB()
        {
            new PickAndOrPlaceJob( DiagnosticsExecutor, null, PickAndOrPlaceJob.PickAndOrPlaceJobOption.PickOnly, SelectedDeviceB, SelectedTeachpointB, SelectedLabware, new MutableString()).Dispatch();
        }
        // ----------------------------------------------------------------------
        private void ExecutePlaceA()
        {
            new PickAndOrPlaceJob( DiagnosticsExecutor, null, PickAndOrPlaceJob.PickAndOrPlaceJobOption.PlaceOnly, SelectedTeachpointA, SelectedLabware).Dispatch();
        }
        // ----------------------------------------------------------------------
        private void ExecutePlaceB()
        {
            new PickAndOrPlaceJob( DiagnosticsExecutor, null, PickAndOrPlaceJob.PickAndOrPlaceJobOption.PlaceOnly, SelectedTeachpointB, SelectedLabware).Dispatch();
        }
        // ----------------------------------------------------------------------
        private void ExecuteTransferAB()
        {
            new PickAndOrPlaceJob( DiagnosticsExecutor, null, PickAndOrPlaceJob.PickAndOrPlaceJobOption.PickAndPlace, SelectedLabware, new MutableString(), SelectedDeviceA, SelectedTeachpointA, SelectedTeachpointB).Dispatch();
        }
        // ----------------------------------------------------------------------
        private void ExecuteTransferBA()
        {
            new PickAndOrPlaceJob( DiagnosticsExecutor, null, PickAndOrPlaceJob.PickAndOrPlaceJobOption.PickAndPlace, SelectedLabware, new MutableString(), SelectedDeviceB, SelectedTeachpointB, SelectedTeachpointA).Dispatch();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteReinventorySelectedRacks()
        {
            try {
                var selected_racks = from x in StaticInventoryView where x.IsSelected select x.RackNumber;
                IEnumerable< string> cannot_reasons = CannotMoveReasons.Union( new[]{
                    ( selected_racks.Count() != 0) ? null : "Please select racks to reinventory",
                    CheckTopAndBottomSlotTeachpoints( selected_racks) ? null : "Please make sure that top and bottom slots of selected racks have been taught",
                    CheckBarcodeReaderConnected,
                    CheckHomed,
                    CheckSafeToMove,
                }).Where( cannot_reason => cannot_reason != null);
                ReinventorySelectedToolTip = ( cannot_reasons.Count() == 0 ? "Reinventory only the selected racks" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            } catch( Exception ex) {
                _log.Warn( "CanExecuteReinventorySelectedRacks: ", ex);
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteReinventorySelectedRacks()
        {
            Debug.WriteLine( "Reinventory button clicked in thread " + Thread.CurrentThread.GetHashCode().ToString());
            // figure out which racks were selected
            var selected_racks = from x in StaticInventoryView where x.IsSelected select x.RackNumber;
            ReinventoryDelegate reinventory = new ReinventoryDelegate( _reinventory_strategy.ReinventorySelectedRacksThread);
            reinventory.BeginInvoke( selected_racks, UpdateInventoryView, true, _reinventory_strategy.ReinventoryThreadComplete, true);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteReinventoryAllRacks()
        {
            try {
                IEnumerable< string> cannot_reasons = CannotMoveReasons.Union( new[]{
                    ( _plate_location_manager != null) ? null : "No racks defined",
                    ( _plate_location_manager == null || CheckTopAndBottomSlotTeachpoints( from x in _plate_location_manager.Racks select x.RackNumber)) ? null : "Please make sure that top and bottom slots of all racks have been taught",
                    CheckBarcodeReaderConnected,
                    CheckHomed,
                    CheckSafeToMove,
                }).Where( cannot_reason => cannot_reason != null);
                ReinventorySelectedToolTip = ( cannot_reasons.Count() == 0 ? "Reinventory all racks" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            } catch( Exception ex) {
                _log.Warn( "CanExecuteReinventoryAllRacks: ", ex);
                return false;
            }
        }
        // ----------------------------------------------------------------------
        // ReinventoryAllRacks used elsewhere.
        // ----------------------------------------------------------------------
        private bool CanExecuteAbortReinventory()
        {
            return true;
        }
        // ----------------------------------------------------------------------
        private void ExecuteAbortReinventory()
        {
            AbortReinventoryEvent.Set();
        }
        // ----------------------------------------------------------------------
        // UpdateStaticInventoryView used elsewhere.
        // ----------------------------------------------------------------------
        private bool CanExecuteUpdateApproachHeight()
        {
            try
            {
                IEnumerable<string> cannot_reasons = (new[]{
                CheckDeviceSelected,
                CheckTeachpointSelected,
                CheckValidTeachpointSelected,
                CheckInitialized,
                CheckProtocolExecutorNotRunning,
            }).Where(cannot_reason => cannot_reason != null);
                UpdateApproachHeightToolTip = (cannot_reasons.Count() == 0 ? "Update approach height" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void UpdateApproachHeight()
        {
            HiveTeachpoint tp = new HiveTeachpoint( SelectedTeachpoint);
            tp.ApproachHeight = NewApproachHeight;
            Hardware.SetTeachpoint( SelectedDevice.Name, tp);
            SaveTeachpointFile( SelectedDevice);
            // is there a better way to do this?  I need the teachpoint readout to update
            TeachpointPosition.ApproachHeight = NewApproachHeight;
            TeachpointPosition.Orientation = PlateOrientation;
            OnPropertyChanged( "TeachpointPosition");
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteReloadTeachpoints()
        {
            try
            {
                IEnumerable<string> cannot_reasons = (new[]{
                CheckProtocolExecutorNotRunning,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        // ReloadAllDeviceTeachpoints used elsewhere.
        // ----------------------------------------------------------------------
        private bool CanExecuteUpdateTeachpoint()
        {
            try
            {
                IEnumerable<string> cannot_reasons = (new[]{
                CheckLabwareSelected,
                CheckDeviceSelected,
                CheckTeachpointSelected,
                //CheckValidTeachpointSelected,
                CheckInitialized,
                CheckProtocolExecutorNotRunning,
            }).Where(cannot_reason => cannot_reason != null);
                UpdateTeachpointToolTip = (cannot_reasons.Count() == 0 ? "Overwrites teachpoint with current position and approach height value" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteUpdateTeachpoint( string teachpoint_name)
        {
            if( !( LabwareDatabase.IsValidLabwareName( PickAndPlaceLabware))) {
                if( MessageBoxResult.No ==  MessageBox.Show( "Are you sure you want to teach with the labware '" + PickAndPlaceLabware + "'?", "Confirm labware", MessageBoxButton.YesNo))
                    return;
            }

            Hardware.UpdateCurrentPosition();
            // save the old teachpoint selections
            string old_teachpoint_a = SelectedTeachpointAName;
            string old_teachpoint_b = SelectedTeachpointBName;
            SaveExternalTeachpoint( SelectedTeachpointName, SelectedLabware);

            // update the teachpoint comboboxes
            ReloadDeviceTeachpoints();
            ReloadDeviceATeachpoints();
            ReloadDeviceBTeachpoints();
            
            TeachpointNames.MoveCurrentTo( teachpoint_name);
            TeachpointANames.MoveCurrentTo( old_teachpoint_a);
            TeachpointBNames.MoveCurrentTo( old_teachpoint_b);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteSaveTeachpoint()
        {
            try
            {
                IEnumerable<string> cannot_reasons = (new[]{
                CheckLabwareSelected,
                CheckDeviceSelected,
                CheckThisDeviceSelected,
                CheckInitialized,
                CheckProtocolExecutorNotRunning,
            }).Where(cannot_reason => cannot_reason != null);
                SaveTeachpointToolTip = (cannot_reasons.Count() == 0 ? "Saves a new, custom teachpoint" : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteSaveTeachpoint()
        {
            // since we didn't specify a teachpoint name, need to prompt the user for one
            TeachpointNameDialog dlg = new TeachpointNameDialog();
            dlg.ShowDialog();
            dlg.Close();
            if( dlg.TeachpointNameValid) {
                try {
                    Hardware.UpdateCurrentPosition();
                    // save the old teachpoint selections
                    string old_teachpoint_a = SelectedTeachpointAName;
                    string old_teachpoint_b = SelectedTeachpointBName;
                    SaveExternalTeachpoint( dlg.TeachpointName, SelectedLabware);

                    // update the teachpoint comboboxes
                    ReloadDeviceTeachpoints();
                    if( SelectedDevice == SelectedDeviceA)
                        ReloadDeviceATeachpoints();
                    else if( SelectedDevice == SelectedDeviceB)
                        ReloadDeviceBTeachpoints();

                    TeachpointNames.MoveCurrentTo( dlg.TeachpointName);
                    TeachpointANames.MoveCurrentTo( old_teachpoint_a);
                    TeachpointBNames.MoveCurrentTo( old_teachpoint_b);
                } catch (Exception ex) {
                    MessageBox.Show(String.Format("Error saving teachpoint '{0}': {1}", dlg.TeachpointName, ex.Message));
                }
            }
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteInterpolateTeachpoints()
        {
            try
            {
                IEnumerable<string> cannot_reasons = (new[]{
                CheckDeviceSelected,
            }).Where(cannot_reason => cannot_reason != null);
                TeachpointInterpolationToolTip = (cannot_reasons.Count() == 0 ? String.Format("Click to open teachpoint interpolation dialog for device '{0}'", SelectedDevice.Name) : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteInterpolateTeachpoints()
        {
            TeachpointInterpolationDialog dlg = new TeachpointInterpolationDialog( this, SelectedDevice);
            dlg.ShowDialog();
            dlg.Close();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteTransformTeachpoints()
        {
            try
            {
                IEnumerable<string> cannot_reasons = (new[]{
                CheckDeviceSelected,
            }).Where(cannot_reason => cannot_reason != null);
                TransformTeachpointToolTip = (cannot_reasons.Count() == 0 ? String.Format("Click to open teachpoint transformation dialog for device '{0}'", SelectedDevice.Name) : String.Join("\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteTransformTeachpoints()
        {
            var dlg = new TeachpointTransformationDialog( this, SelectedDevice.Name);
            dlg.ShowDialog();
            dlg.Close();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteReteachTeachpoint()
        {
            try
            {
                IEnumerable<string> cannot_reasons = (new[]{
                CheckInitialized,
                CheckProtocolExecutorNotRunning,
            }).Where(cannot_reason => cannot_reason != null);
                // tool_tip = ( cannot_reasons.Count() == 0 ? "success" : String.Join( "\n", cannot_reasons));
                return cannot_reasons.Count() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteReteachTeachpoint()
        {
            try {
                ReteachHere( SelectedTeachpointName);
            } catch( Exception ex) {
                MessageBox.Show( "Could not reteach: " + ex.Message);
            }
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteStopAll()
        {
            return DataRequestInterface.Value.GetIOInterfaces().Count() > 0;
        }
        // ----------------------------------------------------------------------
        private void ExecuteStopAll()
        {
            // DKM 2011-03-31 no longer use TS_Stop to do stop all axes.  Instead, we are going to
            // set output bit 3 on the IO module to trigger the laser curtain
            // ts.StopAllAxes();
            Messenger.Default.Send< SoftwareInterlockCommand>( null);
        }
        // ----------------------------------------------------------------------
        private void ExecuteResetInterlocks()
        {
            Messenger.Default.Send< ResetInterlocksMessage>( new ResetInterlocksMessage());
        }
    }
}
