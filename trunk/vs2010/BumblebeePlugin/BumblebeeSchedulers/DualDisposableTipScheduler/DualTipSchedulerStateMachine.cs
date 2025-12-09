using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DualDisposableTipSplitScheduler
{
    public class DualTipSchedulerStateMachine : Stateless.StateMachine< DualTipSchedulerStateMachine.State, DualTipSchedulerStateMachine.Trigger>
    {
        public enum State
        {
            Idle
        }

        public enum Trigger
        {

        }

        public DualTipSchedulerStateMachine()
            : base( DualTipSchedulerStateMachine.State.Idle)
        {
        }
    }
}
