using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.Teachpoints;
using System.Threading;
using System.Workflow.Runtime;
using BioNex.Shared.LabwareDatabase;
using BioNex.BumblebeeAlphaGUI.TipOperations;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.BumblebeeAlphaGUI.SingleSplitTransferScheduler
{
    public class SingleTransferStateMachine : Stateless.StateMachine<SingleTransferStateMachine.TransferState, SingleTransferStateMachine.TransferTrigger>
    {
        #region REED STUFF HERE
        private enum WashType { NoWash, WashSingle, WashDouble, WashSingleDumpToWaste, WashDoubleDumpToWaste };
        private double preaspirate_aspirate_mm = 1;
        //private double preaspirate_wash_mm = 2;
        //private double wash_zdown_from_teachpoint = 20; // 25 puts the bottom of the tip pretty close to the bottom of the waste area
        //private double wash_zdown_from_teachpoint_waste = 24;  //23 puts the tip right at the top of the waste liquid
        //private int num_asp_disp_wash = 2; // how many aspirates and dispense pairs to execute when washing
        //private double wash_w_move = 10; // how far up to move the syringe
        #endregion

        private MotorSettings _wash_settings = null;
        private MotorSettings _z_wash_settings = null;
        private MotorSettings _waste_w_settings = null;
        private const double wash_well_distance = 15;
        private const double wash_well_waste_distance = 13.5;

        // passed into workflow
        private AlphaHardware _hw { get; set; }
        private Stage _source_stage { get; set; }
        private Stage _dest_stage { get; set; }
        private Transfer _transfer_info { get; set; }
        private Channel _tip { get; set; }
        private Teachpoints _teachpoints { get; set; }
        private string _tip_handling_method { get; set; }
        private ILabwareDatabase _labware_database { get; set; }
        private IError ErrorInterface { get; set; }
        private string LastError { get; set; }

        private double _source_x;
        private double _source_y;
        private double _source_r;
        private double _dest_x;
        private double _dest_y;
        private double _dest_r;

        private ILabware _source_labware;
        private ILabware _dest_labware;

        // for blended moves
        BlendedAxes _xz;

        public enum TransferState
        {
            Idle,
            Initialization,
            MoveToSafeZ,
            MoveToSafeZError,
            MoveToSourceYR,
            MoveToSourceYRError,
            MoveXZIntoSource,
            MoveXZIntoSourceError,
            Aspirate,
            AspirateError,
            MoveOutOfSource,
            MoveOutOfSourceError,
            MoveToDestinationYR,
            MoveToDestinationYRError,
            MoveXZIntoDestination,
            MoveXZIntoDestinationError,
            Dispense,
            DispenseError,
            MoveOutOfDestination,
            MoveOutOfDestinationError,
            RunTipHandlingStateMachine
        }

        public enum TransferTrigger
        {
            ReceivedTipInfo,
            Initialized,
            Retry,
            MoveError,
            MoveComplete
        }

        public SingleTransferStateMachine( IError error_interface) : base(TransferState.Idle)
        {
            ErrorInterface = error_interface;

            Configure(TransferState.Idle)
                .Permit(TransferTrigger.ReceivedTipInfo, TransferState.Initialization)
                .OnEntry( () => Idle());
            Configure(TransferState.Initialization)
                .Permit(TransferTrigger.Initialized, TransferState.MoveToSafeZ)
                .OnEntry( () => Initialization());
            Configure(TransferState.MoveToSafeZ)
                .Permit(TransferTrigger.MoveComplete, TransferState.MoveToSourceYR)
                .Permit(TransferTrigger.MoveError, TransferState.MoveToSafeZError)
                .OnEntry( () => MoveToSafeZ());
            Configure(TransferState.MoveToSafeZError)
                .Permit(TransferTrigger.Retry, TransferState.MoveToSafeZ)
                .OnEntry( () => MoveToSafeZError());
            Configure(TransferState.MoveToSourceYR)
                .Permit(TransferTrigger.MoveComplete, TransferState.MoveXZIntoSource)
                .Permit(TransferTrigger.MoveError, TransferState.MoveToSourceYRError)
                .OnEntry( () => MoveToSourceYR());
            Configure(TransferState.MoveToSourceYRError)
                .Permit(TransferTrigger.Retry, TransferState.MoveToSourceYR)
                .OnEntry( () => MoveToSourceYRError());
            Configure(TransferState.MoveXZIntoSource)
                .Permit(TransferTrigger.MoveComplete, TransferState.Aspirate)
                .Permit(TransferTrigger.MoveError, TransferState.MoveXZIntoSourceError)
                .OnEntry( () => MoveXZIntoSource());
            Configure(TransferState.MoveXZIntoSourceError)
                .Permit(TransferTrigger.Retry, TransferState.MoveXZIntoSource)
                .OnEntry( () => MoveXZIntoSourceError());
            Configure(TransferState.Aspirate)
                .Permit(TransferTrigger.MoveComplete, TransferState.MoveOutOfSource)
                .Permit(TransferTrigger.MoveError, TransferState.AspirateError)
                .OnEntry( () => Aspirate());
            Configure(TransferState.AspirateError)
                .Permit(TransferTrigger.Retry, TransferState.Aspirate)
                .OnEntry( () => AspirateError());
            Configure(TransferState.MoveOutOfSource)
                .Permit(TransferTrigger.MoveComplete, TransferState.MoveToDestinationYR)
                .Permit(TransferTrigger.MoveError, TransferState.MoveOutOfSourceError)
                .OnEntry( () => MoveOutOfSource());
            Configure(TransferState.MoveOutOfSourceError)
                .Permit(TransferTrigger.Retry, TransferState.MoveOutOfSource)
                .OnEntry( () => MoveOutOfSourceError());
            Configure(TransferState.MoveToDestinationYR)
                .Permit(TransferTrigger.MoveComplete, TransferState.MoveXZIntoDestination)
                .Permit(TransferTrigger.MoveError, TransferState.MoveToDestinationYRError)
                .OnEntry( () => MoveToDestinationYR());
            Configure(TransferState.MoveToDestinationYRError)
                .Permit(TransferTrigger.Retry, TransferState.MoveToDestinationYR)
                .OnEntry( () => MoveToDestinationYRError());
            Configure(TransferState.MoveXZIntoDestination)
                .Permit(TransferTrigger.MoveComplete, TransferState.Dispense)
                .Permit(TransferTrigger.MoveError, TransferState.MoveXZIntoDestinationError)
                .OnEntry( () => MoveXZIntoDestination());
            Configure(TransferState.MoveXZIntoDestinationError)
                .Permit(TransferTrigger.Retry, TransferState.MoveXZIntoDestination)
                .OnEntry( () => MoveXZIntoDestinationError());
            Configure(TransferState.Dispense)
                .Permit(TransferTrigger.MoveComplete, TransferState.MoveOutOfDestination)
                .Permit(TransferTrigger.MoveError, TransferState.DispenseError)
                .OnEntry( () => Dispense());
            Configure(TransferState.DispenseError)
                .Permit(TransferTrigger.Retry, TransferState.Dispense)
                .OnEntry( () => DispenseError());
            Configure(TransferState.MoveOutOfDestination)
                .Permit(TransferTrigger.MoveComplete, TransferState.RunTipHandlingStateMachine)
                .Permit(TransferTrigger.MoveError, TransferState.MoveOutOfDestinationError)
                .OnEntry( () => MoveOutOfDestination());
            Configure(TransferState.MoveOutOfDestinationError)
                .Permit(TransferTrigger.Retry, TransferState.MoveOutOfDestination)
                .OnEntry( () => MoveOutOfDestinationError());
            Configure(TransferState.RunTipHandlingStateMachine)
                .Permit(TransferTrigger.MoveComplete, TransferState.Idle)
                .OnEntry( () => RunTipHandlingStateMachine());
        }

        public void Execute( AlphaHardware hardware, Stage source_stage, Stage dest_stage, Transfer transfer_info,
                             Channel tip, Teachpoints teachpoints, string tip_handling_method, ILabwareDatabase labware_database)
        {
            _hw = hardware;
            _source_stage = source_stage;
            _dest_stage = dest_stage;
            _transfer_info = transfer_info;
            _tip = tip;
            _teachpoints = teachpoints;
            _tip_handling_method = tip_handling_method;
            _labware_database = labware_database;

            // get the labware information cached
            try
            {
                _source_labware = _labware_database.GetLabware( _transfer_info.Source.LabwareName);
                _dest_labware = _labware_database.GetLabware( _transfer_info.Destination.LabwareName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            // set up wash motor settings
            // old pitch
            //_wash_settings = new MotionSettings(8, 1, 10);
            // new pitch
            _wash_settings = new MotionSettings(50, 2, 10);
            // Reed this only executes on the last chimney up Z
            // old pitch
            //_z_wash_settings = new MotionSettings(7, 0.5, 9);
            // new pitch
            _z_wash_settings = new MotionSettings(14, 1, 9);
            // this is only for the last w dispense move in the waste area
            // old pitch
            //_waste_w_settings = new MotionSettings(.7, 1, 10);
            // new pitch
            _waste_w_settings = new MotionSettings(2.8, 2, 10);

            // set up blended axes
            _xz = new BlendedAxes(new List<IAxis> { _tip.GetX(), _tip.GetZ() });

            Fire(TransferTrigger.ReceivedTipInfo);
        }

        private void Idle()
        {
            Debug.WriteLine( "Waiting for single tip transfer information");
        }

        private void Initialization()
        {
            Fire(TransferTrigger.Initialized);
        }

        private void MoveToSafeZ()
        {
            try {
                _tip.GetZ().MoveAbsolute(0);
                Fire(TransferTrigger.MoveComplete);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(TransferTrigger.MoveError);
            }
        }

        private void MoveToSafeZError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void MoveToSourceYR()
        {
            try {
                DateTime start = DateTime.Now;
                _hw.RequestStageForTransfer( _tip, _source_stage, false);
                Teachpoint tp = _source_stage.GetWellPosition(_tip.GetID(), _transfer_info.SourceWellName, _transfer_info.Source.NumberOfWells);
                _source_x = tp["x"];
                _source_y = tp["y"];
                _source_r = tp["r"];
                _source_stage.MoveAbsolute(_source_y, _source_r, true);
                Fire(TransferTrigger.MoveComplete);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(TransferTrigger.MoveError);
            }
        }

        private void MoveToSourceYRError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void MoveXZIntoSource()
        {
            try {
                // pre-aspirate
                _tip.GetW().MoveAbsolute(preaspirate_aspirate_mm);
                // start the blended move
                double tp_z = _teachpoints.GetStageTeachpoint(_tip.GetID(), _source_stage.GetID()).UpperLeft["z"];
                double well_bottom_z = _source_labware.Thickness - _source_labware.WellDepth;
                _xz.MoveAbsoluteBlended(_source_x, tp_z - well_bottom_z);
                // wait for the blended move to complete
                _xz.WaitForMoveAbsoluteComplete();
                Fire(TransferTrigger.MoveComplete);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(TransferTrigger.MoveError);
            }
        }

        private void MoveXZIntoSourceError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void Aspirate()
        {
            try {
                // this moves the W axis with datalogging enabled
                _tip.Aspirate( _transfer_info.TransferVolume + preaspirate_aspirate_mm);
                Fire(TransferTrigger.MoveComplete);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(TransferTrigger.MoveError);
            }
        }

        private void AspirateError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void MoveOutOfSource()
        {
            try {
                _tip.GetZ().MoveAbsolute(0);
                _hw.UnlockStage( _source_stage);
                Fire(TransferTrigger.MoveComplete);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(TransferTrigger.MoveError);
            }
        }

        private void MoveOutOfSourceError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void MoveToDestinationYR()
        {
            try {
                Teachpoint tp = _dest_stage.GetWellPosition(_tip.GetID(), _transfer_info.DestinationWellNames[0], _transfer_info.Destination.NumberOfWells);
                _dest_x = tp["x"];
                _dest_y = tp["y"];
                _dest_r = tp["r"];
                _hw.RequestStageForTransfer( _tip, _dest_stage, true);
                _dest_stage.MoveAbsolute(_dest_y, _dest_r, true);
                Fire(TransferTrigger.MoveComplete);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(TransferTrigger.MoveError);
            }
        }

        private void MoveToDestinationYRError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void MoveXZIntoDestination()
        {
            try {
                // move blended
                double tp_z = _teachpoints.GetStageTeachpoint(_tip.GetID(), _dest_stage.GetID()).UpperLeft["z"];
                double well_bottom_z = _dest_labware.Thickness - _dest_labware.WellDepth;
                _xz.MoveAbsoluteBlended(_dest_x, tp_z - well_bottom_z);
                _xz.WaitForMoveAbsoluteComplete();
                Fire(TransferTrigger.MoveComplete);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(TransferTrigger.MoveError);
            }
        }

        private void MoveXZIntoDestinationError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void Dispense()
        {
            try {
                // this moves the W axis with datalogging
                // NOTE: even though I passed in 0, Dispense ALWAYS moves to 0.  it is a known, temporary
                //       hack.
                _tip.Dispense(0);
                Debug.WriteLine(String.Format("Tip {0} aspirated from source {1} well {2} and dispensed into dest {3} well {4}", _tip.GetID(), _transfer_info.Source.Barcode, _transfer_info.SourceWellName, _transfer_info.Destination.Barcode, _transfer_info.DestinationWellNames[0]));
                Fire(TransferTrigger.MoveComplete);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(TransferTrigger.MoveError);
            }
        }

        private void DispenseError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void MoveOutOfDestination()
        {
            try {
                _tip.GetZ().MoveAbsolute(0);
                _hw.UnlockStage( _dest_stage);
                Fire(TransferTrigger.MoveComplete);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(TransferTrigger.MoveError);
            }
        }

        private void MoveOutOfDestinationError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void RunTipHandlingStateMachine()
        {
            //! \todo need to move this intelligence outside of the scheduler workflow
            //! \todo maybe create constructors for SimAxes and TSAxes that take an IAxis parameter
            IAxes X, Z, W;
            if (_hw.Simulating)
            {
                X = new SimAxes(new List<IAxis> { _tip.GetX() });
                Z = new SimAxes(new List<IAxis> { _tip.GetZ() });
                W = new SimAxes(new List<IAxis> { _tip.GetW() });
            }
            else
            {
                X = new TSAxes(new List<IAxis> { _tip.GetX() });
                Z = new TSAxes(new List<IAxis> { _tip.GetZ() });
                W = new TSAxes(new List<IAxis> { _tip.GetW() });
            }
            // for supporting blended moves
            List<BlendedAxes> xz = new List<BlendedAxes>();
            BlendedAxes ba = new BlendedAxes(new List<IAxis>(new IAxis[] { _tip.GetX(), _tip.GetZ() }));
            xz.Add(ba);

            try
            {
                if( _tip_handling_method == "Wash tip") {
                    WashStateMachine sm = new WashStateMachine();
                    sm.Execute( _teachpoints, new List<Channel> { _tip }, _hw, X, Z, W, xz);
                } else if( _tip_handling_method == "Change tip") {
                    TipShuckStateMachine sm = new TipShuckStateMachine( ErrorInterface);
                    sm.Execute( _teachpoints, new List<Channel> { _tip }, _hw, X, Z, W, xz);
                }
                else
                    return;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            Fire(TransferTrigger.MoveComplete);
        }

        /// <summary>
        /// Registers only the default "retry" behavior with the ErrorInterface.  It is assumed
        /// that this is getting called because of a servo drive fault, so it will reset
        /// faults and enable the axis before continuing.
        /// </summary>
        /// <param name="message"></param>
        private void HandleErrorWithRetryOnly(string message)
        {
            try {
                string retry = "Try move again";
                BioNex.Shared.LibraryInterfaces.ErrorData error = new BioNex.Shared.LibraryInterfaces.ErrorData(message, new List<string> { retry });
                ErrorInterface.AddError(error);
                WaitHandle.WaitAny(error.EventArray);
                if (error.TriggeredEvent == retry)
                    Fire(TransferTrigger.Retry);
            } catch( Exception ex) {
                LastError = ex.Message;
                Debug.Assert( false, ex.Message);
            }
        }
    }
}
