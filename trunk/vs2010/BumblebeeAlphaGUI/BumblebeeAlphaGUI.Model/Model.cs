using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.BumblebeeAlphaGUI.SchedulerInterface;
using System.Diagnostics;
using System.Collections.ObjectModel;
using BioNex.Shared.Utils;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.HitpickXMLReader;
using System.Windows;
using System.Windows.Controls;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.ApplicationPreferences;
using BioNex.Shared.ErrorHandling;
using BioNex.Shared.LabwareDatabase;
using System.ComponentModel.Composition;
using BioNex.Shared.LibraryInterfaces;
using log4net;
using BioNex.Shared.SimpleInventory;
using GalaSoft.MvvmLight.Messaging;
using BioNex.BumblebeeAlphaGUI.TipOperations;

namespace BioNex.BumblebeeAlphaGUI.Model
{
    [Export("Model")]
    public class Model
    {
        private readonly string BumblebeePreferences = "BumblebeePreferences";
        private TechnosoftConnection _ts = null;
        private AlphaHardware _hw = null;
        private IScheduler _scheduler = null;
        private Teachpoints _teachpoints = new Teachpoints();
        private DateTime _start;
        private RobotStorageInterface _robot_handler;
        private string _tip_handling_method;

        // used to keep track of how a plugin name mapped to the instantiated plugin object
        private Dictionary<string,IScheduler> _scheduler_plugin_descriptions = new Dictionary<string,IScheduler>();
        private Dictionary<string,RobotStorageInterface> _robot_descriptions = new Dictionary<string,RobotStorageInterface>();
        private Dictionary<string,object> _device_descriptions = new Dictionary<string,object>();
        public ObservableCollection<string> DevicePluginNames { get; set; }

        public enum ProtocolState { Idle, Running, Paused };
        public ProtocolState ProtocolExecutionState { get; set; }

        [Import]
        public ILabwareDatabase LabwareDatabase { get; set; }
        [Import]
        public IPreferences Preferences { get; set; }
        [Import]
        public IError ErrorInterface { get; set; }
        [ImportMany(typeof(IScheduler))]
        public IEnumerable<IScheduler> SchedulerPlugins { get; set; }
        [ImportMany(typeof(RobotStorageInterface))]
        public IEnumerable<RobotStorageInterface> RobotStoragePlugins { get; set; }
        [ImportMany(typeof(DeviceInterface))]
        public IEnumerable<DeviceInterface> DevicePlugins { get; set; }

        //[ImportingConstructor]
        public Model()// ILabwareDatabase labware_database, IError error_interface)
        {
            //LabwareDatabase = labware_database;
            //ErrorInterface = error_interface;
            // need to load preferences first, so that we know where all of the critical files are
            DevicePluginNames = new ObservableCollection<string>();
            ProtocolExecutionState = ProtocolState.Idle;
        }

        public ObservableCollection<string> GetSchedulerPluginNames()
        {
            ObservableCollection<string> names = new ObservableCollection<string>();
            foreach( IScheduler i in SchedulerPlugins) {
                names.Add( i.GetSchedulerName());
                _scheduler_plugin_descriptions.Add( i.GetSchedulerName(), i);
            }
            return names;
        }

        public ObservableCollection<string> GetRobotStoragePluginNames()
        {
            ObservableCollection<string> names = new ObservableCollection<string>();
            foreach( RobotStorageInterface i in RobotStoragePlugins) {
                names.Add( i.Name);
                _robot_descriptions.Add( i.Name, i);
                _device_descriptions.Add( i.Name, i);
                DevicePluginNames.Add( i.Name);
            }
            return names;
        }

        public ObservableCollection<string> GetAllDevicePluginNames()
        {
            return DevicePluginNames;
        }

        public ILabware GetLabware( string labware_name)
        {
            return LabwareDatabase.GetLabware( labware_name);
        }

        public Positions GetLRTeachpoint( byte channel_id, byte stage_id)
        {
            Teachpoint tp = _teachpoints.GetStageTeachpoint( channel_id, stage_id).LowerRight;
            Positions p = new Positions();
            p.X = tp["x"];
            p.Y = tp["y"];
            p.Z = tp["z"];
            p.R = tp["r"];
            return p;
        }

