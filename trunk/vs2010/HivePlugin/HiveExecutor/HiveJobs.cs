using System;
using System.Threading;
using BioNex.Hive.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;

namespace BioNex.Hive.Executor
{
    // --------------------------------------------------------------------------
    // base HiveJob.
    // --------------------------------------------------------------------------
    public abstract class HiveJob
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected HiveExecutor Executor { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        protected HiveJob( HiveExecutor executor)
        {
            Executor = executor;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public abstract void Dispatch();
    }

    // --------------------------------------------------------------------------
    // base DoActionJob.
    // --------------------------------------------------------------------------
    public abstract class DoActionJob : HiveJob
    {
        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        private Action Action { get; set; }
        private ManualResetEvent ActionEndedOrAbortedEvent { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        protected DoActionJob( HiveExecutor executor, Action action, ManualResetEvent action_ended_or_aborted_event = null)
            : base( executor)
        {
            Action = action;
            ActionEndedOrAbortedEvent = action_ended_or_aborted_event;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public override void Dispatch()
        {
            DoActionStateMachine state_machine = new DoActionStateMachine( Executor, Action);
            Executor.AddStateMachine( state_machine, ActionEndedOrAbortedEvent);
        }

    }

    // --------------------------------------------------------------------------
    // Homing Jobs.
    // --------------------------------------------------------------------------
    public class HomeXJob : DoActionJob
    {
        public HomeXJob( HiveExecutor executor) : base( executor, new Action( () => executor.Hardware.HomeX( true))){}
    }

    public class HomeZJob : DoActionJob
    {
        public HomeZJob( HiveExecutor executor) : base( executor, new Action( () => executor.Hardware.HomeZ( true))){}
    }

    public class HomeTJob : DoActionJob
    {
        public HomeTJob( HiveExecutor executor) : base( executor, new Action( () => executor.Hardware.HomeT( true))){}
    }

    public class HomeGJob : DoActionJob
    {
        public HomeGJob( HiveExecutor executor) : base( executor, new Action( () => executor.Hardware.HomeG( true))){}
    }

    public class HomeAllAxesJob : HiveJob
    {
        public HomeAllAxesJob( HiveExecutor executor) : base( executor){}
        public override void Dispatch()
        {
            HomeStateMachine home_state_machine = new HomeStateMachine( Executor, null);
            Executor.AddStateMachine(home_state_machine);
        }
    }

    // --------------------------------------------------------------------------
    // Relative-Move Jobs.
    // --------------------------------------------------------------------------
    public class JogXJob : DoActionJob
    {
        public JogXJob( HiveExecutor executor, double increment, bool positive) : base( executor, new Action( () => executor.Hardware.JogX( increment, positive))){}
    }

    public class JogZJob : DoActionJob
    {
        public JogZJob( HiveExecutor executor, double increment, bool positive) : base( executor, new Action( () => executor.Hardware.JogZ( increment, positive))){}
    }

    public class JogTJob : DoActionJob
    {
        public JogTJob( HiveExecutor executor, double increment, bool positive) : base( executor, new Action( () => executor.Hardware.JogT( increment, positive))){}
    }

    public class JogGJob : DoActionJob
    {
        public JogGJob( HiveExecutor executor, double increment, bool positive) : base( executor, new Action( () => executor.Hardware.JogG( increment, positive))){}
    }

    public class JogYJob : DoActionJob
    {
        public JogYJob( HiveExecutor executor, double increment, bool positive) : base( executor, new Action( () => executor.Hardware.JogY( increment, positive))){}
    }

    // --------------------------------------------------------------------------
    // Absolute-Move Jobs.
    // --------------------------------------------------------------------------
    public class MoveXJob : DoActionJob
    {
        public MoveXJob( HiveExecutor executor, double position) : base( executor, new Action( () => executor.Hardware.MoveX( position))){}
    }

    public class MoveZJob : DoActionJob
    {
        public MoveZJob( HiveExecutor executor, double position) : base( executor, new Action( () => executor.Hardware.MoveZ( position))){}
    }

    public class MoveTJob : DoActionJob
    {
        public MoveTJob( HiveExecutor executor, double position) : base( executor, new Action( () => executor.Hardware.MoveT( position))){}
    }

    public class MoveGJob : DoActionJob
    {
        public MoveGJob( HiveExecutor executor, double position) : base( executor, new Action( () => executor.Hardware.MoveG( position))){}
    }

    public class MoveYJob : DoActionJob
    {
        public MoveYJob( HiveExecutor executor, double theta_position) : base( executor, new Action( () => executor.Hardware.MoveY( theta_position))){}
    }

    // --------------------------------------------------------------------------
    // Gross-Move and Plate-Transfer-Move Jobs.
    // --------------------------------------------------------------------------
    public class TuckYJob : DoActionJob
    {
        public TuckYJob( HiveExecutor executor) : base( executor, new Action( () => executor.Hardware.TuckY())){}
    }

    public class TuckToXZJob : DoActionJob
    {
        public TuckToXZJob( HiveExecutor executor, double position_x, double position_z, bool use_tool_space, ManualResetEvent tuck_to_xz_ended_or_aborted_event) : base( executor, new Action( () => executor.Hardware.TuckToXZ( position_x, position_z, use_tool_space)), tuck_to_xz_ended_or_aborted_event){}
    }

    public class ParkJob : DoActionJob
    {
        public ParkJob( HiveExecutor executor) : base( executor, new Action( () => executor.Hardware.Park())){}
    }

    public class PickAndOrPlaceJob : HiveJob
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum PickAndOrPlaceJobOption
        {
            MoveToTeachpointWithoutPlate,
            MoveToTeachpointWithPlate,
            PickOnly,
            PlaceOnly,
            PickAndPlace,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private ManualResetEvent EndedAbortedEvent { get; set; }
        private PickAndOrPlaceJobOption Option { get; set; }

        // shared between pick and place:
        private ILabware Labware { get; set; }
        private MutableString ExpectedBarcode { get; set; }

        // pick only properties:
        private AccessibleDeviceInterface FromDevice { get; set; }
        private HiveTeachpoint FromTeachpoint { get; set; }
        private double AdditionalPickZOffset { get; set; }
        private bool NoRetractBeforePick { get; set; }

        // place only properties:
        private HiveTeachpoint ToTeachpoint { get; set; }
        private double AdditionalPlaceZOffset { get; set; }
        private bool NoRetractAfterPlace { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public PickAndOrPlaceJob( HiveExecutor executor)
            : base( executor)
        {
            throw new Exception( "placeholder to assist compiling.");
        }
        // ----------------------------------------------------------------------
        public PickAndOrPlaceJob( HiveExecutor executor, ManualResetEvent ended_aborted_event, PickAndOrPlaceJobOption option, AccessibleDeviceInterface from_device, HiveTeachpoint from_teachpoint, ILabware labware, MutableString expected_barcode, double additional_pick_z_offset = 0.0)
            : base( executor)
        {
            EndedAbortedEvent = ended_aborted_event;
            if( option != PickAndOrPlaceJobOption.MoveToTeachpointWithoutPlate && option != PickAndOrPlaceJobOption.PickOnly){
                throw new Exception( "this constructor does not accept this option");
            }
            Option = option;
            FromDevice = from_device;
            FromTeachpoint = from_teachpoint;
            Labware = labware;
            ExpectedBarcode = expected_barcode;
            AdditionalPickZOffset = additional_pick_z_offset;
        }
        // ----------------------------------------------------------------------
        public PickAndOrPlaceJob( HiveExecutor executor, ManualResetEvent ended_aborted_event, PickAndOrPlaceJobOption option, HiveTeachpoint to_teachpoint, ILabware labware, double additional_place_z_offset = 0.0)
            : base( executor)
        {
            EndedAbortedEvent = ended_aborted_event;
            if( option != PickAndOrPlaceJobOption.MoveToTeachpointWithPlate && option != PickAndOrPlaceJobOption.PlaceOnly){
                throw new Exception( "this constructor does not accept this option");
            }
            Option = option;
            ToTeachpoint = to_teachpoint;
            Labware = labware;
            AdditionalPlaceZOffset = additional_place_z_offset;
        }
        // ----------------------------------------------------------------------
        public PickAndOrPlaceJob( HiveExecutor executor, ManualResetEvent ended_aborted_event, PickAndOrPlaceJobOption option, ILabware labware, MutableString expected_barcode, AccessibleDeviceInterface from_device, HiveTeachpoint from_teachpoint, HiveTeachpoint to_teachpoint, double additional_z_offset = 0.0)
            : base( executor)
        {
            EndedAbortedEvent = ended_aborted_event;
            if( option != PickAndOrPlaceJobOption.PickAndPlace){
                throw new Exception( "this constructor does not accept this option");
            }
            Option = option;
            Labware = labware;
            ExpectedBarcode = expected_barcode;
            FromDevice = from_device;
            FromTeachpoint = from_teachpoint;
            AdditionalPickZOffset = additional_z_offset;
            ToTeachpoint = to_teachpoint;
            AdditionalPlaceZOffset = additional_z_offset;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public override void Dispatch()
        {
            switch( Option){
                case PickAndOrPlaceJobOption.MoveToTeachpointWithoutPlate:
                    Executor.AddStateMachine( new PickStateMachine( Executor, EndedAbortedEvent, FromDevice, FromTeachpoint, Labware, ExpectedBarcode, AdditionalPickZOffset, PickStateMachine.BlendedPickOption.BPOTerminateAtTeachpoint));
                    break;
                case PickAndOrPlaceJobOption.MoveToTeachpointWithPlate:
                    Executor.AddStateMachine( new PlaceStateMachine( Executor, EndedAbortedEvent, ToTeachpoint, Labware, AdditionalPlaceZOffset, PlaceStateMachine.BlendedPlaceOption.BPOTerminateAtTeachpoint));
                    break;
                case PickAndOrPlaceJobOption.PickOnly:
                    Executor.AddStateMachine( new PickStateMachine( Executor, EndedAbortedEvent, FromDevice, FromTeachpoint, Labware, ExpectedBarcode, AdditionalPickZOffset));
                    break;
                case PickAndOrPlaceJobOption.PlaceOnly:
                    Executor.AddStateMachine( new PlaceStateMachine( Executor, EndedAbortedEvent, ToTeachpoint, Labware, AdditionalPlaceZOffset));
                    break;
                case PickAndOrPlaceJobOption.PickAndPlace:
                    {
                        PlaceStateMachine place_state_machine = new PlaceStateMachine( Executor, EndedAbortedEvent, ToTeachpoint, Labware, AdditionalPlaceZOffset);
                        PickStateMachine pick_state_machine = new PickStateMachine( Executor, null, FromDevice, FromTeachpoint, Labware, ExpectedBarcode, AdditionalPickZOffset, PickStateMachine.BlendedPickOption.BPONormal, place_state_machine);
                        Executor.AddStateMachine( pick_state_machine);
                        Executor.AddStateMachine( place_state_machine);
                    }
                    break;
            }
        }
    }

    // --------------------------------------------------------------------------
    // to be deprecated.
    // --------------------------------------------------------------------------
    public class EnableAllAxesJob : DoActionJob
    {
        public EnableAllAxesJob( HiveExecutor executor) : base( executor, new Action( () => executor.Hardware.EnableAllAxes())){}
    }

    public class DisableAllAxesJob : DoActionJob
    {
        public DisableAllAxesJob( HiveExecutor executor) : base( executor, new Action( () => executor.Hardware.DisableAllAxes())){}
    }

    public class EnableAxisJob : DoActionJob
    {
        public EnableAxisJob( HiveExecutor executor, byte axis_id) : base( executor, new Action( () => executor.Hardware.EnableAxis( axis_id))){}
    }

    public class DisableAxisJob : DoActionJob
    {
        public DisableAxisJob( HiveExecutor executor, byte axis_id) : base( executor, new Action( () => executor.Hardware.DisableAxis( axis_id))){}
    }
}
