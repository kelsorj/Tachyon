using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;

namespace BioNex.HivePrototypePlugin
{
    public class PickStateMachine : HiveStateMachine< PickStateMachine.State, PickStateMachine.Trigger>
    {
        public enum State
        {
            Start,
            ApproachPlate, ApproachPlateError,
            ReApproachPlate, ReApproachPlateError,
            ScanPlate, ScanPlateError, ScanTipboxError,
            AutoGenerateBarcodeAndContinue,
            TakePictureOfBarcode, TakePictureOfBarcodeError,
            PickPlate, PickPlateError,
            ApproachAndPickPlate, ApproachAndPickPlateError,
            End, Abort,
        }

        public enum Trigger
        {
            Success,
            GoToApproachAndPick,
            Failure,
            ScanTipboxFailure,
            TakePicture,
            Retry,
            Ignore,
            Abort,
        }

        // properties.
        Teachpoint _tp;
        ILabware _labware;
        protected AccessibleDeviceInterface _device;

        string _rescan_barcode_text = "Rescan barcode";
        string _autogen_barcode_text = "Autogenerate unique barcode and continue";
        string _take_picture_text = "Take picture of barcode for manual entry";
        string _retake_picture_text = "Try taking picture of barcode again";

        /// <summary>
        /// This flag means that we have a teachpoint we can't get to with PVT without doing a "drop in place" move.
        /// We need to set a flag so that subsequent states know how to behave.  For example, MoveZDownToGrip would
        /// normally get bypassed for PVT, but we only want to bypass if we're NOT doing "drop in place".
        /// </summary>
        private bool _use_kungfu_move { get; set; }
        //bool _last_pvt_erred = false;

        /// <summary>
        /// used to account for placing lids on top of plates, where we need to bias the teachpoint
        /// up by the "lid offset" amount in the labware database,.
        /// </summary>
        private double _additional_z_offset { get; set; }

        private MutableString _expected_barcode;

        private enum BlendedPickOption
        {
            BPONormal,
            BPONoRetractBeforePick,
            BPOErrorRepick,
            BPOErrorRetractOnly,
        }

        private BlendedPickOption Option { get; set; }

        public PickStateMachine( HivePlugin controller, bool called_from_diags)
            : base( controller, typeof( PickStateMachine), State.Start, State.End, State.Abort, Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, controller.ErrorInterface, called_from_diags)
        {
            InitializeStates();
            controller.HiveMessenger.Register< AbortCommand>( this, Abort);
            controller.HiveMessenger.Register< PauseCommand>( this, Pause);
            controller.HiveMessenger.Register< ResumeCommand>( this, Resume);
        }

        public void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.ApproachPlate);

            ConfigureState( State.ApproachPlate, ApproachPlate, State.ScanPlate, State.ApproachPlateError)
                .Permit( Trigger.GoToApproachAndPick, State.ApproachAndPickPlate);
            
            // DKM 2011-05-27 rather than add another ConfigureState method, and potentially confuse
            //                things, I am going to configure the ScanPlate state manually.  I made
            //                this change to allow us to have a Retry-only behavior with button
            //                text that is specific to barcode reading.
            //ConfigureState( State.ScanPlate, ScanPlate, State.PickPlate, State.ScanPlateError, true);
            SM.Configure( State.ScanPlate)
                .Permit( Trigger.Success, State.PickPlate)
                .Permit( Trigger.Failure, State.ScanPlateError)
                .Permit( Trigger.ScanTipboxFailure, State.ScanTipboxError)
                .OnEntry( ScanPlate);

            SM.Configure( State.ScanPlateError)
                .Permit( Trigger.Retry, State.ApproachPlate)
                .Permit( Trigger.Ignore, State.AutoGenerateBarcodeAndContinue)
                .Permit( Trigger.TakePicture, State.TakePictureOfBarcode)
                .OnEntry( () => ScanPlateError( _rescan_barcode_text, _autogen_barcode_text, _take_picture_text) );

            SM.Configure( State.TakePictureOfBarcode)
                .Permit( Trigger.Success, State.ApproachAndPickPlate)
                .Permit( Trigger.Failure, State.TakePictureOfBarcodeError)
                .OnEntry( TakePictureOfBarcode);

            SM.Configure( State.TakePictureOfBarcodeError)
                .Permit( Trigger.Retry, State.TakePictureOfBarcode)
                .Permit( Trigger.Ignore, State.AutoGenerateBarcodeAndContinue)
                .OnEntry( () => HandleErrorWithRetryAndIgnore( _retake_picture_text, _autogen_barcode_text));

