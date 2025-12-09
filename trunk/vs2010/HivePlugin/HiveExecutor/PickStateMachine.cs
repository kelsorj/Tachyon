using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BioNex.Hive.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;

namespace BioNex.Hive.Executor
{
    internal class PickStateMachine : HiveStateMachine< PickStateMachine.State, PickStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        internal enum State
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
        // ----------------------------------------------------------------------
        internal enum Trigger
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
        // ----------------------------------------------------------------------
        internal enum BlendedPickOption
        {
            BPONormal,
            BPONoRetractBeforePick,
            BPOTerminateAtTeachpoint,
        }
        // ----------------------------------------------------------------------
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

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private readonly AccessibleDeviceInterface Device;
        private readonly HiveTeachpoint Teachpoint;
        private readonly ILabware Labware;
        private readonly MutableString ExpectedBarcode;
        /// <summary>
        /// used to account for placing lids on top of plates, where we need to bias the teachpoint
        /// up by the "lid offset" amount in the labware database,.
        /// </summary>
        private double AdditionalZOffset { get; set; }
        private BlendedPickOption Option { get; set; }
        private PlaceStateMachine AssociatedPlace { get; set; }

        /// <summary>
        /// This flag means that we have a teachpoint we can't get to with PVT without doing a "drop in place" move.
        /// We need to set a flag so that subsequent states know how to behave.  For example, MoveZDownToGrip would
        /// normally get bypassed for PVT, but we only want to bypass if we're NOT doing "drop in place".
        /// </summary>
        private bool UseKungFuMove { get; set; }

        private bool DoPreMoveZPositioning { get; set; }
        private bool DoRetractOnly { get; set; }
        private bool DoReapproach { get; set; }

        private Marker MarkerDuringWhichMoveDied { get; set; }

