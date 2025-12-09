using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using BioNex.Hive.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.TechnosoftLibrary;
using log4net;

namespace BioNex.Hive.Executor
{
    public class FlyByBarcodeReadingStateMachine : HiveStateMachine< FlyByBarcodeReadingStateMachine.State, FlyByBarcodeReadingStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // inner classes.
        // ----------------------------------------------------------------------
        private class FlyByException : ApplicationException
        {
            public FlyByException( string message) : base( message) {}
        }

        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum State
        {
            Start,
            MoveToStart, MoveToStartError,
            ReadBarcodes,
            RescanMoreSlowlyIfRetriesRemaining,
            ReadNextPlate,
            ReadNextPlateError,
            UpdateInventory,
            UpdateInventoryError,
            End, Abort,
        }
        // ----------------------------------------------------------------------
        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
            StartOver,
            StrobeSinglePlates,
            NoMorePlates,
            ErrorRescanSlower,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private HiveTeachpoint _top_shelf_teachpoint{ get; set; }
        private HiveTeachpoint _bottom_shelf_teachpoint { get; set; }
        private double _shelf_pitch_mm { get; set; }
        // need this to be a separate member variable because we adjust its value if a misread occurs
        private double _velocity_mmps { get; set; }

        // retry misreads behavior variables here
        private int NumTries { get; set; }
        private const int MaxTries = 1;

        private IAxis _x { get; set; }
        private IAxis _z { get; set; }

        private List<string> barcodes_;
        private ScanningParameters Parameters { get; set; }

        private readonly AutoResetEvent _flyby_done = new AutoResetEvent( false);
        // MUST be a manual reset event because after aborting, we need to check the state
        // of this event again to see if we need to skip to the End state
        private readonly ManualResetEvent InterlockEvent = new ManualResetEvent( false);

        /// <summary>
        /// This gets created upon initialization, because we need a way to figure out which teachpoints
        /// had misread issues.
        /// </summary>
        private readonly List<string> _scanned_teachpoint_names;
        /// <summary>
        /// This gets created after Zipping the results.  Holds the teachpoint names that need to be rescanned statically.
        /// </summary>
        private readonly List<Tuple<int,string>> _teachpoints_to_rescan = new List<Tuple<int,string>>();
        private readonly AccessibleDeviceInterface _scanned_device;

