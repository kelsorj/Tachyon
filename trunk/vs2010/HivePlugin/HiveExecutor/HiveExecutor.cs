using System.Threading;
using BioNex.Hive.Hardware;
using BioNex.Shared.IError;
using BioNex.Shared.StateMachineExecutor;

namespace BioNex.Hive.Executor
{
    public class HiveExecutor
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public bool Busy { get { return RobotExecutor.Busy; }}
        public bool Running { get { return false; }}
        internal HiveHardware Hardware { get; private set; }
        private string Name { get; set; }
        public ErrorEventHandler HandleError { get; private set; }
        private StateMachineExecutor RobotExecutor { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public HiveExecutor( HiveHardware hardware, string name, ErrorEventHandler handle_error)
        {
            Hardware = hardware;
            Name = name;
            HandleError = handle_error;

            RobotExecutor = new StateMachineExecutor( this, name);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void Start()
        {
            RobotExecutor.Start();
        }
        // ----------------------------------------------------------------------
        public void AddStateMachine( IStateMachineExecutorStateMachine state_machine, ManualResetEvent state_machine_ended_or_aborted_event = null)
        {
            RobotExecutor.AddStateMachine( state_machine, state_machine_ended_or_aborted_event);
        }
    }
}