        public Positions GetULTeachpoint( byte channel_id, byte stage_id)
        {
            Teachpoint tp = _teachpoints.GetStageTeachpoint( channel_id, stage_id).UpperLeft;
            Positions p = new Positions();
            p.X = tp["x"];
            p.Y = tp["y"];
            p.Z = tp["z"];
            p.R = tp["r"];
            return p;
        }

        public Positions GetWashTeachpoint( byte channel_id)
        {
            Teachpoint tp = _teachpoints.GetWashTeachpoint( channel_id);
            Positions p = new Positions();
            p.X = tp["x"];
            p.Z = tp["z"];
            return p;
        }

        public Positions GetRobotTeachpoint( byte stage_id)
        {
            Teachpoint tp = _teachpoints.GetRobotTeachpoint( stage_id);
            Positions p = new Positions();
            p.Y = tp["y"];
            p.R = tp["r"];
            return p;
        }

        public void LoadTeachpointFile()
        {
            _teachpoints.LoadTeachpointFile( Preferences.GetPreferenceValue( BumblebeePreferences, "teachpoint file"), null);
        }

        public void Initialize()
        {
            string app_path = BioNex.Shared.Utils.FileSystem.GetAppPath();

            try {
                LoadTeachpointFile();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }

            string motor_settings_path;
            string hardware_configuration_path;
            string tsm_setup_folder;
            // initialize the motion control hardware
            //! \todo put connection type into motor settings XML
            //_ts = new TechnosoftConnection( "1", TML.TMLLib.CHANNEL_SYS_TEC_USBCAN, 500000); // notice this is Systec Device #1
            _ts = new TechnosoftConnection();
            try {
                motor_settings_path = Preferences.GetPreferenceValue( BumblebeePreferences, "motor settings file");
                tsm_setup_folder = Preferences.GetPreferenceValue( BumblebeePreferences, "TSM setup folder");
                _ts.LoadConfiguration( motor_settings_path, tsm_setup_folder);
            } catch( KeyNotFoundException ex) {
                MessageBox.Show( "Motor settings file and/or TSM setup folder has not been specified.  Please make selections and then restart the application.");
                throw ex;
            } catch( InvalidMotorSettingsException ex) {
                string error = String.Format( "Error parsing motor settings for axis {0}.  The current value for the setting named {1} is either missing, or invalid.  The valid range is between {2} and {3}.", ex.AxisID, ex.SettingName, ex.MinValue, ex.MaxValue);
                MessageBox.Show( error + "\n\n(this messagebox will eventually be replaced by an entry in the log)");
            }

            // after loading the axes, read the hardware configuration file
            // to map axes to channels and stages
            _hw = new AlphaHardware( ErrorInterface);
            try {
                hardware_configuration_path = Preferences.GetPreferenceValue( BumblebeePreferences, "hardware configuration file");
                _hw.LoadConfiguration( hardware_configuration_path, _ts);
            } catch( KeyNotFoundException) {
                MessageBox.Show( "Hardware configuration file has not been specified.  Please select one and then restart the application.");
            }
            for( byte i=1; i<=_hw.GetNumberOfStages(); i++)
                _hw.GetStage(i).SetSystemTeachpoints( _teachpoints);
            // turn off the pump
            _hw.GetChannel( 1).GetX().SetOutput( 13, true);
            _hw.GetChannel( 1).GetX().SetOutput( 25, true);
            _ts.SetBroadcastMasterAxisID(11); // Is this a bad assumption, assuming axis 11 will always be here? Probably not horrible, but it could be better.
        }

        public void Close()
        {
            if( _ts != null)
                _ts.Close();
        }

        /*
        private void LoadReportingPlugins()
        {
            string exe_path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string path = exe_path.Substring( 0, exe_path.LastIndexOf( '\\'));
            _reporter.LoadReportingPlugins( path);
        }
        */

        /// <summary>
        /// Loads all plugins that implement IScheduler and adds their names
        /// to the combobox in the main GUI
        /// </summary>
        public ObservableCollection<string> LoadSchedulerPlugins()
        {
            _scheduler_plugin_descriptions.Clear();
            ObservableCollection<string> device_plugin_names = new ObservableCollection<string>();
            // get a list of all DLLs in the output folder
            string exe_path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string path = exe_path.Substring( 0, exe_path.LastIndexOf( '\\'));
            List<IScheduler> scheduler_plugins = new List<IScheduler>();
            BioNex.Shared.Utils.Plugins.LoadPlugins<IScheduler>( path, scheduler_plugins);
            // now populate the combobox
            foreach( IScheduler si in scheduler_plugins) {
                _scheduler_plugin_descriptions.Add( si.GetSchedulerName(), si);
                device_plugin_names.Add( si.GetSchedulerName());
            }
            return device_plugin_names;
        }