        private static readonly ILog _log = LogManager.GetLogger(typeof(FlyByBarcodeReadingStateMachine));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public FlyByBarcodeReadingStateMachine( HiveExecutor executor, ManualResetEvent ended_aborted_event, AccessibleDeviceInterface scanned_device, ScanningParameters sp)
            : base( executor, ended_aborted_event, typeof( FlyByBarcodeReadingStateMachine), State.Start, State.End, State.Abort, Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, executor.HandleError)
        {
            Parameters = sp;
            _scanned_device = scanned_device;
            _top_shelf_teachpoint = executor.Hardware.GetTeachpoint( scanned_device.Name, sp.TopShelfTeachpointName);
            _bottom_shelf_teachpoint = executor.Hardware.GetTeachpoint( scanned_device.Name, sp.BottomShelfTeachpointName);
            _velocity_mmps = sp.ScanVelocityMmPerSec;
            _scanned_teachpoint_names = Shared.Utils.Strings.GenerateIntermediateTeachpointNames( sp.TopShelfTeachpointName, sp.BottomShelfTeachpointName);

            double bottom_shelf_z = _bottom_shelf_teachpoint.Z;
            _shelf_pitch_mm = Math.Abs( _top_shelf_teachpoint.Z - bottom_shelf_z) / (Parameters.NumberOfShelves - 1);

            // cache IAxes
            _x = executor.Hardware.XAxis;
            _z = executor.Hardware.ZAxis;

            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        /// <summary>
        /// Call this from the application to figure out where problems are.
        /// Empty string = missed strobe
        /// NOREAD = Microscan-specific read failure string
        /// anything else = the actual barcode that was read
        /// </summary>
        /// <returns></returns>
        public List<string> GetBarcodes()
        {
            return barcodes_;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Called by robot to abort if a safety interlock gets tripped.  Probably should make this
        /// state machine register an event handler for interlocks...
        /// </summary>
        public void AbortDueToSafetyEvent()
        {
            InterlockEvent.Set();
        }
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.MoveToStart);
            ConfigureState( State.MoveToStart, MoveToStart, State.ReadBarcodes, State.MoveToStartError);
            SM.Configure(State.ReadBarcodes)
                .Permit(Trigger.Success, State.End)
                .Permit(Trigger.Failure, State.RescanMoreSlowlyIfRetriesRemaining)
                .Permit(Trigger.StrobeSinglePlates, State.ReadNextPlate)
                .Permit(Trigger.ErrorRescanSlower, State.RescanMoreSlowlyIfRetriesRemaining)
                .OnEntry( ReadBarcodes);
            SM.Configure(State.ReadNextPlate)
                .PermitReentry(Trigger.Success)
                .Permit(Trigger.NoMorePlates, State.End)
                .Permit(Trigger.Failure, State.ReadNextPlateError)
                .OnEntry( ReadNextPlate);
            SM.Configure(State.ReadNextPlateError)
                .Permit( Trigger.Retry, State.ReadNextPlate)
                .OnEntry( f => HandleErrorWithRetryOnly( LastError));
            SM.Configure(State.RescanMoreSlowlyIfRetriesRemaining)
                .Permit(Trigger.StartOver, State.MoveToStart)
                .Permit(Trigger.Failure, State.End)
                .OnEntry( RescanMoreSlowly);
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.Abort, AbortedStateFunction);
        }
        // ----------------------------------------------------------------------
        private void MoveToStart()
        {
            try {
                double z_tool = _top_shelf_teachpoint.Z + Parameters.StartingPointOffset;
                double y_tool = _top_shelf_teachpoint.Y;
                double z_world = HiveMath.ConvertZToolToWorldUsingY( Executor.Hardware.Config.ArmLength, Executor.Hardware.Config.FingerOffsetZ, z_tool, y_tool);
                // DKM 2010-09-20 it's possible that going 50mm above the top teachpoint is not possible because
                //                it could be past the Z max limit
                double z_max_limit = Executor.Hardware.ZAxis.Settings.MaxLimit;
                if( z_world > z_max_limit)
                    z_world = z_max_limit;

                Executor.Hardware.TuckToXZ( _top_shelf_teachpoint.X, z_world, false);
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }
        // ----------------------------------------------------------------------
        private void FlyByComplete( IAsyncResult iar)
        {
            AsyncResult result = iar as AsyncResult;
            BioNex.Shared.Microscan.MicroscanReader.ReadBarcodesDelegate caller = (BioNex.Shared.Microscan.MicroscanReader.ReadBarcodesDelegate) result.AsyncDelegate;
            Debug.Assert( result != null);
            try {
                string result_message = caller.EndInvoke( out barcodes_, iar);
                // DKM 2010-08-19 don't replace NOREAD anymore -- we are actually going to use this in the database code
                //                to handle tipboxes / backwards plates as a special case
                //_barcodes = _barcodes.ReplaceItems<string>( Constants.NoRead, String.Empty).ToList();
                if( result_message != String.Empty) {
                    //throw new FlyByException( "Failed to read barcodes: " + result_message);
                    Console.WriteLine( "Failed to read barcodes: " + result_message);
                }
                _flyby_done.Set();
            } catch( IOException ex) {
                // if we get here, something happened as a result of an inventory failure.  I'm not sure
                // what the specific reason is yet, but you can reproduce it by scanning a rack with
                // a top teachpoint that's too high.  The reinventory step will fail after the initial setup
                // move.  If you then click another rack and reinventory it, it will scan the previous rack
                // and then fail here.
                _log.Debug( ex);
            } catch( Exception ex) {
                _log.Debug( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void ReadBarcodes()
        {
            try {
                double z_tool = _top_shelf_teachpoint.Z;
                double y_tool = _top_shelf_teachpoint.Y;
                double z_motor = HiveMath.ConvertZToolToWorldUsingY( Executor.Hardware.Config.ArmLength, Executor.Hardware.Config.FingerOffsetZ, z_tool, y_tool);
                z_motor += 16.0; // set the trigger position to 10.0mm above teachpoints
                int top_shelf_pos_iu = _z.ConvertToCounts( z_motor);
                int shelf_spacing_iu = _z.ConvertToCounts( _shelf_pitch_mm);
                MotorSettings settings = _z.Settings;
                double velocity_iu = TechnosoftConnection.ConvertVelocityToIU( _velocity_mmps, settings.SlowLoopServoTimeS, settings.EncoderLines, settings.GearRatio) * (_z.GetSpeedFactor());
                double accel_iu = TechnosoftConnection.ConvertAccelerationToIU( Parameters.ScanAccelMmPerSec2, settings.SlowLoopServoTimeS, settings.EncoderLines, settings.GearRatio);
                int jerk_iu = settings.Jerk;

                Executor.Hardware.BarcodeReader.FlyByRead( Parameters.NumberOfShelves, FlyByComplete);

                // Check to make sure we don't try to trigger a BCR scan at a Z encoder position < 0.
                // If it will be close (within 1000 counts), let's change the shelf_spacing so everything will work.
                double min_pos_iu = top_shelf_pos_iu - ( shelf_spacing_iu * (Parameters.NumberOfShelves - 1) );
                if ( (min_pos_iu > -1000) && (min_pos_iu < 0) )
                    shelf_spacing_iu = top_shelf_pos_iu / (Parameters.NumberOfShelves-1) - 1;

                // #362: attempt to avoid hanging issues by using a timeout.  I have seen cases where the barcode reader doesn't scan all
                // of the plates, so it gets stuck because not all of the barcodes end up in the buffer.  Figure out a logical time for
                // scanning all of the shelves, given the top and bottom teachpoints, as well as the scanning speed.  Timeout if total
                // time is 50% longer than the estimate.
                double total_distance_mm = (Parameters.NumberOfShelves - 1) * _shelf_pitch_mm;
                double time_estimate_s = total_distance_mm / _velocity_mmps;

                try {
                    _z.CallFunctionWithPeriodicActions( "BCR_SCAN_DOWN2", top_shelf_pos_iu, shelf_spacing_iu, Parameters.NumberOfShelves,
                                                        velocity_iu, accel_iu, jerk_iu);
                } catch( Exception) {
                    // this catches the case where the function doesn't complete in time.  One possible reason
                    // is that the interlocks got tripped during the scan
                    
                    // it's a little weird to fire success in this case, but that's what transitions us to the End state
                    Fire( Trigger.Success);
                    _log.Error( "Reinventory did not complete successfully");
                    return;
                }

                // DKM 2011-06-07 the bcr scanning function is now effectively blocking, but we do still need to let
                //                the reader finish sending its data over the wire!  So if we get here, we should
                //                wait for _flyby_done.
                int wait_result = WaitHandle.WaitAny( new WaitHandle[] { _flyby_done }, 5000); // wait an additional 5 seconds for barcode information, which should be more than enough.
                if( wait_result == WaitHandle.WaitTimeout) {
                    const string message = "Barcode reader did not strobe correctly.  Please try reinventorying.";
                    _log.Error( message);
                    Fire( Trigger.Success);
                    return;
                }

                // check for missed strobes.  when this happens, you will get < _number_of_shelves barcodes back
                int num_missed_strobes = Parameters.NumberOfShelves - barcodes_.Count();
                // need to match up the barcode result with the reread condition mask, now that each shelf
                // has its own plate type associated with it.
                var results = barcodes_.Zip( Parameters.RereadConditionMasks, EvaluateScan).ToList();
                // save the teachpoint names that need to be rescanned
                _teachpoints_to_rescan.Clear();
                for( int i=0; i<results.Count(); i++) {
                    // skip "true" results because those aren't misreads
                    if( results[i])
                        continue;

                    _teachpoints_to_rescan.Add( new Tuple<int,string>(i,_scanned_teachpoint_names[i]));
                }

                int num_problems = (from x in results where !x select x).Count();

                if( num_problems == 0) {
                    Fire( Trigger.Success);
                } else if( num_problems < Parameters.BarcodeMisreadThreshold || (num_problems != 0 && NumTries >= MaxTries)) {
                    // we'll start static scanning if, on the first pass, we have less misreads than the threshold,
                    // or if we're on the second, slower pass and got ANY misreads at all
                    // in the next state, we'll use _teachpoints_to_rescan to take our static images
                    Fire(Trigger.StrobeSinglePlates);
                } else {
                    Fire(Trigger.ErrorRescanSlower);
                }
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// This is used by zip to compare a barcode with its plate type to determine if it is a misread
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private static bool EvaluateScan( string barcode, byte mask)
        {
            // return false if we don't want to allow NOREADs, but did get a NOREAD
            if( (mask & ScanningParameters.RereadNoRead) != 0 && Constants.IsNoRead(barcode)) {
                return false;
            }
            return true;
        }
        // ----------------------------------------------------------------------
        private void ReadNextPlate()
        {
            try {
                // if another plate to scan
                if( _teachpoints_to_rescan.Count() > 0) {
                    // move to the teachpoint
                    Executor.Hardware.MoveToDeviceLocationForBCRStrobe( _scanned_device, _teachpoints_to_rescan.First().Item2, false);
                    // scan the barcode
                    string barcode = Executor.Hardware.ReadBarcode();
                    barcodes_[_teachpoints_to_rescan.First().Item1] = barcode;
                    _teachpoints_to_rescan.RemoveAt( 0);
                    Fire( Trigger.Success);
                } else {
                    // if no more plates to scan
                    Fire( Trigger.NoMorePlates);
                }
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }
        // ----------------------------------------------------------------------
        private void RescanMoreSlowly()
        {
            // if we get here, then that means we didn't get all of the barcodes that we had expected, so try again ONCE
            if( NumTries++ < MaxTries) {
                // #254 cut the speed down to 150mm/s if we are retrying
                _velocity_mmps = 150;
                Fire( Trigger.StartOver);
            } else { // need to handle the error differently -- go to user input state
                NumTries = 0;
                Fire( Trigger.Failure);
            }
        }
    }
}
