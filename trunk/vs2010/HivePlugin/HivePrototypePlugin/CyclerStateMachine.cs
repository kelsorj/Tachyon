using System;
using System.Collections.Generic;
using System.Threading;
using BioNex.Shared.Utils;
using BioNex.Shared.ThreadsafeMessenger;

namespace BioNex.HivePrototypePlugin
{
    public class CyclerStateMachine : StateMachineWrapper<CyclerStateMachine.State,CyclerStateMachine.Trigger>
    {
        /// <summary>
        /// This message is used to tell the cycler GUI that a plate has been moved from start to teachpoint.
        /// No data needs to be associated with the message since we're just incrementing a progressbar.
        /// </summary>
        public class PlateTransferComplete
        {
        }

        public enum State
        {
            Idle,
            GetNextTeachpoint,
            TransferFromStartToTeachpoint,
            TransferFromTeachpointToStart,
            Done,
        }

        public enum Trigger
        {
            Success,
            Abort,
            NoMoreTeachpoints,
            Stop,
        }

        private readonly HivePlugin _plugin;
        private readonly ThreadsafeMessenger _messenger;
        private readonly string _start_device_name;
        private readonly string _start_location;
        private readonly string _labware;
        //---- I could have used parameterized triggers, but think this is a more straightforward way to get arguments to the next state
        private string _to_device_name; // set in GetNextTeachpoint
        private string _to_location; // set in GetNextTeachpoint
        //-----
        private bool _portrait;
        private readonly List<Tuple<string,string>> _devices_and_teachpoints;
        private readonly IEnumerator<Tuple<string,string>> _iterator;

        // allow looping of cycle tester
        private readonly int _number_of_iterations;
        private int _current_iteration;

        private readonly AutoResetEvent _stop_event;

        public CyclerStateMachine( HivePlugin plugin, ThreadsafeMessenger messenger, string start_device_name, string start_location, string labware, bool portrait,
                                   List<Tuple<string, string>> devices_and_teachpoints, int NumberOfIterations)
            : base( State.Idle, Trigger.Success, Trigger.Abort, true)
        {
            _plugin = plugin;
            _start_device_name = start_device_name;
            _start_location = start_location;
            _labware = labware;
            _portrait = portrait;
            _messenger = messenger;
            _devices_and_teachpoints = devices_and_teachpoints;
            _iterator = _devices_and_teachpoints.GetEnumerator(); // now points at position before first item
            _stop_event = new AutoResetEvent( false);
            _number_of_iterations = NumberOfIterations;

            SM.Configure( State.Idle)
                .Permit( Trigger.Success, State.GetNextTeachpoint);
            SM.Configure( State.GetNextTeachpoint)
                .Permit( Trigger.Stop, State.Done)
                .Permit( Trigger.NoMoreTeachpoints, State.Done)
                .Permit( Trigger.Success, State.TransferFromStartToTeachpoint)
                .OnEntry( GetNextTeachpoint);
            SM.Configure( State.TransferFromStartToTeachpoint)
                .Permit( Trigger.Stop, State.Done)
                .Permit( Trigger.Success, State.TransferFromTeachpointToStart)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( TransferFromStartToTeachpoint);
            SM.Configure( State.TransferFromTeachpointToStart)
                .Permit( Trigger.Stop, State.Done)
                .Permit( Trigger.Success, State.GetNextTeachpoint)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( TransferFromTeachpointToStart);
            SM.Configure( State.Done)
                // it's possible that we transitioned here in the middle of another state
                // so ignore those triggers
                .Ignore( Trigger.Success)
                .Ignore( Trigger.NoMoreTeachpoints)
                .Ignore( Trigger.Abort)
                .Ignore( Trigger.Stop)
                .OnEntry( Done);
        }

        public void Stop()
        {
            _stop_event.Set();
        }

        private void GetNextTeachpoint()
        {
            // move the iterator to the next position and see if it's valid
            // if invalid, we're done with cycle testing!
            if( !_iterator.MoveNext()) {
                if (++_current_iteration < _number_of_iterations) {
                    _iterator.Reset();
                    _iterator.MoveNext();
                } else {
                    Fire(Trigger.NoMoreTeachpoints);
                    return;
                }
            }

            // check for stop
            if( _stop_event.WaitOne( 0)) {
                Fire( Trigger.Stop);
                return;
            }
            // otherwise, get new teachpoint data, set member variables, and continue
            _to_device_name = _iterator.Current.Item1;
            _to_location = _iterator.Current.Item2;
            Fire( Trigger.Success);
        }

        private void TransferFromStartToTeachpoint()
        {
            _plugin.TransferPlate(_start_device_name, _start_location, _to_device_name, _to_location, _labware, new MutableString());
            _messenger.Send<PlateTransferComplete>(new PlateTransferComplete());
            // check for stop
            if( _stop_event.WaitOne( 0)) {
                Fire( Trigger.Stop);
                return;
            }
            Fire( Trigger.Success);
        }

        private void TransferFromTeachpointToStart()
        {
            _plugin.TransferPlate(_to_device_name, _to_location, _start_device_name, _start_location, _labware, new MutableString());
            _messenger.Send<PlateTransferComplete>(new PlateTransferComplete());
            // check for stop
            if( _stop_event.WaitOne( 0)) {
                Fire( Trigger.Stop);
                return;
            }
            Fire( Trigger.Success);
        }

        private void Done()
        {
            base.EndStateFunction();
        }
    }
}
