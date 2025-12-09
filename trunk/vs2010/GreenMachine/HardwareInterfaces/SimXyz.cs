using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;

namespace BioNex.GreenMachine.HardwareInterfaces
{
    public class SimXyz : IXyz
    {
        private AxisInfo[] _axis_info;
        private bool _homed;

        public override void Initialize()
        {
            _axis_info = new AxisInfo[3];
            for( int i=0; i<3; i++)
                _axis_info[i] = new AxisInfo();

            _homed = false;
        }

        public override void Close()
        {
            
        }

        public override void MoveAbsolute(IXyz.Axes axis, double position_mm, int velocity, int accel, int decel, bool wait_for_complete)
        {
            _axis_info[(int)axis].PositionMM = position_mm;
        }

        public override void MoveRelative(IXyz.Axes axis, double amount_mm, int velocity, int accel, int decel, bool wait_for_complete)
        {
            _axis_info[(int)axis].PositionMM += amount_mm;
        }

        public override void HomeAllAxes()
        {
            foreach( var x in _axis_info)
                x.PositionMM = 0;

            _homed = true;
        }

        public override void HomeAxis(IXyz.Axes axis, bool blocking)
        {
            _axis_info[(int)axis].PositionMM = 0;
        }

        public override double GetPositionMM( Axes axis)
        {
            return _axis_info[(int)axis].PositionMM;
        }

        public override void EnableAxis( Axes axis, bool enable)
        {
            _axis_info[(int)axis].Enabled = enable;
        }

        public override bool IsAxisEnabled( Axes axis)
        {
            return _axis_info[(int)axis].Enabled;
        }

        public override void Stop()
        {
        }

        public override bool Homed()
        {
            return _homed;
        }
    }
}
