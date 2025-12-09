using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using BioNex.BumblebeeAlphaGUI.TipOperations;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;
using log4net;
using Stateless;
using BioNex.BumblebeeAlphaGUI.DualDisposableTipSplitScheduler;
using GalaSoft.MvvmLight.Messaging;
using BioNex.Shared.DeviceInterfaces;

namespace BioNex.BumblebeeAlphaGUI.DualTipSplitScheduler
{
    public class DualTipTransferStateMachine
    {
        // this stuff passed in as workflow parameters
        private AlphaHardware Hardware { get; set; }
        private Stage SourceStage { get; set; }
        private Stage DestinationStage { get; set; }
        private List<KeyValuePair<Channel, Transfer>> TipAssignments { get; set; }
        private Teachpoints Teachpoints { get; set; }
        private double SourceAngle { get; set; }
        private double DestAngle { get; set; }
        private string TipHandlingMethod { get; set; }
        private ILabwareDatabase LabwareDatabase { get; set; }
        private ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        private TipTracker TipTracker { get; set; }
        private Stage TipBoxStage { get; set; }

        private IError ErrorInterface { get; set; }
        private static readonly ILog _log = LogManager.GetLogger(typeof(DualTipTransferStateMachine));
        private static readonly ILog _pipette_log = LogManager.GetLogger("PipetteLogger");

        // local objects
        private List<Channel> tips_ = new List<Channel>();
        private GroupedChannels grouped_channels_ { get; set; }
        private List<Transfer> transfers_ = new List<Transfer>();
        private List< ILiquidProfile> liquid_profiles_ = new List< ILiquidProfile>();
        private ILabware _source_labware;
        private ILabware _dest_labware;
        private List<double> x_positions_; // for sharing x positions between states
        private List<double> y_positions_; // for sharing y positions between states
        private string _last_error;
        private double z_safe = 0;

        private DateTime _start { get; set; }
        private string _channel_ids { get; set; }

        private StateMachine<State,Trigger> SM { get; set; }
        private bool Aborting { get; set; }
        private Trigger LastTrigger { get; set; }

        public enum State
        {
            Idle,
            Initialize,
            TipsOn,
            PreMoveXToSource,
            PreMoveXToSourceError,
            MoveToSourceYR,
            MoveToSourceYRError,
            MoveToSource,
            MoveToSourceError,
            MoveIntoSource,
            MoveIntoSourceError,
            Aspirate,
            AspirateError,
            MoveOutOfSource,
            MoveOutOfSourceError,
            MoveFromSource,
            MoveFromSourceError,
            LockDestPlate,
            PreMoveXToDest,
            PreMoveXToDestError,
            MoveToDestYR,
            MoveToDestYRError,
            MoveToDest,
            MoveToDestError,
            MoveIntoDest,
            MoveIntoDestError,
            Dispense,
            DispenseError,
            MoveOutOfDest,
            MoveOutOfDestError,
            MoveFromDest,
            MoveFromDestError,
            UnlockDestPlate,
            RunTipHandlingStateMachine,
            Done
        }

        public enum Trigger
        {
            NoTrigger,
            ReceivedTransferInfo,
            SourceStageInUse,
            DestStageInUse,
            Done,
            MoveError,
            Retry,
            MoveComplete,
            Exit
        }

        public DualTipTransferStateMachine( IError error_interface)
        {
            SM = new StateMachine<State,Trigger>( State.Idle);
            Messenger.Default.Register<AbortCommand>( this, Abort);

            ErrorInterface = error_interface;
            InitializeStates();
        }

        private void Abort( AbortCommand command)
        {
            Aborting = true;
        }

        public void Execute( AlphaHardware hardware, Stage source_stage, Stage dest_stage,
                             List<KeyValuePair<Channel,Transfer>> tip_assignments,
                             Teachpoints teachpoints, double source_angle,
                             double dest_angle, string tip_handling_method,
                             ILabwareDatabase labware_database,
                             ILiquidProfileLibrary liquid_profile_library,
                             TipTracker tip_tracker, Stage tipbox_stage)
        {
            Hardware = hardware;
            SourceStage = source_stage;
            DestinationStage = dest_stage;
            TipAssignments = tip_assignments;
            Teachpoints = teachpoints;
            SourceAngle = source_angle;
            DestAngle = dest_angle;
            TipHandlingMethod = tip_handling_method;
            LabwareDatabase = labware_database;
            LiquidProfileLibrary = liquid_profile_library;
            TipTracker = tip_tracker;
            TipBoxStage = tipbox_stage;

            StringBuilder sb = new StringBuilder();
            foreach( KeyValuePair<Channel,Transfer> kvp in tip_assignments) {
                Channel c = kvp.Key;
                sb.Append( c.GetID());
                sb.Append( " ");
            }
            _channel_ids = sb.ToString();

            LastTrigger = Trigger.ReceivedTransferInfo;
            while( !Aborting) {
                if( LastTrigger == Trigger.Exit)
                    break;
                if( LastTrigger != Trigger.NoTrigger) {
                    Fire( SM, LastTrigger);
                }
                Thread.Sleep( 10);
            }
        }