        public IEnumerable<string> GetTipBoxNames()
        {
            return LabwareDatabase.GetTipBoxNames();
        }

        public void TeachTipPosition( byte channel_id, byte stage_id, string tipbox_name)
        {
            // get the labware info
            TipBox tipbox = LabwareDatabase.GetLabware( tipbox_name) as TipBox;
            if( tipbox == null)
                return; //! \todo throw an exception?

            Channel c = _hw.GetChannel( channel_id);
            Stage s = _hw.GetStage( stage_id);
            // move channel and stage to tipname
            Teachpoint tp = s.GetWellPosition( channel_id, "A1", tipbox.NumberOfTips);
            c.GetX().MoveAbsolute( tp["x"], false);
            s.MoveAbsolute( tp["y"], tp["r"]);
            c.GetX().MoveAbsolute( tp["x"]);
            // take the parameters from the labware database for the selected labware
            try {
                double z_contact = c.TipOnHere( tipbox, true);
                // store this nominal Z position in the database so we know where the tip
                // was pressed on before teaching the UL and LR stage points
                tipbox.UpdateTipPressParameters( 0, 0, z_contact);
            } catch( NoTipPresentException ex) {
                MessageBox.Show( ex.Message);
            } catch( MissedTipException ex) {
                MessageBox.Show( ex.Message);
            }
        }

        /// <summary>
        /// This executes when the scheduler is done processing the hitpick list
        /// </summary>
        /// <param name="iar"></param>
        private void ProcessComplete( IAsyncResult iar)
        {
            // DKM 021010 don't need this anymore, now that we have an actual robot picking plates
            /*
            // return all stages to their robot teachpoints
            byte num_stages = GetNumberOfStages();
            for( byte i=1; i<=num_stages; i++) {
                Stage s = _hw.GetStage( i);
                s.MoveToRobotTeachpoint();
            }
            */
            string done_message = String.Format( "DONE -- total time: {0}", DateTime.Now - _start);
            LogManager.GetLogger(typeof(Model)).Info( done_message);
            MessageBox.Show( done_message, "Protocol complete!");
            ProtocolExecutionState = ProtocolState.Idle;
        }

        public byte GetNumberOfStages()
        {
            return _hw.GetNumberOfStages();
        }

        public byte GetNumberOfChannels()
        {
            return _hw.GetNumberOfChannels();
        }

        public bool IsStageHomed( byte stage_id, List<IAxis> axes_not_homed)
        {
            Stage stage = _hw.GetStage( stage_id);
            return stage.IsHomed( axes_not_homed);
        }

        public bool IsChannelHomed( byte channel_id, List<IAxis> axes_not_homed)
        {
            Channel channel = _hw.GetChannel( channel_id);
            return channel.IsHomed( axes_not_homed);
        }

        public void UpdatePositions( byte channel_id, byte stage_id, out double x, out double y, out double z, out double w, out double r)
        {
            x = _hw.GetChannel( channel_id).GetX().GetPositionMM();
            z = _hw.GetChannel( channel_id).GetZ().GetPositionMM();
            w = _hw.GetChannel( channel_id).GetW().GetPositionMM();
            y = _hw.GetStage( stage_id).GetY().GetPositionMM();
            r = _hw.GetStage( stage_id).GetR().GetPositionMM();
        }

