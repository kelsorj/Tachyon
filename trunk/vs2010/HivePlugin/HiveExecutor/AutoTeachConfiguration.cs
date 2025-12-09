using System.Collections.Generic;

namespace BioNex.Hive.Executor
{
    public class AutoTeachConfiguration
    {
        public class Origin
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }

        public class Panel
        {
            public string Name { get; set; }
            public Origin Origin { get; set; }
            public int RackCount { get; set; }
            public int SlotCount { get; set; }
            public double SlotXSpacing { get; set; }
            public double SlotZSpacing { get; set; }

            public int RackToSkipYAfter {get;set;}
        }

        public List<Panel> Panels { get; set; }

        // sensor info
        public int XSensorInputBitIndex { get; set; }
        public int YSensorInputBitIndex { get; set; }
        public int ZSensorInputBitIndex { get; set; }

        public bool XSensorEdgeTriggerState { get; set; }
        public bool YSensorEdgeTriggerState { get; set; }
        public bool ZSensorEdgeTriggerState { get; set; }

        public bool XSensorSeekPositive { get; set; }
        public bool YSensorSeekPositive { get; set; }
        public bool ZSensorSeekPositive { get; set; }

        // offsets needed for sensing in various directions
        /// <summary>
        /// After teaching Z, we need to raise up Z by some amount before looking for Y
        /// </summary>
        public double ZOffsetForTeachingY { get; set; }

        // determine if the system is doing calibration run
        public bool calibratejig { get; set; }
        public double calibration_step_size { get; set; }
        public double calibration_range { get; set; }

        // for first teach
        public double XJigOffsetForTeachingZ { get; set; }
        public double YJigOffsetForTeachingZ { get; set; }
        public double ZJigOffsetForTeachingZ { get; set; }
        public double XSensedOffsetForTeachingZ { get; set; }
        public double YSensedOffsetForTeachingZ { get; set; }
        /// <summary>
        /// After teaching Z, we need to move X over by some amount before looking for Y
        /// </summary>
        public double XOffsetForTeachingY { get; set; }
        public double YOffsetForTeachingY { get; set; }
        /// <summary>
        /// After teaching Y, we need to move Z by some amount before looking for X
        /// </summary>
        public double ZOffsetForTeachingX { get; set; }
        /// <summary>
        /// After teaching Y, we need to move Y back by some amount before looking for X
        /// </summary>
        public double YOffsetForTeachingX { get; set; }
        public double XOffsetForTeachingX { get; set; }

        public double XOffsetForFinalZ {get;set;}
        public double YOffsetForFinalZ {get;set;}
        public double ZOffsetForFinalZ {get;set;}

        public double XSensorConstTerm { get; set; }
        public double YSensorConstTerm { get; set; }
        public double ZSensorConstTerm { get; set; }     
        public double ZSensorFirstOrderTerm { get; set; }   // Z Sensor xform polynomial is with respect to Y sensed values
        public double ZSensorSecondOrderTerm { get; set; }  // Z Sensor xform polynomial is with respect to Y sensed values

        public string AutoTeachFileName { get; set; }

        public AutoTeachConfiguration()
        {
            Panels = new List<Panel>();
        }
    }
}
