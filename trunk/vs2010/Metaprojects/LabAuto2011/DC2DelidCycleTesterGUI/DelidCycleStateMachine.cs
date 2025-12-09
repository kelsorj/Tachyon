using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.DC2DelidCycleTesterGUI
{
    public class DelidCycleStateMachine : StateMachineWrapper2<DelidCycleStateMachine.State, DelidCycleStateMachine.Trigger>
    {
        public enum State 
        { 
            Idle,
            GetPlate,
            RemoveLid,
            ReplaceLid,
            ExercisePlateMover,
            ReplacePlate,
            Done
        };
        public enum Trigger 
        { 
            Success,
            Fail,
            Abort,
            Retry,
            Ignore
        };

        HivePrototypePlugin.HivePlugin _robot;
        DeviceInterface _bumble;
        ILabware _labware;
        ILabware _lid;
        int _rack;
        int _slot;

        byte _stage;
        string _golden_name;

        public DelidCycleStateMachine(DeviceInterface robot, DeviceInterface bumble, ILabware labware, ILabware lid, string golden_name, int rack, int slot) 
            : base(typeof(DelidCycleStateMachine), State.Idle, State.Done, State.Done, Trigger.Success, Trigger.Fail, Trigger.Retry, Trigger.Ignore, Trigger.Abort, null, true)
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Success, State.GetPlate);
            SM.Configure(State.GetPlate)
                .Permit(Trigger.Success, State.RemoveLid)
                .OnEntry(GetPlateFunction);
            SM.Configure(State.RemoveLid)
                .Permit(Trigger.Success, State.ExercisePlateMover)
                .OnEntry(RemoveLidFunction);
            SM.Configure(State.ExercisePlateMover)
                .Permit(Trigger.Success, State.ReplaceLid)
                .OnEntry(ExercisePlateMoverFunction);
            SM.Configure(State.ReplaceLid)
                .Permit(Trigger.Success, State.ReplacePlate)
                .OnEntry(ReplaceLidFunction);
            SM.Configure(State.ReplacePlate)
                .Permit(Trigger.Success, State.Done)
                .OnEntry(ReplacePlateFunction);
            SM.Configure(State.Done).OnEntry(EndStateFunction);

            _robot = (HivePrototypePlugin.HivePlugin)robot;
            _bumble = bumble;
            _labware = labware;
            _lid = lid;
            _rack = rack;
            _slot = slot;
            _golden_name = golden_name;

            _stage = 1; // hard code stage id for now
        }

        private void GetPlateFunction() 
        {
            // move stage to robot teachpoint
            _bumble.ExecuteCommand(BumblebeePlugin.Bumblebee.MoveStageToRobotTeachpoint, 
                new Dictionary<string, object>() { { BumblebeePlugin.Bumblebee.MoveStageToRobotTeachpointParameters.Stage, _stage } });

            // pick and place from rack & slot to stage

            var tp_name = new HivePrototypePlugin.PlateLocation(_rack, _slot).ToString();
            _robot.Pick(_robot.Name, tp_name,_labware.Name, true, new MutableString());
            _robot.Place(_bumble.Name, String.Format("BB PM {0}", _stage), _labware.Name, true, "");
        }

        private void RemoveLidFunction() 
        {
            _robot.Pick(_bumble.Name, String.Format("BB PM {0}", _stage), _lid.Name, true, new MutableString());
            _robot.Place(_robot.Name, _golden_name, _lid.Name, true, "");
        }

        private void ExercisePlateMoverFunction() 
        {
            // move stage to robot teachpoint
            _bumble.ExecuteCommand(BumblebeePlugin.Bumblebee.MoveStageToYR,
                new Dictionary<string, object>() { 
                { BumblebeePlugin.Bumblebee.MoveStageToYRParameters.Stage, (byte)(_stage-1) }, 
                { BumblebeePlugin.Bumblebee.MoveStageToYRParameters.Y, 150.0 }, 
                { BumblebeePlugin.Bumblebee.MoveStageToYRParameters.R, 270.0 } 
                });

            // move stage to robot teachpoint
            _bumble.ExecuteCommand(BumblebeePlugin.Bumblebee.MoveStageToYR,
                new Dictionary<string, object>() { 
                { BumblebeePlugin.Bumblebee.MoveStageToYRParameters.Stage, (byte)(_stage-1) }, 
                { BumblebeePlugin.Bumblebee.MoveStageToYRParameters.Y, 300.0 }, 
                { BumblebeePlugin.Bumblebee.MoveStageToYRParameters.R, 90.0 } 
                });

            // move stage to robot teachpoint
            _bumble.ExecuteCommand(BumblebeePlugin.Bumblebee.MoveStageToYR,
                new Dictionary<string, object>() { 
                { BumblebeePlugin.Bumblebee.MoveStageToYRParameters.Stage, (byte)(_stage-1) }, 
                { BumblebeePlugin.Bumblebee.MoveStageToYRParameters.Y, 250.0 }, 
                { BumblebeePlugin.Bumblebee.MoveStageToYRParameters.R, 270.0 } 
                });
        }

        private void ReplaceLidFunction() 
        {
            // move stage to robot teachpoint
            _bumble.ExecuteCommand(BumblebeePlugin.Bumblebee.MoveStageToRobotTeachpoint, 
                new Dictionary<string, object>() { { BumblebeePlugin.Bumblebee.MoveStageToRobotTeachpointParameters.Stage, _stage } });

            _robot.Pick(_robot.Name, _golden_name, _lid.Name, true, new MutableString());
            _robot.Place(_bumble.Name, String.Format("BB PM {0}", _stage), _lid.Name, true, "");
        }

        private void ReplacePlateFunction() 
        {
            var tp_name = new HivePrototypePlugin.PlateLocation(_rack, _slot).ToString();
            _robot.Pick(_bumble.Name, String.Format("BB PM {0}", _stage), _labware.Name, true, new MutableString());
            _robot.Place(_robot.Name, tp_name, _labware.Name, true, "");
        }
    }
}