        public void JogX( byte channel_id, double amount_mm)
        {
            try {
                _hw.GetChannel(channel_id).GetX().MoveRelative(amount_mm);
            } catch( AxisException ex) {
                MessageBox.Show(String.Format("Could not jog X{0} axis by {1}mm: {2}", channel_id, amount_mm, ex.Message));
            }
        }
        public void JogY( byte stage_id, double amount_mm)
        {
            try {
                _hw.GetStage( stage_id).GetY().MoveRelative( amount_mm);
            }
            catch (AxisException ex) {
                MessageBox.Show(String.Format("Could not jog Y{0} axis by {1}mm: {2}", stage_id, amount_mm, ex.Message));
            }
        }
        public void JogZ( byte channel_id, double amount_mm)
        {
            try {
                _hw.GetChannel( channel_id).GetZ().MoveRelative( amount_mm);
            }
            catch (AxisException ex) {
                MessageBox.Show(String.Format("Could not jog Z{0} axis by {1}mm: {2}", channel_id, amount_mm, ex.Message));
            }
        }
        public void JogW( byte channel_id, double amount_mm)
        {
            try {
                _hw.GetChannel( channel_id).GetW().MoveRelative( amount_mm);
            }
            catch (AxisException ex) {
                MessageBox.Show(String.Format("Could not jog W{0} axis by {1}mm: {2}", channel_id, amount_mm, ex.Message));
            }
        }
        public void JogR( byte stage_id, double amount_mm)
        {
            try {
                _hw.GetStage( stage_id).GetR().MoveRelative( amount_mm);
            }
            catch (AxisException ex) {
                MessageBox.Show(String.Format("Could not jog R{0} axis by {1}mm: {2}", stage_id, amount_mm, ex.Message));
            }
        }
        public void MoveAboveUL(byte channel_id, byte stage_id, double distance_above_mm)
        {
            try {
                // first, need to move all of the channels up so we don't crash other tips
                // that may have been deployed!
                for (byte c = 1; c <= GetNumberOfChannels(); c++)
                    _hw.GetChannel(c).GetZ().MoveAbsolute(0);
                Channel channel = _hw.GetChannel(channel_id);
                Stage stage = _hw.GetStage(stage_id);
                StageTeachpoint stp = _teachpoints.GetStageTeachpoint(channel_id, stage_id);
                Teachpoint tp = stp.UpperLeft;
                // now move all other axes
                channel.GetX().MoveAbsolute(tp["x"]);
                stage.GetY().MoveAbsolute(tp["y"]);
                stage.GetR().MoveAbsolute(tp["r"]);
                channel.GetZ().MoveAbsolute(tp["z"] - distance_above_mm);
            } catch( AxisException ex) {
                MessageBox.Show(String.Format("Could not move {0}mm above upper left teachpoint for stage {1}: {2}", distance_above_mm, stage_id, ex.Message));
            }
        }
        public void MoveToUL( byte channel_id, byte stage_id)
        {
            try {
                // first, need to move all of the channels up so we don't crash other tips
                // that may have been deployed!
                for( byte c=1; c<=GetNumberOfChannels(); c++)
                    _hw.GetChannel( c).GetZ().MoveAbsolute( 0);
                Channel channel = _hw.GetChannel( channel_id);
                Stage stage = _hw.GetStage( stage_id);
                StageTeachpoint stp = _teachpoints.GetStageTeachpoint( channel_id, stage_id);
                Teachpoint tp = stp.UpperLeft;
                // now move all other axes
                channel.GetX().MoveAbsolute(tp["x"]);
                stage.GetY().MoveAbsolute(tp["y"]);
                stage.GetR().MoveAbsolute(tp["r"]);
                // now move z down if we want to move to the teachpoint, instead of just moving above it
                channel.GetZ().MoveAbsolute(tp["z"]);
            }
            catch (AxisException ex) {
                MessageBox.Show(String.Format("Could not move to upper left teachpoint for stage {0}: {1}", stage_id, ex.Message));
            }
        }
        public void MoveAboveLR( byte channel_id, byte stage_id, double distance_above_mm)
        {
            try {
                // first, need to move all of the channels up so we don't crash other tips
                // that may have been deployed!
                for( byte c=1; c<=GetNumberOfChannels(); c++)
                    _hw.GetChannel( c).GetZ().MoveAbsolute( 0);
                Channel channel = _hw.GetChannel( channel_id);
                Stage stage = _hw.GetStage( stage_id);
                StageTeachpoint stp = _teachpoints.GetStageTeachpoint( channel_id, stage_id);
                Teachpoint tp = stp.LowerRight;
                // now move all other axes
                channel.GetX().MoveAbsolute(tp["x"]);
                stage.GetY().MoveAbsolute(tp["y"]);
                stage.GetR().MoveAbsolute(tp["r"]);
                channel.GetZ().MoveAbsolute(tp["z"] - distance_above_mm);
            }
            catch (AxisException ex) {
                MessageBox.Show(String.Format("Could not move {0}mm above lower right teachpoint for stage {1}: {2}", distance_above_mm, stage_id, ex.Message));
            }        
        }
        public void MoveToLR( byte channel_id, byte stage_id)
        {
            try {
                // first, need to move all of the channels up so we don't crash other tips
                // that may have been deployed!
                for( byte c=1; c<=GetNumberOfChannels(); c++)
                    _hw.GetChannel( c).GetZ().MoveAbsolute( 0);
                Channel channel = _hw.GetChannel( channel_id);
                Stage stage = _hw.GetStage( stage_id);
                StageTeachpoint stp = _teachpoints.GetStageTeachpoint( channel_id, stage_id);
                Teachpoint tp = stp.LowerRight;
                // now move all other axes
                channel.GetX().MoveAbsolute(tp["x"]);
                stage.GetY().MoveAbsolute(tp["y"]);
                stage.GetR().MoveAbsolute(tp["r"]);
                // now move z down if we want to move to the teachpoint, instead of just moving above it
                channel.GetZ().MoveAbsolute(tp["z"]);
            }   
            catch (AxisException ex) {
                MessageBox.Show(String.Format("Could not move to lower right teachpoint for stage {0}: {1}", stage_id, ex.Message));
            }
        }
        public void MoveAboveWash( byte channel_id)
        {
            Channel channel = _hw.GetChannel( channel_id);
            Teachpoint wtp = _teachpoints.GetWashTeachpoint( channel_id);
            // move to z = 0;
            channel.GetZ().MoveAbsolute( 0);
            // now move all other axes
            channel.GetX().MoveAbsolute( wtp["x"]);
        }
        public void MoveToWash( byte channel_id)
        {
            Channel channel = _hw.GetChannel( channel_id);
            Teachpoint wtp = _teachpoints.GetWashTeachpoint( channel_id);
            // move to z = 0;
            channel.GetZ().MoveAbsolute( 0);
            // now move all other axes
            channel.GetX().MoveAbsolute( wtp["x"]);
            channel.GetZ().MoveAbsolute( wtp["z"]);
        }