        private const string RESCAN_BARCODE_TEXT = "Rescan barcode";
        private const string AUTOGEN_BARCODE_TEXT = "Autogenerate unique barcode and continue";
        private const string TAKE_PICTURE_TEXT = "Take picture of barcode for manual entry";
        private const string RETAKE_PICTURE_TEXT = "Try taking picture of barcode again";

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        internal PickStateMachine( HiveExecutor executor, ManualResetEvent ended_aborted_event, AccessibleDeviceInterface device, HiveTeachpoint tp, ILabware lw, MutableString expected_barcode, double additional_z_offset = 0, BlendedPickOption option = BlendedPickOption.BPONormal, PlaceStateMachine associated_place = null)
            : base( executor, ended_aborted_event, typeof( PickStateMachine), State.Start, State.End, State.Abort, Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, executor.HandleError)
        {
            InitializeStates();

            executor.Hardware.Messenger.Register< AbortCommand>( this, Abort);
            executor.Hardware.Messenger.Register< PauseCommand>( this, Pause);
            executor.Hardware.Messenger.Register< ResumeCommand>( this, Resume);

            Device = device;
            Teachpoint = tp;
            Labware = lw;
            ExpectedBarcode = expected_barcode;
            AdditionalZOffset = additional_z_offset;
            Option = option;
            AssociatedPlace = associated_place;

            // this is support for the "kung fu move" when plates at the bottom of the hive can't actually be picked with PVT
            // because the plate will clip the cover at the bottom of the robot
            UseKungFuMove = ( Teachpoint.Z <= executor.Hardware.Config.MinimumAllowableToolZHeightForPVT);

            DoPreMoveZPositioning = false;
            DoRetractOnly = false;
            DoReapproach = false;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
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
                .OnEntry( () => ScanPlateError( RESCAN_BARCODE_TEXT, AUTOGEN_BARCODE_TEXT, TAKE_PICTURE_TEXT) );

            SM.Configure( State.TakePictureOfBarcode)
                .Permit( Trigger.Success, State.ApproachAndPickPlate)
                .Permit( Trigger.Failure, State.TakePictureOfBarcodeError)
                .OnEntry( TakePictureOfBarcode);

            SM.Configure( State.TakePictureOfBarcodeError)
                .Permit( Trigger.Retry, State.TakePictureOfBarcode)
                .Permit( Trigger.Ignore, State.AutoGenerateBarcodeAndContinue)
                .OnEntry( () => HandleErrorWithRetryAndIgnore( RETAKE_PICTURE_TEXT, AUTOGEN_BARCODE_TEXT));

            SM.Configure( State.ScanTipboxError)
                .Permit( Trigger.Retry, State.ApproachPlate)
                .Permit( Trigger.Abort, State.Abort) // DKM 2012-01-19 if you pick and place from diags, expected barcode is "", which makes us end up in ScanTipboxError.  Need to allow Abort in diags.
                .OnEntry( () => HandleErrorWithRetryOnly( RESCAN_BARCODE_TEXT));

            SM.Configure( State.AutoGenerateBarcodeAndContinue)
                .Permit( Trigger.Success, State.PickPlate)
                .OnEntry( AutoGenerateBarcode);
            // DKM done

            ConfigureState( State.PickPlate, PickPlate, State.End, State.PickPlateError);

            ConfigureState( State.ApproachAndPickPlate, ApproachAndPickPlate, State.End, State.ApproachAndPickPlateError);

            // DKM 2011-06-02 new pick state machine wasn't using a End function like the old state machine,
            //                which unregistered HiveMessenger handlers
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.Abort, AbortedStateFunction);
        }
        // ----------------------------------------------------------------------
        protected override void EndStateFunction()
        {
            Executor.Hardware.Messenger.Unregister< AbortCommand>( this, Abort);
            Executor.Hardware.Messenger.Unregister< PauseCommand>( this, Pause);
            Executor.Hardware.Messenger.Unregister< ResumeCommand>( this, Resume);
            base.EndStateFunction();
        }
        // ----------------------------------------------------------------------
        protected override void AbortedStateFunction()
        {
            if( AssociatedPlace != null){
                AssociatedPlace.PickFlush();
            }
            Executor.Hardware.Messenger.Unregister< AbortCommand>( this, Abort);
            Executor.Hardware.Messenger.Unregister< PauseCommand>( this, Pause);
            Executor.Hardware.Messenger.Unregister< ResumeCommand>( this, Resume);
            base.AbortedStateFunction();
        }
        // ----------------------------------------------------------------------
        private void Abort( AbortCommand command)
        {
            base.Abort();
        }
        // ----------------------------------------------------------------------
        private void Pause( PauseCommand command)
        {
            base.Pause();
        }
        // ----------------------------------------------------------------------
        private void Resume( ResumeCommand command)
        {
            base.Resume();
        }
        // ----------------------------------------------------------------------
        private void ApproachPlate()
        {
            // DKM 2011-10-20 the problem with the Bumblebee is that if a plate is barcoded on the landscape side, and the
            //                robot wants to pick up the plate in landscape, and if we want to use barcode confirmation,
            //                we will ALWAYS get an error because the barcode in this orientation is past the usable
            //                depth of field for the Mini-3.
            bool barcode_too_far_away_to_read = Device.ProductName == BioNexDeviceNames.Bumblebee && Teachpoint.Orientation == HiveTeachpoint.TeachpointOrientation.Landscape;

            // DKM scan barcode regardless of the expected barcode, since we want to double-check tipboxes now
            // DKM 2011-06-06 Dave 2.0 wants to be able to turn off barcode reading so check the skip parameter for now
            if( Executor.Hardware.SkipBarcodeConfirmation && ExpectedBarcode.Value != Constants.Strobe || barcode_too_far_away_to_read){
                Fire( Trigger.GoToApproachAndPick);
                return;
            }

            try{
                Dictionary< IAxis, double> move_done_window = new Dictionary< IAxis, double>();
                move_done_window[ XAxis] = 0.2;
                BlendedPick( approach: true, pick: false, move_done_window: move_done_window);
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void PickPlate()
        {
            try{
                BlendedPick( approach: DoReapproach, pick: true, move_done_window: new Dictionary< IAxis, double>());
                Fire( Trigger.Success);
            } catch( Exception ex){
                if( MarkerDuringWhichMoveDied > Marker.MXZPosition){
                    DoPreMoveZPositioning = true;
                }
                if( MarkerDuringWhichMoveDied > Marker.MPick){
                    DoRetractOnly = true;
                } else{
                    DoReapproach = true;
                }
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void ApproachAndPickPlate()
        {
            try{
                BlendedPick( approach: true, pick: true, move_done_window: new Dictionary< IAxis, double>());
                Fire( Trigger.Success);
            } catch( Exception ex){
                if( MarkerDuringWhichMoveDied > Marker.MXZPosition){
                    DoPreMoveZPositioning = true;
                }
                if( MarkerDuringWhichMoveDied > Marker.MPick){
                    DoRetractOnly = true;
                }
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void ScanPlate()
        {
            // only strobe barcode if we're expecting one
            try {
                // DKM 2011-06-04 need to change the behavior -- instead of not strobing
                //                when the expected barcode is "", we want to strobe anyway
                //                and see if we get a NOREAD.  This is to get some added
                //                protection against false positives in systems where we
                //                want to scan tipboxes.
                string read_barcode = "";
                if( ExpectedBarcode.Value == "") {
                    read_barcode = Executor.Hardware.ReadBarcode();
                    // here, I check for EMPTY instead of !NOREAD, because I know we are expecting a plate,
                    // so we shouldn't see an EMPTY barcode.
                    if( Constants.IsEmpty( read_barcode)) {
                        LastError = String.Format( "The tipbox is missing from {0}.  Please place a tipbox at this location and click '{1}'.", Teachpoint.Name, RESCAN_BARCODE_TEXT);
                        Fire( Trigger.ScanTipboxFailure);
                        return;
                    }
                    Log.InfoFormat( "{0} read barcode '{1}'", Executor.Hardware.Name, read_barcode);
                } else {
                    if (!Executor.Hardware.ConfirmBarcode(ExpectedBarcode.Value, out read_barcode, UseKungFuMove ? 5 : 0)) {
                        if (Constants.IsStrobe(ExpectedBarcode.Value) && Constants.IsNoRead(read_barcode))
                            LastError = "The barcode could not be read";
                        else
                            LastError = String.Format("The barcode read '{0}' does not match the expected barcode '{1}'", read_barcode, ExpectedBarcode.Value);
                        Fire(Trigger.Failure);
                        return;
                    } else {
                        // DKM 2011-10-05 for picking barcoded plates from a stacker device (where we don't know the barcode ahead of time)
                        if (Constants.IsStrobe(ExpectedBarcode.Value)) {
                            // here I have decided to set any downstacked plate's barcode to Constants.Strobe so we have a way
                            // to change the plate's barcode in a more-or-less generic way.
                            Log.Info("Changed the plate's barcode from " + ExpectedBarcode.Value + " to " + read_barcode);
                            ExpectedBarcode.Value = read_barcode;
                        }
                    }
                }
                Executor.Hardware.LastReadBarcode = read_barcode;
                Fire( Trigger.Success);
            } catch( Exception ex) {
                string error = String.Format( "Could not confirm barcode at location '{0}': {1}", Teachpoint.Name, ex.Message);
                LastError = error;
                Fire( Trigger.Failure);
            }
        }
        // ----------------------------------------------------------------------
        private void AutoGenerateBarcode()
        {
            Executor.Hardware.LastReadBarcode = ExpectedBarcode.Value = System.Guid.NewGuid().ToString();
            Fire( Trigger.Success);
        }
        // ----------------------------------------------------------------------
        private void TakePictureOfBarcode()
        {
            try {
                // take picture of barcode
                Executor.Hardware.MoveToDeviceLocationForBCRStrobe( Device, Teachpoint.Name, false);
                // pop up barcode misread dialog
                BarcodeReadErrorInfo info = new BarcodeReadErrorInfo( Teachpoint.Name, "", System.IO.Path.GetTempPath() + "PickImage.jpg");
                info.NewBarcode = Executor.Hardware.SaveBarcodeImage( info.ImagePath);
                if( Constants.IsNoRead( info.NewBarcode)) {
                    Executor.Hardware.HandleBarcodeMisreads( new List<BarcodeReadErrorInfo> { info }, null, null, new UpdateInventoryLocationDelegate( (location,barcode) => {
                        Executor.Hardware.LastReadBarcode = ExpectedBarcode.Value = barcode;
                    }));
                } else if( ExpectedBarcode != info.NewBarcode) {
                    // the idea here is that if the user neglects to replace the plate with the correct one and
                    // clicks Take Picture, we don't want to allow the system to continue running with the wrong
                    // plate.  So instead we now need to verify that the barcode read during image capture is
                    // still the expected barcode.
                    LastError = String.Format( "Successfully read barcode '{0}' during image capture, but it was not the expected barcode '{1}'", info.NewBarcode, ExpectedBarcode.Value);
                    Fire( Trigger.Failure);
                    return;
                } else {
                    Executor.Hardware.LastReadBarcode = ExpectedBarcode.Value = info.NewBarcode;
                }

                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// This allows the user to rescan, autogen barcode, or take picture for manual entry
        /// </summary>
        private void ScanPlateError( string retry_label, string ignore_label, string picture_label)
        {
            IDictionary< string, Trigger> label_to_trigger = new Dictionary< string, Trigger>();
            label_to_trigger[ retry_label] = RetryTrigger;
            label_to_trigger[ ignore_label] = IgnoreTrigger;
            label_to_trigger[ picture_label] = Trigger.TakePicture;
            HandleLabels( label_to_trigger);
        }
        // ----------------------------------------------------------------------
        private void BlendedPick( bool approach, bool pick, IDictionary< IAxis, double> move_done_window)
        {
            // DKM 2011-06-02 since PVT has increased the granularity of a move, we can't pause in the middle of motion.  This
            //                is my best attempt at pausing pick and place without allowing the robot to complete the entire
            //                move.
            WaitHandle.WaitAny( new WaitHandle[] { SMPauseEvent, _main_gui_abort_event });

            HiveMultiAxisTrajectory hive_multi_axis_trajectory = null;

            try{
                // get configuration values.
                double arm_length = Executor.Hardware.Config.ArmLength;
                double finger_offset = Executor.Hardware.Config.FingerOffsetZ;
                double safe_t = Executor.Hardware.Config.ThetaSafe;
                double kung_fu_t = Executor.Hardware.Config.ThetaKungFu;

                // get current positions.
                double current_x = XAxis.GetPositionMM();
                double current_z = ZAxis.GetPositionMM();
                double current_t = TAxis.GetPositionMM();
                double current_g = GAxis.GetPositionMM();

                // convert current positions to tool space.
                Tuple< double, double> current_yz_tool = HiveMath.ConvertTZWorldToYZTool( arm_length, finger_offset, current_t, current_z);
                double current_y_tool = current_yz_tool.Item1;
                double current_z_tool = current_yz_tool.Item2;

                // compute y destinations.
                double safe_y = HiveMath.GetYFromTheta( arm_length, safe_t);
                double kung_fu_y = HiveMath.GetYFromTheta( arm_length, kung_fu_t);

                // compute z destinations.
                double entry_z_tool = Teachpoint.Z + Math.Max( AdditionalZOffset + Labware[ LabwarePropertyNames.GripperOffset].ToDouble(), Teachpoint.ApproachHeight);
                double pick_z_tool = Teachpoint.Z + AdditionalZOffset + Labware[ LabwarePropertyNames.GripperOffset].ToDouble();
                double exit_z_tool = Teachpoint.Z + AdditionalZOffset + Labware[ LabwarePropertyNames.GripperOffset].ToDouble() + Teachpoint.ApproachHeight;

                double kung_fu_z = HiveMath.ConvertTZWorldToYZTool( arm_length, finger_offset, safe_t, HiveMath.ConvertZToolToWorldUsingTheta( arm_length, finger_offset, entry_z_tool, kung_fu_t)).Item2;

                // compute g destinations.
                double ungrip_width = Labware[ Teachpoint.Orientation == HiveTeachpoint.TeachpointOrientation.Portrait ? LabwarePropertyNames.MaxPortraitGripperPos : LabwarePropertyNames.MaxLandscapeGripperPos].ToDouble();
                double grip_width =   Labware[ Teachpoint.Orientation == HiveTeachpoint.TeachpointOrientation.Portrait ? LabwarePropertyNames.MinPortraitGripperPos : LabwarePropertyNames.MinLandscapeGripperPos].ToDouble();

                // start at current position.
                IDictionary< IAxis, double> init_pos = Executor.Hardware.TechnosoftConnection.GetAxes().Values.ToDictionary< IAxis, IAxis, double>( axis => axis, axis => axis.GetPositionMM());
                hive_multi_axis_trajectory = new HiveMultiAxisTrajectory( Executor.Hardware, init_pos, move_done_window);

                Stopwatch sw1 = Stopwatch.StartNew();

                // if not retracting only, then...
                if( !DoRetractOnly){
                    if( approach){
                        // ungrip.
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MUngrip, dst_g: ungrip_width, post_blend_distance: BlendingConstants.UngripBlend);
                        // if recovering from error with a repick, then move z to entry height.
                        if( DoPreMoveZPositioning){
                            hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MErrorRise, dst_z: UseKungFuMove ? kung_fu_z : entry_z_tool, acc_z: 500.0);
                            hive_multi_axis_trajectory.AddSeparator();
                        }
                        // if retracting before pick or not retracting before pick and x move from current to teachpoint would exceed 0.5mm, then retract to safe.
                        if( Option != BlendedPickOption.BPONoRetractBeforePick || Math.Abs( Teachpoint.X - current_x) > 0.5){
                            if( safe_y - current_y_tool < -0.5){
                                hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRetract1, dst_y: safe_y);
                                hive_multi_axis_trajectory.AddSeparator();
                            }
                        }
                        // position x and z for entry.
                        if( UseKungFuMove){
                            hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MXZPosition, dst_x: Teachpoint.X, dst_z: kung_fu_z, pre_blend_distance: BlendingConstants.SafeZoneCornerBlend, post_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                            hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MXZPosition, dst_t: kung_fu_t, pre_blend_distance: Math.Abs( safe_t - kung_fu_t) / 2.0);
                        } else{
                            hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MXZPosition, dst_x: Teachpoint.X, dst_z: entry_z_tool, pre_blend_distance: BlendingConstants.SafeZoneCornerBlend, post_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                        }
                    }
                    if( pick){
                        // extend y to teachpoint.
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MExtend, dst_y: Teachpoint.Y, pre_blend_distance: BlendingConstants.SafeZoneCornerBlend, post_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                        // drop down to pick (if necessary).
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MDrop, dst_z: pick_z_tool, acc_z: 500.0, pre_blend_distance: BlendingConstants.GripperEmptyCornerBlend, post_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                        // pick (unless we're terminating at the teachpoint).
                        if( Option != BlendedPickOption.BPOTerminateAtTeachpoint){
                            hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MPick, dst_g: grip_width, pre_blend_distance: BlendingConstants.GripperEmptyCornerBlend, post_blend_distance: BlendingConstants.GripBlend);
                        }
                    }
                }
                if(( pick) && ( Option != BlendedPickOption.BPOTerminateAtTeachpoint)){
                    // rise up to exit.
                    hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRise, dst_z: exit_z_tool, acc_z: 1500.0, pre_blend_distance: BlendingConstants.GripBlend, post_blend_distance: BlendingConstants.GripperFullCornerBlend);
                    // retract to safe.
                    if( UseKungFuMove){
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
            } catch( PVTSetupRejectedException psre){
                MarkerDuringWhichMoveDied = Marker.MUngrip;
                throw psre;
            } catch( Exception ex){
                MarkerDuringWhichMoveDied = ( Marker)( hive_multi_axis_trajectory.MarkerDuringWhichMoveDied());
                throw ex;
            }
        }
    }
}
