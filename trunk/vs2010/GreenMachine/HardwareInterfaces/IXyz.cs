using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.GreenMachine.HardwareInterfaces
{
    public abstract class IXyz
    {
        public enum Axes { X, Y, Z };

        protected class AxisInfo
        {
            public double PositionMM { get; set; }
            public bool Enabled { get; set; }
            public bool Homed { get; set; }
        }

        public abstract void Initialize();
        public abstract void Close();
        public abstract void MoveAbsolute( Axes axis, double position_mm, int velocity, int accel, int decel, bool wait_for_complete);
        public abstract void MoveRelative( Axes axis, double amount_mm, int velocity, int accel, int decel, bool wait_for_complete);
        public abstract void HomeAllAxes();
        public abstract void HomeAxis( Axes axis, bool blocking);
        public abstract double GetPositionMM( Axes axis);
        public abstract void EnableAxis( Axes axis, bool enable);
        public abstract bool IsAxisEnabled( Axes axis);
        public abstract void Stop();
        public abstract bool Homed();
    }
}