        public void Teach(bool teach_ul, byte channel_id, byte stage_id)
        {
            try {
                Channel channel = _hw.GetChannel(channel_id);
                Stage stage = _hw.GetStage(stage_id);
                double x = channel.GetX().GetPositionMM();
                double z = channel.GetZ().GetPositionMM();
                double y = stage.GetY().GetPositionMM();
                double r = stage.GetR().GetPositionMM();

                if (teach_ul)
                    _teachpoints.AddUpperLeftStageTeachpoint(channel_id, stage_id, x, z, y, r);
                else
                    _teachpoints.AddLowerRightStageTeachpoint(channel_id, stage_id, x, z, y, r);

                _teachpoints.SaveTeachpointFile();
            } catch( Exception ex) {
                MessageBox.Show(String.Format("Could not save teachpoint file: {0}", ex.Message));
            }
        }

        private void AutoTeach( bool auto_teach_ul, byte channel_id, byte stage_id)
        {
            // get current position information
            Channel channel = _hw.GetChannel(channel_id);
            Stage stage = _hw.GetStage(stage_id);
            double x = channel.GetX().GetPositionMM();
            double z = channel.GetZ().GetPositionMM();
            double y = stage.GetY().GetPositionMM();
            double r = stage.GetR().GetPositionMM();

            // apply rotation to figure out where the opposite corner is
            // we assume a 96 well plate
            double a1x, a1y;
            Wells.GetA1DistanceFromCenterOfPlate( 96, out a1x, out a1y);
            // rotate these positions by r
            double a1x_rotated, a1y_rotated;
            Wells.GetXYAfterRotation( a1x, a1y, Math.Abs(r), r < 0, out a1x_rotated, out a1y_rotated);
            // figure out where the opposite well location is
            // for a 96 well plate, the x position is a1x + 11 * 9, and
            // y position is a1y - 7 * 9
            double lrx = a1x + (11 * 9);
            double lry = a1y - (7 * 9);
            // rotate the LR point by r degrees
            double lrx_rotated, lry_rotated;
            Wells.GetXYAfterRotation( lrx, lry, Math.Abs(r), r < 0, out lrx_rotated, out lry_rotated);
            // now that we have the rotated UL and LR points, we know the new X and Y offsets
            // and can apply them to one teachpoint to get the other
            double delta_x = lrx_rotated - a1x_rotated;
            double delta_y = lry_rotated - a1y_rotated;
            double new_x, new_y;
            if( auto_teach_ul) {
                // we want to teach the UL teachpoint based on the LR teachpoint
                new_x = x - delta_x;
                new_y = y + delta_y;
            } else {
                // we want to teach the LR teachpoint based on the UL teachpoint
                new_x = x + delta_x;
                new_y = y - delta_y;
            }
            // modify the teachpoints object -- it will get saved from the Teach()
            // method once this method returns

            if( auto_teach_ul)
                _teachpoints.AddUpperLeftStageTeachpoint(channel_id, stage_id, new_x, z, new_y, r);
            else
                _teachpoints.AddLowerRightStageTeachpoint(channel_id, stage_id, new_x, z, new_y, r);

            _teachpoints.SaveTeachpointFile();        
        }