        private void Fire( Stateless.StateMachine<State,Trigger> state_machine, Trigger trigger)
        {
            state_machine.Fire( trigger);
        }

        private void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.ReceivedTransferInfo, State.Initialize);

            SM.Configure(State.Initialize)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Done, State.TipsOn)
                .OnEntry( Initialize);

            SM.Configure(State.TipsOn)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Done, State.MoveToSourceYR)
                .Permit(Trigger.SourceStageInUse, State.PreMoveXToSource)
                .OnEntry( TipsOn);

            SM.Configure(State.PreMoveXToSource)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Done, State.MoveToSourceYR)
                .Permit(Trigger.MoveError, State.PreMoveXToSourceError)
                .OnEntry( PreMoveXToSource);

            SM.Configure(State.PreMoveXToSourceError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.PreMoveXToSource)
                .OnEntry( PreMoveXToSourceError);

            SM.Configure(State.MoveToSourceYR)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.MoveToSource)
                .Permit(Trigger.MoveError, State.MoveToSourceYRError)
                .OnEntry( MoveToSourceYR);
            SM.Configure(State.MoveToSourceYRError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveToSourceYR)
                .OnEntry( MoveToSourceYRError);

            SM.Configure(State.MoveToSource)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.MoveIntoSource)
                .Permit(Trigger.MoveError, State.MoveToSourceError)
                .OnEntry( MoveToSource);
            SM.Configure(State.MoveToSourceError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveToSource)
                .OnEntry( MoveToSourceError);

            SM.Configure(State.MoveIntoSource)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.Aspirate)
                .Permit(Trigger.MoveError, State.MoveIntoSourceError)
                .OnEntry( MoveIntoSource);
            SM.Configure(State.MoveIntoSourceError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveIntoSource)
                .OnEntry( MoveIntoSourceError);

            SM.Configure(State.Aspirate)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.MoveOutOfSource)
                .Permit(Trigger.MoveError, State.AspirateError)
                .OnEntry( Aspirate);
            SM.Configure(State.AspirateError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.Aspirate)
                .OnEntry( AspirateError);

            SM.Configure(State.MoveOutOfSource)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.MoveFromSource)
                .Permit(Trigger.MoveError, State.MoveOutOfSourceError)
                .OnEntry( MoveOutOfSource);
            SM.Configure(State.MoveOutOfSourceError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveOutOfSource)
                .OnEntry( MoveOutOfSourceError);

            SM.Configure(State.MoveFromSource)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.LockDestPlate)
                .Permit(Trigger.MoveError, State.MoveFromSourceError)
                .OnEntry( MoveFromSource);
            SM.Configure(State.MoveFromSourceError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveFromSource)
                .OnEntry( MoveFromSourceError);

            SM.Configure(State.LockDestPlate)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Done, State.MoveToDestYR)
                .Permit(Trigger.DestStageInUse, State.PreMoveXToDest)
                .OnEntry( LockDestPlate);

            SM.Configure(State.PreMoveXToDest)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Done, State.MoveToDestYR)
                .Permit(Trigger.MoveError, State.PreMoveXToDestError)
                .OnEntry( PreMoveXToDest);
            SM.Configure(State.PreMoveXToDestError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.PreMoveXToDest)
                .OnEntry( PreMoveXToDestError);

            SM.Configure(State.MoveToDestYR)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.MoveToDest)
                .Permit(Trigger.MoveError, State.MoveToDestYRError)
                .OnEntry( MoveToDestYR);
            SM.Configure(State.MoveToDestYRError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveToDestYR)
                .OnEntry( MoveToDestYRError);

            SM.Configure(State.MoveToDest)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.MoveIntoDest)
                .Permit(Trigger.MoveError, State.MoveToDestError)
                .OnEntry( MoveToDest);
            SM.Configure(State.MoveToDestError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveToDest)
                .OnEntry( MoveToDestError);

            SM.Configure(State.MoveIntoDest)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.Dispense)
                .Permit(Trigger.MoveError, State.MoveIntoDestError)
                .OnEntry( MoveIntoDest);
            SM.Configure(State.MoveIntoDestError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveIntoDest)
                .OnEntry( MoveIntoDestError);

            SM.Configure(State.Dispense)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.MoveOutOfDest)
                .Permit(Trigger.MoveError, State.DispenseError)
                .OnEntry( Dispense);
            SM.Configure(State.DispenseError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.Dispense)
                .OnEntry( DispenseError);

            SM.Configure(State.MoveOutOfDest)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.MoveFromDest)
                .Permit(Trigger.MoveError, State.MoveOutOfDestError)
                .OnEntry( MoveOutOfDest);
            SM.Configure(State.MoveOutOfDestError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveOutOfDest)
                .OnEntry( MoveOutOfDestError);

            SM.Configure(State.MoveFromDest)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.MoveComplete, State.UnlockDestPlate)
                .Permit(Trigger.MoveError, State.MoveFromDestError)
                .OnEntry( MoveFromDest);
            SM.Configure(State.MoveFromDestError)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Retry, State.MoveFromDest)
                .OnEntry( MoveFromDestError);

            SM.Configure(State.UnlockDestPlate)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Done, State.RunTipHandlingStateMachine)
                .OnEntry( UnlockDestPlate);

            SM.Configure(State.RunTipHandlingStateMachine)
                .Ignore(Trigger.NoTrigger)
                .Permit(Trigger.Done, State.Done)
                .OnEntry( RunTipHandlingStateMachine);

            SM.Configure(State.Done)
                .OnEntry( Done);
        }

        private enum PreCalculatedValues
        {
            ZSrcStageTeachpoint = 0,
            ZDstStageTeachpoint,
            ZPosSrcLabwareClearanceOffset,
            // ZPosSrcTopOfLabwareOffset,
            ZPosSrcAspirateBeginOffset,
            ZPosSrcAspirateEndOffset,
            ZPosClearBetweenSrcAndDst,
            ZPosDstLabwareClearanceOffset,
            // ZPosDstTopOfLabwareOffset,
            ZPosDstDispenseBeginOffset,
            ZPosDstDispenseEndOffset,
            WPosBegin,
            WPosAspirateBegin,
            WPosAspirateEnd,
            WPosDispenseEnd,
            WPosEnd,
        }

        Dictionary< PreCalculatedValues, List< double >> pre_calc_values_;

        private void Initialize()
        {
            _start = DateTime.Now;
            // caches the tip and transfer info
            foreach (KeyValuePair<Channel, Transfer> kvp in TipAssignments)
            {
                tips_.Add(kvp.Key);
                transfers_.Add(kvp.Value);
                liquid_profiles_.Add( LiquidProfileLibrary.LoadLiquidProfileByName( kvp.Value.LiquidProfileName));
            }

            // group the channels
            grouped_channels_ = new GroupedChannels( tips_);

            // given the source stage angle, we can figure out where each channel should go
            List<string> tip_wells = new List<string>();
            foreach (Transfer t in transfers_)
                tip_wells.Add(t.SourceWellName);
            //! \todo freaking labware again
            _source_labware = LabwareDatabase.GetLabware( transfers_[0].Source.LabwareName);
            _dest_labware = LabwareDatabase.GetLabware( transfers_[0].Destination.LabwareName);

            x_positions_ = new List<double>();
            y_positions_ = new List<double>();
            // make rotation calculations
            for (int i = 0; i < tip_wells.Count; i++)
            {
                //! \todo why isn't this also in the Utils class somewhere???
                string well_name = tip_wells[i];
                // x, y is the location of tip1's well at 0º
                double x, y;
                Wells.GetWellDistanceFromCenterOfPlate(well_name, _source_labware.NumberOfWells, out x, out y);
                // now get the xy position when rotated to SourceAngle
                double x_rot, y_rot;
                Wells.GetXYAfterRotation(x, y, SourceAngle, SourceAngle < 0, out x_rot, out y_rot);
                double center_x, center_y;
                SourceStage.GetCenterPosition(tips_[i].GetID(), out center_x, out center_y);
                x_positions_.Add(center_x + x_rot);
                y_positions_.Add(center_y - y_rot);
            }

            // pre-populate z-axis and w-axis positions to go to:
            pre_calc_values_ = new Dictionary< PreCalculatedValues, List< double>>();

            pre_calc_values_[ PreCalculatedValues.ZSrcStageTeachpoint] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.ZDstStageTeachpoint] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.ZPosSrcLabwareClearanceOffset] = new List< double>();
            // pre_calc_values_[ PreCalculatedValues.ZPosSrcTopOfLabwareOffset] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateBeginOffset] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateEndOffset] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.ZPosClearBetweenSrcAndDst] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.ZPosDstLabwareClearanceOffset] = new List< double>();
            // pre_calc_values_[ PreCalculatedValues.ZPosDstTopOfLabwareOffset] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseBeginOffset] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseEndOffset] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.WPosBegin] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.WPosAspirateBegin] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.WPosAspirateEnd] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.WPosDispenseEnd] = new List< double>();
            pre_calc_values_[ PreCalculatedValues.WPosEnd] = new List< double>();

            for( int i = 0; i < tips_.Count; ++i){
                // DKM 2010-08-09
                double src_labware_thickness = _source_labware.Thickness;
                double src_well_bottom_z = _source_labware.Thickness - _source_labware.WellDepth;

                // pre-calculate source stage teachpoint list.
                pre_calc_values_[ PreCalculatedValues.ZSrcStageTeachpoint].Add( Teachpoints.GetStageTeachpoint( tips_[ i].GetID(), SourceStage.GetID()).UpperLeft[ "z"]);
                pre_calc_values_[ PreCalculatedValues.ZDstStageTeachpoint].Add( Teachpoints.GetStageTeachpoint( tips_[ i].GetID(), DestinationStage.GetID()).UpperLeft[ "z"]);

                // DKM 2010-08-09 these values are now always positive, and do not include the stage teachpoint.
                //                They are only offsets, and it's up to the hardware layer to figure out whether
                //                or not to add/subtract to/from the stage teachpoint.
                pre_calc_values_[ PreCalculatedValues.ZPosSrcLabwareClearanceOffset].Add( src_labware_thickness + 5);
                // pre_calc_values_[ PreCalculatedValues.zPosSrcTopOfLabwareOffset].Add( src_labware_thickness);
                pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateBeginOffset].Add( src_well_bottom_z + transfers_[i].AspirateDistanceFromWellBottomMm.Value + liquid_profiles_[ i].ZMoveDuringAspirating);
                pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateEndOffset].Add( src_well_bottom_z + transfers_[i].AspirateDistanceFromWellBottomMm.Value);

                // make sure from top to bottom: top of labware, beginning of aspirate, and ending of aspirate.
                // DKM 2010-08-10 now that we deal with offsets from a teachpoint, the sign has to change
                //                e.g. the top of the labware should be higher than the beginning of aspirate
                // Debug.Assert( pre_calc_values_[ PreCalculatedValues.ZPosSrcTopOfLabwareOffset][ i] >= pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateBeginOffset][ i]);
                Debug.Assert( pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateBeginOffset][ i] >= pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateEndOffset][ i]);

                // DKM 2010-08-09
                double dst_labware_thickness = _dest_labware.Thickness;
                double dst_well_bottom_z = _dest_labware.Thickness - _dest_labware.WellDepth;

                // DKM 2010-08-09 these values are now always positive, and do not include the stage teachpoint.
                //                They are only offsets, and it's up to the hardware layer to figure out whether
                //                or not to add/subtract to/from the stage teachpoint.
                pre_calc_values_[ PreCalculatedValues.ZPosClearBetweenSrcAndDst].Add( Math.Max( src_labware_thickness, dst_labware_thickness) + 5);
                pre_calc_values_[ PreCalculatedValues.ZPosDstLabwareClearanceOffset].Add( dst_labware_thickness + 5);
                // pre_calc_values_[ PreCalculatedValues.ZPosDstTopOfLabwareOffset].Add( dst_labware_thickness);
                pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseBeginOffset].Add( dst_well_bottom_z + transfers_[i].DispenseDistanceFromWellBottomMm.Value);
                pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseEndOffset].Add( dst_well_bottom_z + transfers_[i].DispenseDistanceFromWellBottomMm.Value + liquid_profiles_[ i].ZMoveDuringDispensing);

                // Debug.Assert( pre_calc_values_[ PreCalculatedValues.ZPosDstTopOfLabwareOffset][ i] >= pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseEndOffset][ i]);
                Debug.Assert( pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseEndOffset][ i] >= pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseBeginOffset][ i]);

                bool is_interpolated = false;
                pre_calc_values_[ PreCalculatedValues.WPosBegin].Add( 0.0);
                pre_calc_values_[ PreCalculatedValues.WPosAspirateBegin].Add( liquid_profiles_[ i].PreAspirateVolume);
                try {
                    pre_calc_values_[ PreCalculatedValues.WPosAspirateEnd].Add( liquid_profiles_[ i].PreAspirateVolume + liquid_profiles_[ i].GetAdjustedVolume( transfers_[ i].TransferVolume, out is_interpolated));
                } catch( BioNex.Shared.Utils.PiecewiseLinearFunction.InputOutOfRangeException) {
                    //! \todo this is not quite what we want; if we aren't able to calculate the proper transfer volume, then we need to figure out the proper way to handle this situation.
                    pre_calc_values_[ PreCalculatedValues.WPosAspirateEnd].Add( liquid_profiles_[ i].PreAspirateVolume + transfers_[i].TransferVolume);
                }
                pre_calc_values_[ PreCalculatedValues.WPosDispenseEnd].Add( liquid_profiles_[ i].PreAspirateVolume - liquid_profiles_[ i].PostDispenseVolume);
                pre_calc_values_[ PreCalculatedValues.WPosEnd].Add( 0.0);

                // make sure from emptier to fuller: 0, DispenseEnd, AspirateBegin, and AspirateEnd.
                Debug.Assert( 0 <= pre_calc_values_[ PreCalculatedValues.WPosDispenseEnd][ i]);
                Debug.Assert( pre_calc_values_[ PreCalculatedValues.WPosDispenseEnd][ i] <= pre_calc_values_[ PreCalculatedValues.WPosAspirateBegin][ i]);
                Debug.Assert( pre_calc_values_[ PreCalculatedValues.WPosAspirateBegin][ i] <= pre_calc_values_[ PreCalculatedValues.WPosAspirateEnd][ i]);
            }

            LastTrigger = Trigger.Done;
        }

        private void MoveToSourceYR() /* FYC: actually, just start all the moves */
        private void TipsOn()
        {
            // TIP PRESSING GOES HERE
            // we need the following information passed to the tip changing state machine
            // tip_assignments: tells us which channels were locked by the calculations
            // TipBoxStage: this is the stage that is dedicated to tip boxes
            // _teachpoints: so we know how to get to the desired tip "wells"
            // _hw: so we can lock the stage and also get at the hardware to make it move
            // _tip_tracker: keeps track of which tips are still available for pressing
            TipsOnStateMachine tips_on_sm = new TipsOnStateMachine( ErrorInterface);
            TipBox tipbox = LabwareDatabase.GetLabware( "tipbox") as TipBox;
            Debug.Assert( tipbox != null, "Labware specified is not a tipbox");
            string message = "(gantt) changing tips ";
            foreach( KeyValuePair<Channel,Transfer> kvp in TipAssignments)
                message += kvp.Key.GetID().ToString() + ", ";
            message += " took {0}s";
            DateTime start = DateTime.Now;
            tips_on_sm.Execute( TipAssignments, TipBoxStage, Teachpoints, Hardware, TipTracker, tipbox);
            double total_time = (DateTime.Now - start).TotalSeconds;
            _log.Debug( String.Format( message, total_time));
            
            if( SourceStage.Available)
                LastTrigger = Trigger.Done;
            else
                LastTrigger = Trigger.SourceStageInUse;
        }

        private void PreMoveXToSource()
        {
            try {
                // pre-aspirate.
                //! \todo test to see if PreAspirate is doing what Felix was doing with MoveAbsolute( false)
                grouped_channels_.PreAspirate( pre_calc_values_[ PreCalculatedValues.WPosAspirateBegin], false);
                // move X and Z blended:
                // set up the move.
                grouped_channels_.MoveAbsoluteBlendedXZOffset( x_positions_, pre_calc_values_[ PreCalculatedValues.ZSrcStageTeachpoint], pre_calc_values_[ PreCalculatedValues.ZPosSrcTopOfLabwareOffset]);
                // do the move and wait.
                grouped_channels_.WaitForMoveBlendedComplete();
                while( !SourceStage.Available)
                    Thread.Sleep( 10);
                LastTrigger = Trigger.Done;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void PreMoveXToSourceError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveToSourceYR()
        {
            try {
                // (non-blocking) move channels near source stage.  (probably the longest move, so start it first.)
                grouped_channels_.MoveAbsoluteBlendedXZOffset( x_positions_, pre_calc_values_[ PreCalculatedValues.ZSrcStageTeachpoint], pre_calc_values_[ PreCalculatedValues.ZPosSrcLabwareClearanceOffset], false, true);
                // (non-blocking) move source stage.
                SourceStage.MoveAbsolute( y_positions_[ 0], SourceAngle, false);
                // (non-blocking) pre-aspirate.  (probably the shortest move, so start it last.)
                grouped_channels_.PreAspirate( pre_calc_values_[ PreCalculatedValues.WPosAspirateBegin], false);
                // FYC: any metrics we should be logging?
                // double total_time = (DateTime.Now - _start).TotalSeconds;
                // _log.Debug( String.Format( "(gantt) [{0}] move source stage took {1}s", _channel_ids, total_time));
                // FYC: where should the following line be placed? what are we actually trying to measure?
                // lock the source plate!
                if (tips_.Count == 1)
                    Hardware.RequestStageForTransfer(tips_[0], SourceStage, false);
                else
                    Hardware.RequestStageForTransfer(tips_[0], tips_[1], SourceStage);

                // move stage, wait for completion
                SourceStage.MoveAbsolute(y_positions_[0], SourceAngle);
                double total_time = (DateTime.Now - _start).TotalSeconds;
                _log.Debug( String.Format( "(gantt) [{0}] move source stage took {1}s", _channel_ids, total_time));
                _start = DateTime.Now;
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveToSourceYRError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveToSource()
        {
            try {
                // wait for pre-aspirate.  (get this check out of the way.)
                grouped_channels_.PreAspirate( pre_calc_values_[ PreCalculatedValues.WPosAspirateBegin], true);
                // block on source-stage move.  we need the source stage in place before we can go farther on z.
                SourceStage.MoveAbsolute( y_positions_[ 0], SourceAngle, true);
                // FYC. for now, wait for channels to move near source stage.  in the future, i'd like to be able to set a new destination for z.
                grouped_channels_.WaitForMoveBlendedComplete();
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveToSourceError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveIntoSource()
        {
            try {                
                // calculate velocities for piercing liquid with z.
                var z_velocities = 
                    from i in Enumerable.Range( 0, tips_.Count)
                    select Math.Abs( pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateBeginOffset][ i] - pre_calc_values_[ PreCalculatedValues.ZPosSrcLabwareClearanceOffset][ i]) / liquid_profiles_[ i].TimeToEnterLiquid;
                // move into source.
                grouped_channels_.MoveAbsoluteZOffset( pre_calc_values_[ PreCalculatedValues.ZSrcStageTeachpoint], pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateBeginOffset], z_velocities.ToList< double>(), true, true);
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveIntoSourceError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        /// <remarks>
        /// Note from Dave: I had to change this around to support the GroupedChannels concept.  After
        /// looking over this code, it looks like this was using IAxes in a way I hadn't used it before,
        /// and is pretty cool.  All X and W axes are added to the same IAxes object to command all to
        /// move simultaneously.  With GroupedChannels, I have to do things a little differently, and
        /// need to call MoveAbsoluteWZ in a loop.  So the original loop code had to be changed a little.
        /// </remarks>
        private void Aspirate()
        {
            try {
                List<double> w_velocities = new List<double>();
                List<double> w_accel_factors = new List<double>();
                int delay_ms = 0;
                for (int i = 0; i < tips_.Count; i++){
                    w_velocities.Add( liquid_profiles_[ i].RateToAspirate);

                    double w_accel_factor = liquid_profiles_[ i].MaxAccelDuringAspirate / 100.0;
                    if( w_accel_factor > 1.0){
                        w_accel_factor = 1.0;
                    }
                    if( w_accel_factor < 0.01) {
                        w_accel_factor = 0.01;
                    }
                    w_accel_factors.Add( w_accel_factor);

                    int post_aspirate_delay_ms = ( int)( liquid_profiles_[ i].PostAspirateDelay * 1000);
                    if( post_aspirate_delay_ms > delay_ms){
                        delay_ms = post_aspirate_delay_ms;
                    }
                }
                grouped_channels_.AspirateOrDispenseNew( pre_calc_values_[ PreCalculatedValues.WPosAspirateEnd], pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateEndOffset], w_velocities, w_accel_factors, pre_calc_values_[ PreCalculatedValues.ZSrcStageTeachpoint]);

                // post-aspirate delay.
                TimeSpan interval = new TimeSpan( 0, 0, 0, 0, delay_ms);
                Thread.Sleep( interval);

                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void AspirateError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveOutOfSource()
        {
            try {
                // calculate velocities for unpiercing liquid with z.
                var z_velocities = 
                    from i in Enumerable.Range( 0, tips_.Count)
                    select Math.Abs( pre_calc_values_[ PreCalculatedValues.ZPosSrcLabwareClearanceOffset][ i] - pre_calc_values_[ PreCalculatedValues.ZPosSrcAspirateEndOffset][ i]) / liquid_profiles_[ i].TimeToExitLiquid;
                // move out of source.
                grouped_channels_.MoveAbsoluteZOffset( pre_calc_values_[ PreCalculatedValues.ZSrcStageTeachpoint], pre_calc_values_[ PreCalculatedValues.ZPosSrcLabwareClearanceOffset], z_velocities.ToList< double>(), true, true);
                Hardware.UnlockStage( SourceStage);
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveOutOfSourceError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveFromSource()
        {
            try {
                // move z up to clearance height.
                grouped_channels_.MoveAbsoluteZOffset( pre_calc_values_[ PreCalculatedValues.ZSrcStageTeachpoint], pre_calc_values_[ PreCalculatedValues.ZPosClearBetweenSrcAndDst], true);
                // debug message.
                double total_time = (DateTime.Now - _start).TotalSeconds;
                _log.Debug( String.Format( "(gantt) [{0}] aspirate took {1}s", _channel_ids, total_time));
                // unlock source stage.
                Hardware.UnlockStage(SourceStage);
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveFromSourceError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void LockDestPlate()
        {
            if( !DestinationStage.Available) {
                LastTrigger = Trigger.DestStageInUse;
                return;
            }

            if (tips_.Count == 1)
                Hardware.RequestStageForTransfer(tips_[0], DestinationStage, true);
            else
                Hardware.RequestStageForTransfer(tips_[0], tips_[1], DestinationStage);

            _start = DateTime.Now;
            LastTrigger = Trigger.Done;
        }

        private void PreMoveXToDest()
        {
            try {
                // move X and Z blended:
                // set up the move.
                grouped_channels_.MoveAbsoluteBlendedXZOffset( x_positions_, pre_calc_values_[ PreCalculatedValues.ZDstStageTeachpoint], pre_calc_values_[ PreCalculatedValues.ZPosDstTopOfLabwareOffset]);
                // do the move and wait.
                grouped_channels_.WaitForMoveBlendedComplete();
                while( !SourceStage.Available)
                    Thread.Sleep( 10);
                LastTrigger = Trigger.Done;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void PreMoveXToDestError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveToDestYR()
        {
            try {
                // given the source stage angle, we can figure out where each channel should go
                List<string> tip_wells = new List<string>();
                foreach (Transfer t in transfers_)
                    tip_wells.Add(t.DestinationWellNames[0]);

                x_positions_ = new List<double>();
                y_positions_ = new List<double>();
                // make rotation calculations
                for (int i = 0; i < tip_wells.Count; i++)
                {
                    string well_name = tip_wells[i];
                    // x, y is the location of tip1's well at 0º
                    double x, y;
                    Wells.GetWellDistanceFromCenterOfPlate(well_name, _dest_labware.NumberOfWells, out x, out y);
                    // now get the xy position when rotated to SourceAngle
                    double x_rot, y_rot;
                    Wells.GetXYAfterRotation(x, y, DestAngle, DestAngle < 0, out x_rot, out y_rot);
                    double center_x, center_y;
                    DestinationStage.GetCenterPosition(tips_[i].GetID(), out center_x, out center_y);
                    x_positions_.Add(center_x + x_rot);
                    y_positions_.Add(center_y - y_rot);
                }
                // (non-blocking) move channels near destination stage.  (probably the longest move, so start it first.)
                grouped_channels_.MoveAbsoluteBlendedXZOffset( x_positions_, pre_calc_values_[ PreCalculatedValues.ZDstStageTeachpoint], pre_calc_values_[ PreCalculatedValues.ZPosDstLabwareClearanceOffset], false, true);
                // (non-blocking) move destination stage.
                DestinationStage.MoveAbsolute( y_positions_[ 0], DestAngle, false);
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveToDestYRError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveToDest()
        {
            try {
                // block on destination-stage move.  we need the destination stage in place before we can go farther on z.
                DestinationStage.MoveAbsolute( y_positions_[ 0], DestAngle, true);
                // FYC. for now, wait for channels to move near destination stage.  in the future, i'd like to be able to set a new destination for z.
                grouped_channels_.WaitForMoveBlendedComplete();
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveToDestError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveIntoDest()
        {
            try {
                // calculate velocities for piercing liquid with z.
                var z_velocities = 
                    from i in Enumerable.Range( 0, tips_.Count)
                    select Math.Abs( pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseBeginOffset][ i] - pre_calc_values_[ PreCalculatedValues.ZPosDstLabwareClearanceOffset][ i]) / liquid_profiles_[ i].TimeToEnterLiquid;
                // move into dest.
                grouped_channels_.MoveAbsoluteZOffset( pre_calc_values_[ PreCalculatedValues.ZDstStageTeachpoint], pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseBeginOffset], z_velocities.ToList< double>(), true, true);
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveIntoDestError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void Dispense()
        {
            try {
                List<double> w_velocities = new List<double>();
                List<double> w_accel_factors = new List<double>();
                int delay_ms = 0;
                for (int i = 0; i < tips_.Count; i++){
                    w_velocities.Add( liquid_profiles_[ i].RateToDispense);

                    double w_accel_factor = liquid_profiles_[ i].MaxAccelDuringDispense / 100.0;
                    if( w_accel_factor > 1.0){
                        w_accel_factor = 1.0;
                    }
                    if( w_accel_factor < 0.01) {
                        w_accel_factor = 0.01;
                    }
                    w_accel_factors.Add( w_accel_factor);

                    int post_dispense_delay_ms = ( int)( liquid_profiles_[ i].PostDispenseDelay * 1000);
                    if( post_dispense_delay_ms > delay_ms){
                        delay_ms = post_dispense_delay_ms;
                    }
                }
                grouped_channels_.AspirateOrDispenseNew( pre_calc_values_[ PreCalculatedValues.WPosDispenseEnd], pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseEndOffset], w_velocities, w_accel_factors, pre_calc_values_[ PreCalculatedValues.ZDstStageTeachpoint]);

                // post-dispense delay.
                TimeSpan interval = new TimeSpan( 0, 0, 0, 0, delay_ms);
                Thread.Sleep( interval);

                for( int i=0; i<tips_.Count(); i++) {
                    log4net.ThreadContext.Properties["ChannelID"] = tips_[i].GetID().ToString();
                    log4net.ThreadContext.Properties["DeviceName"] = "Bumblebee";
                    Transfer t = transfers_[i];
                    log4net.ThreadContext.Properties["SourceBarcode"] = t.Source.Barcode;
                    log4net.ThreadContext.Properties["SourceWell"] = t.SourceWellName;
                    log4net.ThreadContext.Properties["DestinationBarcode"] = t.Destination.Barcode;
                    log4net.ThreadContext.Properties["DestinationWell"] = t.DestinationWellNames[0];
                    log4net.ThreadContext.Properties["Volume"] = (t.TransferUnits == VolumeUnits.ul ? t.TransferVolume : t.TransferVolume * 1000).ToString();
                    _pipette_log.Info( "transfer complete");
                }
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void DispenseError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveOutOfDest()
        {
            try {
                // calculate velocities for unpiercing liquid with z.
                var z_velocities = 
                    from i in Enumerable.Range( 0, tips_.Count)
                    select Math.Abs( pre_calc_values_[ PreCalculatedValues.ZPosDstLabwareClearanceOffset][ i] - pre_calc_values_[ PreCalculatedValues.ZPosDstDispenseEndOffset][ i]) / liquid_profiles_[ i].TimeToExitLiquid;
                // move out of dest.
                grouped_channels_.MoveAbsoluteZOffset( pre_calc_values_[ PreCalculatedValues.ZDstStageTeachpoint], pre_calc_values_[ PreCalculatedValues.ZPosDstLabwareClearanceOffset], z_velocities.ToList< double>(), true, true);
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveOutOfDestError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void MoveFromDest()
        {
            try {
                // move z up to safe height.
                List<double> z_positions = new List<double>();
                foreach (Channel c in tips_)
                    z_positions.Add( z_safe);
                grouped_channels_.MoveAbsoluteZ( z_positions);
                // debug message.
                double total_time = (DateTime.Now - _start).TotalSeconds;
                _log.Debug( String.Format( "(gantt) [{0}] dispense took {1}s", _channel_ids, total_time));
                LastTrigger = Trigger.MoveComplete;
            } catch( AxisException ex) {
                _last_error = ex.Message;
                ex.Axis.ResetFaults();
                ex.Axis.Enable( true);
                LastTrigger = Trigger.MoveError;
            }
        }

        private void MoveFromDestError()
        {
            HandleErrorWithRetryOnly( _last_error);
        }

        private void UnlockDestPlate()
        {
            Hardware.UnlockStage(DestinationStage);

            LastTrigger = Trigger.Done;
        }

        private void RunTipHandlingStateMachine()
        {
            try
            {
                _start = DateTime.Now;
                if( TipHandlingMethod == "Wash tip") {
                    WashStateMachine sm = new WashStateMachine();
                    sm.Execute( Teachpoints, tips_, Hardware, grouped_channels_);
                } else if( TipHandlingMethod == "Change tip") {
                    TipShuckStateMachine sm = new TipShuckStateMachine( ErrorInterface);
                    sm.Execute( Teachpoints, tips_, Hardware, grouped_channels_);
                } else
                    return;
                double total_time = (DateTime.Now - _start).TotalSeconds;

                // unlock the channels that got used
                foreach( Channel c in tips_) {
                    Hardware.UnlockChannel( c);
                    /*
                    // mark the dest_plate's well as Used
                    foreach( string wellname in kvp.Value.DestinationWellNames)
                        _current_dest_plate.SetWellUsageState( wellname, Wells.WellUsageStates.Used);
                     */
                }

                _log.Debug( String.Format( "(gantt) [{0}] tip shucking took {1}s", _channel_ids, total_time));
            }
            catch (Exception ex)
            {
                throw ex;
            }

            LastTrigger = Trigger.Done;
        }

        private void Done()
        {
            LastTrigger = Trigger.Exit;
        }

        /// <summary>
        /// Registers only the default "retry" behavior with the ErrorInterface.  It is assumed
        /// that this is getting called because of a servo drive fault, so it will reset
        /// faults and enable the axis before continuing.
        /// </summary>
        /// <param name="message"></param>
        private void HandleErrorWithRetryOnly(string message)
        {
            if( message.Contains( "Short-circuit")) {
                LastTrigger = Trigger.Retry;
                return;
            }

            try {
                string retry = "Try move again";
                BioNex.Shared.LibraryInterfaces.ErrorData error = new BioNex.Shared.LibraryInterfaces.ErrorData(message, new List<string> { retry });
                ErrorInterface.AddError(error);
                WaitHandle.WaitAny(error.EventArray);
                if (error.TriggeredEvent == retry)
                    LastTrigger = Trigger.Retry;
            } catch( Exception ex) {
                _last_error = ex.Message;
                Debug.Assert( false, ex.Message);
            }
        }
    }
}
