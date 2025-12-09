using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.PlateDefs;

namespace BioNex.BumblebeeAlphaGUI.DualDisposableTipSplitScheduler
{
    public class TransferAssignmentStateMachine 
        : StateMachineWrapper<TransferAssignmentStateMachine.State,TransferAssignmentStateMachine.Trigger>
    {
        public enum State
        {
            Idle
        }

        public enum Trigger
        {
            Exit
        }

        public TransferAssignmentStateMachine()
            : base(State.Idle, Trigger.Exit)
        {

        }

        public void Execute( List<Transfer> all_transfers)
        {

        }
    }
}