        public void TeachWash( byte channel_id)
        {
            /*
            if( MessageBox.Show( String.Format( "Are you sure you want to teach the wash station teachpoint for Channel #{0}?", channel_id), "Confirm Teachpoint", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            */
            Channel channel = _hw.GetChannel( channel_id);
            double x = channel.GetX().GetPositionMM();
            double z = channel.GetZ().GetPositionMM();
            _teachpoints.AddWashTeachpoint( channel_id, x, z);
            _teachpoints.SaveTeachpointFile();
        }

        public void TeachRobotPosition( byte stage_id)
        {
            Stage s = _hw.GetStage( stage_id);
            double y = s.GetY().GetPositionMM();
            double r = s.GetR().GetPositionMM();
            _teachpoints.AddRobotTeachpoint( stage_id, y, r);
            _teachpoints.SaveTeachpointFile();
        }

        public bool IsHomed( List<IAxis> axes_not_homed)
        {
            if( _hw == null)
                return false;
            return _hw.IsHomed( axes_not_homed);
        }

        public bool IsOn()
        {
            return _hw.IsOn();
        }

        public bool IsChannelXEnabled( byte channel_id)
        {
            return _hw.GetChannel( channel_id).GetX().IsOn();
        }

        public bool IsStageYEnabled( byte stage_id)
        {
            return _hw.GetStage( stage_id).GetY().IsOn();
        }

        public void DisableXYAxes( byte channel_id, byte stage_id, bool disable)
        {
            _hw.GetChannel( channel_id).GetX().Enable( !disable);
            _hw.GetStage( stage_id).GetY().Enable( !disable);
        }

        public void TestTipOn( string tipbox_name, byte channel, byte stage, string tip_name)
        {
            Channel c = _hw.GetChannel( channel);
            Stage s = _hw.GetStage( stage);
            // move channel and stage to tipname
            Teachpoint tp = s.GetWellPosition( channel, tip_name, 96);
            c.GetX().MoveAbsolute( tp["x"], false);
            s.MoveAbsolute( tp["y"], tp["r"]);
            c.GetX().MoveAbsolute( tp["x"]);
            // take the parameters from the labware database for the selected labware
            ILabware tipbox = LabwareDatabase.GetLabware( tipbox_name);
            try {
                c.TipOnHere( tipbox, true);
            } catch( NoTipPresentException ex) {
                MessageBox.Show( ex.Message);
            } catch( MissedTipException ex) {
                MessageBox.Show( ex.Message);
            }
        }

        public void On()
        {
            _hw.Enable( true);
        }

        public void ResetServos()
        {
            _hw.GetChannel( 1).GetX().ResetFaultsOnAllAxes();
        }

        public void ServoOn()
        {
            _hw.Enable( true);
        }

        public void StopAllMotors()
        {
            _hw.StopAllMotors();
        }

        public void SetSystemSpeed( int percent)
        {
            _hw.SetSystemSpeed( percent);
        }

