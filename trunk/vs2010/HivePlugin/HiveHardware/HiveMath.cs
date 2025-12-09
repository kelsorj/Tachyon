using System;

namespace BioNex.Hive.Hardware
{
    public class HiveWorldPoint
    {
        public double X { get; set; }
        public double Z { get; set; }
        public double T { get; set; }
        public double G { get; set; } 
    }

    public class HiveToolPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double G { get; set; } 

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void ConvertFromHiveWorldPoint( HiveWorldPoint world_point, double arm_length, double finger_offset)
        {
            X = world_point.X;
            G = world_point.G;
            Tuple< double, double> yz_tool = HiveMath.ConvertTZWorldToYZTool( arm_length, finger_offset, world_point.T, world_point.Z);
            Y = yz_tool.Item1;
            Z = yz_tool.Item2;
        }
    }

    public static class HiveMath
    {
        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        /// <summary>
        /// this is used to get from the Zteachpoint to Zworld.
        /// </summary>
        /// <remarks>
        /// From here, we can add on gripper offset, clearance height, etc.
        /// </remarks>
        /// <param name="arm_length"></param>
        /// <param name="finger_offset"></param>
        /// <param name="z_tool"></param>
        /// <param name="y_tool"></param>
        /// <returns></returns>
        static public double ConvertZToolToWorldUsingY( double arm_length, double finger_offset, double z_tool, double y_tool)
        {
            return finger_offset + z_tool + Math.Sqrt( arm_length * arm_length - y_tool * y_tool);
        }
        // ----------------------------------------------------------------------
        static public double ConvertZToolToWorldUsingTheta( double arm_length, double finger_offset, double z_tool, double theta_tool)
        {
            return finger_offset + z_tool + ( arm_length * Math.Cos( Math.PI * theta_tool / 180.0));
        }
        // ----------------------------------------------------------------------
        static public double GetYFromTheta( double arm_length, double theta)
        {
            return arm_length * Math.Sin( theta * Math.PI / 180.0);
        }
        // ----------------------------------------------------------------------
        static public double GetThetaFromY( double arm_length, double y)
        {
            return 180.0 / Math.PI * Math.Asin(y / arm_length);
        }
        // ----------------------------------------------------------------------
        static public Tuple< double, double> ConvertTZWorldToYZTool( double arm_length, double finger_offset, double t_world, double z_world)
        {
            double y_tool = GetYFromTheta(arm_length, t_world);
            double z_for_y = Math.Sqrt( arm_length * arm_length - y_tool * y_tool);
            double z_tool = z_world - finger_offset - z_for_y;
            return Tuple.Create< double, double>( y_tool, z_tool);
        }
    }
}
