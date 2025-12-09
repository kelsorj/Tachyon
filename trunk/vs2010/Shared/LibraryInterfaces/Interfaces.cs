using System;

namespace BioNex.Shared.LibraryInterfaces
{
    /// <remarks>
    /// ITipBox used to "be-a" ILabware, but in the end this wouldn't work because I needed
    /// my TipBox to be able to call Labware's constructor that takes a DataRow.  This wouldn't
    /// be possible because you can't derive TipBox from ITipBox and Labware.  And as I thought
    /// about this, I figured it wasn't so bad to use containment, such that ILabware instances
    /// "have-a" ITipBoxProperties.
    /// </remarks>
    public class ITipBoxProperties
    {
        /// <summary>
        /// These parameters dictate how the tip press will work
        /// </summary>
        public class ITipPressParameters
        {
            // this is where the press should stop for a good seal
            // but it isn't really used at the moment -- we are still trying to figure out tip pressing
            public double ZNominal;
            // these are the motor parameters
            public double Velocity;
            public double Acceleration;public double Jerk;
            public double MinJerk;
            public int PressTimeMS;
            public double CurrentLimit;
            public readonly double IMaxPS;
            /// <summary>
            /// This is how far from the target position the current limit will kick in
            /// </summary>
            public double TorqueLimitDistanceMM;
            public double SealOffset;

            protected const double default_velocity = 366.0;
            protected const double default_acceleration = 732.0;
            protected const double default_jerk = 10.0;
            protected const double default_min_jerk = 5.0;
            protected const int default_press_time_ms = 250;
            protected const double default_current_limit = 1.5;
            protected const double default_imaxps = 6.11;
            protected const double default_torque_limit_distance_mm = 20;
            protected const double default_seal_offset = 10.6;

            public ITipPressParameters()
            {
                Velocity = default_velocity;
                Acceleration = default_acceleration;
                Jerk = default_jerk;
                MinJerk = default_min_jerk;
                PressTimeMS = default_press_time_ms;
                CurrentLimit = default_current_limit;
                IMaxPS = default_imaxps;
                TorqueLimitDistanceMM = default_torque_limit_distance_mm;
                SealOffset = default_seal_offset;
            }

            public ITipPressParameters( double v, double a, int press_time_ms, double current_limit, double torque_limit_distance_mm)
            {
                Velocity = v;
                Acceleration = a;
                Jerk = default_jerk;
                MinJerk = default_min_jerk;
                PressTimeMS = press_time_ms;
                CurrentLimit = current_limit;
                IMaxPS = default_imaxps;
                TorqueLimitDistanceMM = torque_limit_distance_mm;
                SealOffset = default_seal_offset;
            }

            public ITipPressParameters Clone()
            {
                return MemberwiseClone() as ITipPressParameters;
            }

            public override bool Equals(object obj)
            {
                ITipPressParameters other = obj as ITipPressParameters;
                if( other == null)
                    return false;

                return Velocity == other.Velocity 
                    && Acceleration == other.Acceleration 
                    && Jerk == other.Jerk 
                    && MinJerk == other.MinJerk 
                    && PressTimeMS == other.PressTimeMS 
                    && CurrentLimit == other.CurrentLimit 
                    && IMaxPS == other.IMaxPS 
                    && TorqueLimitDistanceMM == other.TorqueLimitDistanceMM 
                    && ZNominal == other.ZNominal 
                    && SealOffset == other.SealOffset;
            }
            public override int GetHashCode()
            {
                return base.GetHashCode(); // don't bother with hashcode, this class won't be used as a hash key
            }
        }

        public double TipLength { get; protected set; }
        public double Volume { get; protected set; }
        public double XOffset { get; protected set; }
        public double YOffset { get; protected set; }
        public double ZOffset { get; protected set; }
        public ITipPressParameters TipPress { get; set; }

        public ITipBoxProperties( double length, double volume, double x_offset, double y_offset,
                                  double z_press_offset_from_teachpoint, ITipPressParameters tip_press_parameters)
        {
            TipLength = length;
            Volume = volume;
            XOffset = x_offset;
            YOffset = y_offset;
            ZOffset = z_press_offset_from_teachpoint;
            TipPress = tip_press_parameters;
        }

        public ITipBoxProperties( ITipBoxProperties other)
        {
            TipLength = other.TipLength;
            Volume = other.Volume;
            XOffset = other.XOffset;
            YOffset = other.YOffset;
            ZOffset = other.ZOffset;
            TipPress = other.TipPress.Clone();
        }

        public ITipBoxProperties Clone()
        {
            ITipBoxProperties copy = (ITipBoxProperties)MemberwiseClone();
            copy.TipPress = TipPress.Clone();
            return copy;
        }

        public override bool Equals(object obj)
        {
            ITipBoxProperties other = obj as ITipBoxProperties;
            if( other == null)
                return false;

            return TipLength == other.TipLength 
                && Volume == other.Volume 
                && XOffset == other.XOffset 
                && YOffset == other.YOffset 
                && ZOffset == other.ZOffset 
                && TipPress.Equals( other.TipPress);
        }
        
        public override int GetHashCode()
        {
            return base.GetHashCode(); // don't bother with hashcode, this class won't be used as a hash key
        }

    }

    public interface ISystemSetupEditor
    {
        string Name { get; }
        void ShowTool();
    }

    public interface IErrorNotification
    {
        void SendNotification( string text1, string text2);
    }

    public interface ITipBoxManager
    {
        Tuple< string, string> AcquireTipBox();
        void ReleaseTipBox( Tuple< string, string> location);
    }

    public interface IReportsStatus
    {
        /// <summary>
        /// Allows caller to get string description of what's going on internally.  This is intended to be used
        /// for scheduling classes, like IRobotScheduler, IPlateScheduler, ChannelService, etc.
        /// </summary>
        /// <returns></returns>
        string GetStatus();
    }
}