        public void ReturnChannelsHome()
        {
            // I got rid of this implementation because it doesn't return all of the channels home at the same time
            /*
            foreach( Channel c in _hw)
                c.ReturnHome();
             */
            // move Zs up together and wait for all to complete
            foreach( Channel c in _hw)
                c.GetZ().MoveAbsolute( 0, false);
            foreach( Channel c in _hw)
                c.GetZ().MoveAbsolute( 0);
            // move Xs home together, don't wait for completion
            foreach( Channel c in _hw)
                c.GetX().MoveAbsolute( 0, false);
        }

        public void ReturnStagesHome()
        {
            foreach( Stage s in _hw.GetStages())
                s.ReturnHome();
        }

        public void Home()
        {
            _hw.Home( ErrorInterface);
        }

        public void HomeX( byte channel_id)
        {
            _hw.GetChannel( channel_id).GetX().Home( false);
        }

        public void HomeY( byte stage_id)
        {
            _hw.GetStage( stage_id).GetY().Home( false);
        }

        public void HomeZ( byte channel_id)
        {
            _hw.GetChannel( channel_id).GetZ().Home( false);
        }

        public void HomeW( byte channel_id)
        {
            _hw.GetChannel( channel_id).GetW().Home( false);
        }

        public void HomeR( byte stage_id)
        {
            _hw.GetStage( stage_id).GetR().Home( false);
        }

        public void ShowDiagnostics( string plugin_name)
        {
            if( !_device_descriptions.ContainsKey( plugin_name))
                return;
            BioNex.Shared.DeviceInterfaces.DeviceInterface di = _device_descriptions[plugin_name] as BioNex.Shared.DeviceInterfaces.DeviceInterface;
            if( di == null)
                return;
            di.ShowDiagnostics();
        }

        public UserControl GetDeviceDiagnosticsPanel( string plugin_name)
        {
            if( !_device_descriptions.ContainsKey( plugin_name) && !_robot_descriptions.ContainsKey( plugin_name))
                return null;
            BioNex.Shared.DeviceInterfaces.DeviceInterface di;
            if( _device_descriptions.ContainsKey( plugin_name))
                di = _device_descriptions[plugin_name] as BioNex.Shared.DeviceInterfaces.DeviceInterface;
            else
                di = _robot_descriptions[plugin_name] as BioNex.Shared.DeviceInterfaces.DeviceInterface;
            if( di == null)
                return null;
            return di.GetDiagnosticsPanel();
        }