            SM.Configure( State.ScanTipboxError)
                .Permit( Trigger.Retry, State.ApproachPlate)
                .OnEntry( () => HandleErrorWithRetryOnly( _rescan_barcode_text));

            SM.Configure( State.AutoGenerateBarcodeAndContinue)
                .Permit( Trigger.Success, State.PickPlate)
                .OnEntry( AutoGenerateBarcode);
            // DKM done

            ConfigureState( State.PickPlate, PickPlate, State.End, State.PickPlateError);

            ConfigureState( State.ApproachAndPickPlate, ApproachAndPickPlate, State.End, State.ApproachAndPickPlateError);

            // DKM 2011-06-02 new pick state machine wasn't using a Done function like the old state machine,
            //                which unregistered HiveMessenger handlers
            ConfigureState( State.End, Done);
        }

        public virtual void Pick(AccessibleDeviceInterface device, Teachpoint tp, ILabware lw, MutableString expected_barcode, double additional_z_offset = 0, bool no_retract_before_pick = false)
        {
            _tp = tp;
            _labware = lw;
            _additional_z_offset = additional_z_offset;
            _device = device;

            Option = no_retract_before_pick ? BlendedPickOption.BPONoRetractBeforePick : BlendedPickOption.BPONormal;

            _expected_barcode = expected_barcode;

            // this is support for the "kung fu move" when plates at the bottom of the hive can't actually be picked with PVT
            // because the plate will clip the cover at the bottom of the robot
            _use_kungfu_move = ( _tp["z"] <= _controller.Config.MinimumAllowableToolZHeightForPVT);

            Start();
        }

        private void Abort( AbortCommand command)
        {
            base.Abort();
        }

        private void Pause( PauseCommand command)
        {
            base.Pause();
        }

        private void Resume( ResumeCommand command)
        {
            base.Resume();
        }

        private void ApproachPlate()
        {
            // DKM 2011-10-20 the problem with the Bumblebee is that if a plate is barcoded on the landscape side, and the
            //                robot wants to pick up the plate in landscape, and if we want to use barcode confirmation,
            //                we will ALWAYS get an error because the barcode in this orientation is past the usable
            //                depth of field for the Mini-3.
            bool barcode_too_far_away_to_read = _device.ProductName == BioNexDeviceNames.Bumblebee && _tp.TeachpointItems.Where( x => x.AxisName == "orientation").First().Position == (double)(BioNex.HivePrototypePlugin.HivePlugin.PortraitOrLandscape.Landscape);

            // DKM scan barcode regardless of the expected barcode, since we want to double-check tipboxes now
            // DKM 2011-06-06 Dave 2.0 wants to be able to turn off barcode reading so check the skip parameter for now
            // DKM 2011-10-20 also skip if the barcode is too far away
            if( (_controller.SkipBarcodeConfirmation && _expected_barcode.Value != Constants.Strobe) || barcode_too_far_away_to_read){
                Fire( Trigger.GoToApproachAndPick);
                return;
            }

            try{
                Dictionary< IAxis, double> move_done_window = new Dictionary< IAxis, double>();
                move_done_window[ _x_axis] = 0.2;
                BlendedPick( approach:true, pick:false, move_done_window:move_done_window);
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }

        private void ScanPlate()
        {
            string read_barcode = "";
            // only strobe barcode if we're expecting one
            try {
                // DKM 2011-06-04 need to change the behavior -- instead of not strobing
                //                when the expected barcode is "", we want to strobe anyway
                //                and see if we get a NOREAD.  This is to get some added
                //                protection against false positives in systems where we
                //                want to scan tipboxes.
                if( _expected_barcode.Value == "") {
                    read_barcode = _controller.ReadBarcode();
                    // here, I check for EMPTY instead of !NOREAD, because I know we are expecting a plate,
                    // so we shouldn't see an EMPTY barcode.
                    if( Constants.IsEmpty( read_barcode)) {
                        LastError = String.Format( "The tipbox is missing from {0}.  Please place a tipbox at this location and click '{1}'.", _tp.Name, _rescan_barcode_text);
                        Fire( Trigger.ScanTipboxFailure);
                        return;
                    }
                    Log.Info( String.Format( "{0} read barcode '{1}'", _controller.Name, read_barcode));
                } else {
                    if (!_controller.ConfirmBarcode(_expected_barcode.Value, out read_barcode, _use_kungfu_move ? 5 : 0)) {
                        if (Constants.IsStrobe(_expected_barcode.Value) && Constants.IsNoRead(read_barcode))
                            LastError = "The barcode could not be read";
                        else
                            LastError = String.Format("The barcode read '{0}' does not match the expected barcode '{1}'", read_barcode, _expected_barcode.Value);
                        Fire(Trigger.Failure);
                        return;
                    } else {
                        // DKM 2011-10-05 for picking barcoded plates from a stacker device (where we don't know the barcode ahead of time)
                        if (Constants.IsStrobe(_expected_barcode.Value)) {
                            // here I have decided to set any downstacked plate's barcode to Constants.Strobe so we have a way
                            // to change the plate's barcode in a more-or-less generic way.
                            Log.Info("Changed the plate's barcode from " + _expected_barcode.Value + " to " + read_barcode);
                            _expected_barcode.Value = read_barcode;
                        }
                    }
                }
                _controller.LastReadBarcode = read_barcode;
                Fire( Trigger.Success);
            } catch( Exception ex) {
                string error = String.Format( "Could not confirm barcode at location '{0}': {1}", _tp.Name, ex.Message);
                LastError = error;
                Fire( Trigger.Failure);
            }
        }

        private void AutoGenerateBarcode()
        {
            _controller.LastReadBarcode = _expected_barcode.Value = System.Guid.NewGuid().ToString();
            Fire( Trigger.Success);
        }

        private void TakePictureOfBarcode()
        {
            try {
                // take picture of barcode
                _controller.MoveToDeviceLocationForBCRStrobe( _device, _tp.Name, false);
                // pop up barcode misread dialog
                BarcodeReadErrorInfo info = new BarcodeReadErrorInfo( _tp.Name, "", System.IO.Path.GetTempPath() + String.Format("PickImage{0:0000}.jpg", BioNex.Shared.Microscan.MicroscanReader.ImageCounter++));
                info.NewBarcode = _controller.SaveBarcodeImage( info.ImagePath);
                if( Constants.IsNoRead( info.NewBarcode)) {
                    _controller.HandleBarcodeMisreads( new List<BarcodeReadErrorInfo> { info }, null, null, new UpdateInventoryLocationDelegate( (location,barcode) => {
                        _controller.LastReadBarcode = _expected_barcode.Value = barcode;
                    }));
                // DKM 2011-10-31 refs #541: handle the case where a plate is downstacked backwards and there is no barcode to read.  If we are forcing
                //                           a strobe and we read the barcod correctly, in this case we can accept the barcode as the initial value
                //                           and continue to process the plate.
                } else if( _expected_barcode != Constants.Strobe && _expected_barcode != info.NewBarcode) {
                    // the idea here is that if the user neglects to replace the plate with the correct one and
                    // clicks Take Picture, we don't want to allow the system to continue running with the wrong
                    // plate.  So instead we now need to verify that the barcode read during image capture is
                    // still the expected barcode.
                    LastError = String.Format( "Successfully read barcode '{0}' during image capture, but it was not the expected barcode '{1}'", info.NewBarcode, _expected_barcode);
                    Fire( Trigger.Failure);
                    return;
                } else {
                    _controller.LastReadBarcode = _expected_barcode.Value = info.NewBarcode;
                }

                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void PickPlate()
        {
            try{
                BlendedPick( approach:false, pick:true, move_done_window:new Dictionary< IAxis, double>());
                Fire( Trigger.Success);
            } catch( Exception ex){
                Option = ( MarkerDuringWhichMoveDied <= Marker.MPick) ? BlendedPickOption.BPOErrorRepick : BlendedPickOption.BPOErrorRetractOnly;
                StandardRetry( ex);
            }
        }

        private void ApproachAndPickPlate()
        {
            try{
                BlendedPick( approach:true, pick:true, move_done_window:new Dictionary< IAxis, double>());
                Fire( Trigger.Success);
            } catch( Exception ex){
                Option = ( MarkerDuringWhichMoveDied <= Marker.MXZPosition) ? Option : ( MarkerDuringWhichMoveDied <= Marker.MPick) ? BlendedPickOption.BPOErrorRepick : BlendedPickOption.BPOErrorRetractOnly;
                StandardRetry( ex);
            }
        }

        private void Done()
        {
            _controller.HiveMessenger.Unregister<AbortCommand>( this, Abort);
            _controller.HiveMessenger.Unregister<PauseCommand>( this, Pause);
            _controller.HiveMessenger.Unregister<ResumeCommand>( this, Resume);
            EndStateFunction();
        }

        private enum Marker
        {
            MSuccess = -1,
            MUngrip = 0,
            MErrorRise,
            MRetract1,
            MXZPosition,
            MExtend,
            MDrop,
            MPick,
            MRise,
            MRetract2,
        }

        private Marker MarkerDuringWhichMoveDied { get; set; }

        private void BlendedPick( bool approach, bool pick, IDictionary< IAxis, double> move_done_window)
        {
            // DKM 2011-06-02 since PVT has increased the granularity of a move, we can't pause in the middle of motion.  This
            //                is my best attempt at pausing pick and place without allowing the robot to complete the entire
            //                move.
            WaitHandle.WaitAny( new WaitHandle[] { SMPauseEvent, _main_gui_abort_event });

            if( Option == BlendedPickOption.BPOErrorRepick){
                approach = true;
                pick = true;
            }

            if( Option == BlendedPickOption.BPOErrorRetractOnly){
                approach = false;
                pick = true;
            }

            HiveMultiAxisTrajectory hive_multi_axis_trajectory = null;

            try{
                // get configuration values.
                double arm_length = _controller.Config.ArmLength;
                double finger_offset = _controller.Config.FingerOffsetZ;
                double safe_t = _controller.Config.ThetaSafe;
                double kung_fu_t = -17.0;

                // get current positions.
                double current_x = _x_axis.GetPositionMM();
                double current_z = _z_axis.GetPositionMM();
                double current_t = _t_axis.GetPositionMM();
                double current_g = _g_axis.GetPositionMM();

                // convert current positions to tool space.
                double current_y_tool = 0;
                double current_z_tool = 0;
                Hive.ConvertTZWorldToYZTool( arm_length, finger_offset, current_t, current_z, out current_y_tool, out current_z_tool);

                // compute y destinations.
                double safe_y = Hive.GetYFromTheta( arm_length, safe_t);
                double kung_fu_y = Hive.GetYFromTheta( arm_length, kung_fu_t);

                // compute z destinations.
                double entry_z_tool = _tp[ "z"] + Math.Max( _additional_z_offset + _labware[ LabwarePropertyNames.GripperOffset].ToDouble(), _tp[ "approach_height"]);
                double pick_z_tool = _tp[ "z"] + _additional_z_offset + _labware[ LabwarePropertyNames.GripperOffset].ToDouble();
                double exit_z_tool = _tp[ "z"] + _additional_z_offset + _labware[ LabwarePropertyNames.GripperOffset].ToDouble() + _tp[ "approach_height"];

                double temp = 0.0;
                double kung_fu_z = 0.0;
                Hive.ConvertTZWorldToYZTool( arm_length, finger_offset, safe_t, Hive.ConvertZToolToWorldUsingTheta( arm_length, finger_offset, entry_z_tool, kung_fu_t), out temp, out kung_fu_z);

                // compute g destinations.
                double ungrip_width = _labware[ _tp[ "orientation"] == 1 ? LabwarePropertyNames.MaxPortraitGripperPos : LabwarePropertyNames.MaxLandscapeGripperPos].ToDouble();
                double grip_width =   _labware[ _tp[ "orientation"] == 1 ? LabwarePropertyNames.MinPortraitGripperPos : LabwarePropertyNames.MinLandscapeGripperPos].ToDouble();

                // start at current position.
                IDictionary< IAxis, double> init_pos = _controller.ts.GetAxes().Values.ToDictionary< IAxis, IAxis, double>( axis => axis, axis => axis.GetPositionMM());
                hive_multi_axis_trajectory = new HiveMultiAxisTrajectory( _controller, init_pos, move_done_window);

                Stopwatch sw1 = Stopwatch.StartNew();

                // if not retracting only, then...
                if( Option != BlendedPickOption.BPOErrorRetractOnly){
                    if( approach){
                        // ungrip.
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MUngrip, dst_g: ungrip_width, post_blend_distance: BlendingConstants.UngripBlend);
                        // if recovering from error with a repick, then move z to entry height.
                        if( Option == BlendedPickOption.BPOErrorRepick){
                            hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MErrorRise, dst_z: _use_kungfu_move ? kung_fu_z : entry_z_tool, acc_z: 500.0);
                            hive_multi_axis_trajectory.AddSeparator();
                        }
                        // if retracting before pick or not retracting before pick and x move from current to teachpoint would exceed 0.5mm, then retract to safe.
                        if( Option != BlendedPickOption.BPONoRetractBeforePick || Math.Abs( _tp[ "x"] - current_x) > 0.5){
                            if( safe_y - current_y_tool < -0.5){
                                hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRetract1, dst_y: safe_y);
                                hive_multi_axis_trajectory.AddSeparator();
                            }
                        }
                        // position x and z for entry.
                        if( _use_kungfu_move){
                            hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MXZPosition, dst_x: _tp[ "x"], dst_z: kung_fu_z, pre_blend_distance: BlendingConstants.SafeZoneCornerBlend, post_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                            hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MXZPosition, dst_t: kung_fu_t, pre_blend_distance: Math.Abs( safe_t - kung_fu_t) / 2.0);
                        } else{
                            hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MXZPosition, dst_x: _tp[ "x"], dst_z: entry_z_tool, pre_blend_distance: BlendingConstants.SafeZoneCornerBlend, post_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                        }
                    }
                    if( pick){
                        // extend y to teachpoint.
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MExtend, dst_y: _tp[ "y"], pre_blend_distance: BlendingConstants.SafeZoneCornerBlend, post_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                        // drop down to pick (if necessary).
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MDrop, dst_z: pick_z_tool, acc_z: 500.0, pre_blend_distance: BlendingConstants.GripperEmptyCornerBlend, post_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                        // pick.
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MPick, dst_g: grip_width, pre_blend_distance: BlendingConstants.GripperEmptyCornerBlend, post_blend_distance: BlendingConstants.GripBlend);
                    }
                }
                if( pick){
                    // rise up to exit.
                    hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRise, dst_z: exit_z_tool, acc_z: 1500.0, pre_blend_distance: BlendingConstants.GripBlend, post_blend_distance: BlendingConstants.GripperFullCornerBlend);
                    // retract to safe.
                    if( _use_kungfu_move){
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRetract2, dst_y: kung_fu_y, pre_blend_distance: BlendingConstants.GripperFullCornerBlend, post_blend_distance: 5.0);
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRetract2, dst_t: safe_t, pre_blend_distance: 1.0);
                    } else{
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRetract2, dst_y: safe_y, pre_blend_distance: BlendingConstants.GripperFullCornerBlend);
                    }
                }

                long elapsed = sw1.ElapsedMilliseconds;
                Log.DebugFormat( "Pick waveform generation took {0}ms", elapsed);

                hive_multi_axis_trajectory.GeneratePVTPoints();

                elapsed = sw1.ElapsedMilliseconds - elapsed;
                Log.DebugFormat( "Pick point generation took {0}ms", elapsed);

                IAxis.ExecuteCoordinatedPVTTrajectory( ( byte)6, hive_multi_axis_trajectory);

                MarkerDuringWhichMoveDied = Marker.MSuccess;
            } catch( Exception ex){
                MarkerDuringWhichMoveDied = ( Marker)( hive_multi_axis_trajectory.MarkerDuringWhichMoveDied());
                throw ex;
            }
        }

        /// <summary>
        /// This allows the user to rescan, autogen barcode, or take picture for manual entry
        /// </summary>
        private void ScanPlateError( string retry_label, string ignore_label, string picture_label)
        {
            try{
                List< string> error_strings = new List< string>{ retry_label, ignore_label, picture_label};
                if( _show_abort_label){
                    error_strings.Add( ABORT_LABEL);
                }
                ErrorData error_data = new ErrorData( LastError, error_strings);
                SMStopwatch.Stop();
                ErrorInterface.AddError( error_data);
                List< ManualResetEvent> events = new List< ManualResetEvent>{ _main_gui_abort_event};
                events.AddRange( error_data.EventArray);
                int event_index = WaitHandle.WaitAny( events.ToArray());
                SMStopwatch.Start();
                if( error_data.TriggeredEvent == retry_label){
                    Fire( RetryTrigger);
                } else if( error_data.TriggeredEvent == ignore_label){
                    Fire( IgnoreTrigger);
                } else if( error_data.TriggeredEvent == picture_label) {
                    Fire( Trigger.TakePicture);
                } else if(( error_data.TriggeredEvent == ABORT_LABEL) || ( event_index == 0)){
                    Fire( AbortTrigger);
                } else{
                    Debug.Assert( false, UNEXPECTED_EVENT_STRING);
                    Fire( AbortTrigger);
                }
            } catch( Exception ex){
                // unexpected exception.
                LastError = ex.Message;
                Debug.Assert( false, ex.Message);
            }
        }
    }
}