        public void Execute( string scheduler_plugin, string robot_plugin, string hitpick_filepath, string tip_handling_method)
        {
            _tip_handling_method = tip_handling_method; // cache this so we can do validation on the tips needed
            // make sure the Z axes return home first
            foreach( Channel c in _hw)
                c.GetZ().MoveAbsolute( 0, false);
            foreach( Channel c in _hw)
                c.GetZ().MoveAbsolute( 0, true);

            // set up the selected scheduler
            _scheduler = _scheduler_plugin_descriptions[scheduler_plugin];
            _scheduler.SetHardware( _hw);
            _scheduler.SetTeachpoints( _teachpoints);
            _scheduler.SetTipHandlingMethod( tip_handling_method);
            _robot_handler = _robot_descriptions[robot_plugin];
            try
            {
                _robot_handler.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show( String.Format("Could not initialize robot '{0}': {1}", _robot_handler.Name, ex.Message));
                return;
            }
            _scheduler.SetPlateHandler( _robot_handler);
            _scheduler.Reset();
            // load the file into the hitpick reader
            try {
                Reader reader = new Reader( LabwareDatabase);
                TransferOverview to = reader.Read( hitpick_filepath, null);
                _hw.RevertStageModes();
                // validate transfers against inventory
                ValidateInventory( to);
                // temporary hack: the problem at this point is that we have a tip operation specified in the GUI,
                // i.e. wash, change, none, and a tips_only attribute in the hardware configuration XML file.  We
                // need to decide the best way to make these two features converge so that validation of tips can
                // be handled more cleanly.  For now, we need to look at the tip operation and the stage mode, and
                // if the tip operation isn't "change tip" then we have to change any stage that has mode == Tips
                // to be mode == undefined
                if( tip_handling_method != TipHandlingStrings.ChangeTip) {
                    List<Stage> stages = _hw.GetStages();
                    foreach( Stage s in stages){
                        if( s.Mode == Stage.ModeType.Tips)
                            s.ResetMode();
                    }
                }
                Messenger.Default.Send<ProgressMax>( new ProgressMax( to.Transfers.Count()));
                TransferProcess process = new TransferProcess( _scheduler.StartProcess);
                Debug.WriteLine( "started at {0}", DateTime.Now.ToString());
                _start = DateTime.Now;
                process.BeginInvoke( to, new AsyncCallback(ProcessComplete), null);
                ProtocolExecutionState = ProtocolState.Running;
                LogManager.GetLogger(typeof(Model)).Info( String.Format( "Starting protocol '{0}'", hitpick_filepath));
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ValidateInventory( TransferOverview to)
        {
            List<string> barcodes_not_found = new List<string>();
            // loop over all of the labware needed by the hitpick file, and make sure all
            // of the labware barcodes exist in inventory
            foreach( KeyValuePair<string,Plate> kvp in to.SourcePlates) {
                Plate plate = kvp.Value;
                /*
                // is this way too inefficient?  we are querying every robot storage device for each barcode.
                // perhaps on the next iteration, I should query by passing all of the barcodes.  Also this
                // is an opportunity to multithread the operations.
                bool found_barcode = false;
                foreach( RobotStorageInterface rsi in RobotStoragePlugins) {
                    if( rsi.HasPlateWithBarcode( plate.Barcode)) {
                        found_barcode = true;
                        break;
                    }
                }
                if( !found_barcode)
                    throw new InventoryBarcodeNotFoundException( plate.Barcode);
                 */

                // I can't loop over the items right now because the speedy robot always reports that it has a barcode!
                if( !_robot_handler.HasPlateWithBarcode( plate.Barcode))
                    barcodes_not_found.Add( plate.Barcode);
            }
            // now do the same for dest plates.  I might want to refactor this later.
            foreach( KeyValuePair<string,Plate> kvp in to.DestinationPlates) {
                Plate plate = kvp.Value;
                if( !_robot_handler.HasPlateWithBarcode( plate.Barcode))
                    barcodes_not_found.Add( plate.Barcode);
            }
            // finally, need to evaluate tips (if we're doing Change Tips) to see if we'll
            // have enough tip boxes
            if( _tip_handling_method == TipHandlingStrings.ChangeTip) {
                int num_tips_needed = to.Transfers.Count;
                // for now, assume 96 well tip boxes
                int num_tipboxes_needed = (num_tips_needed / 96) + (num_tips_needed % 96 > 0 ? 1 : 0);
                for( int i=1; i<=num_tipboxes_needed; i++) {
                    string tipbox_barcode = "tipbox" + i.ToString();
                    if( !_robot_handler.HasPlateWithBarcode( tipbox_barcode))
                        barcodes_not_found.Add( tipbox_barcode);
                }
            }
            if( barcodes_not_found.Count != 0)
                throw new InventoryBarcodeNotFoundException( String.Join( ", ", barcodes_not_found.ToArray()));
        }

        /// <summary>
        /// Pause delegates all pause behavior to the Bumblebee scheduler.  If it's
        /// WF-based, then we need to use WF functions to support it.  The Hive will
        /// not get any specific pause behavior built into it.  It's up to the
        /// BB to not allow the Hive to move.
        /// </summary>
        public void Pause()
        {
            _scheduler.Pause();
            ProtocolExecutionState = ProtocolState.Paused;
        }

        /// <summary>
        /// Resume delegates all resume behavior to the Bumblebee scheduler
        /// </summary>
        public void Resume()
        {
            _scheduler.Resume();
            ProtocolExecutionState = ProtocolState.Running;
        }

        /// <summary>
        /// Abort delegates all abort behavior to the Bumblebee scheduler
        /// </summary>
        public void Abort()
        {
            _scheduler.Abort();
            ProtocolExecutionState = ProtocolState.Idle;
        }

        public void SavePreferences()
        {
            Preferences.Save( BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\preferences.xml");
        }

        public string GetPreference( string key)
        {
            return Preferences.GetPreferenceValue( BumblebeePreferences, key);
        }

        public void SetPreference( string key, string value)
        {
            Preferences.SetPreferenceValue( BumblebeePreferences, key, value);
        }

        public void HomeAllDevices()
        {
            foreach( DeviceInterface di in DevicePlugins) {
                di.Initialize();
            }
        }
    }
}
