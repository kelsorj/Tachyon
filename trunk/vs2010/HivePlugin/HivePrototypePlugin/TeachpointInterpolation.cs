using System.Collections.Generic;
using System.Linq;
using BioNex.Hive.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.Utils;
using CookComputing.XmlRpc;

namespace BioNex.HivePrototypePlugin
{
    public class InterpolatedTeachpointInfo
    {
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double ApproachHeight { get; set; }
        public HiveTeachpoint.TeachpointOrientation Orientation { get; set; }

        public InterpolatedTeachpointInfo()
        { }

        public InterpolatedTeachpointInfo(InterpolatedTeachpointInfo tp)
        {
            Name = tp.Name;
            X = tp.X;
            Y = tp.Y;
            Z = tp.Z;
            ApproachHeight = tp.ApproachHeight;
            Orientation = tp.Orientation;
        }

        public InterpolatedTeachpointInfo( HiveTeachpoint tp)
        {
            Name = tp.Name;
            X = tp.X;
            Y = tp.Y;
            Z = tp.Z;
            ApproachHeight = tp.ApproachHeight;
            Orientation = tp.Orientation;
        }

        public static explicit operator HiveTeachpoint(InterpolatedTeachpointInfo info)
        {
            return new HiveTeachpoint( info.Name, info.X, info.Y, info.Z, info.ApproachHeight, info.Orientation);
        }
    }

    class TeachpointInterpolation
    {
        /// <summary>
        /// Local version, returns list of interpolated teachpoints from a local device
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="device_name"></param>
        /// <param name="dockable_device_barcode"></param>
        /// <param name="top_left"></param>
        /// <param name="bottom_left"></param>
        /// <param name="bottom_right"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static List< InterpolatedTeachpointInfo> GetInterpolatedTeachpoints( HivePlugin controller, string device_name, string dockable_device_barcode, string top_left, string bottom_left, string bottom_right, int rows, int columns)
        {
            if (device_name == "" || top_left == "" || bottom_left == "")
                return new List< InterpolatedTeachpointInfo>(); // can't be done

            // get teachpoint information
            HiveTeachpoint top_teachpoint;
            HiveTeachpoint bottom_left_teachpoint;
            HiveTeachpoint bottom_right_teachpoint;

            try{
                bool dockable = controller.DataRequestInterface.Value.GetDockablePlateStorageInterfaces().Where( (x) => (x as DeviceInterface).Name == device_name).FirstOrDefault() != null;
                // FYC -- hive reorg new code to test:
                controller.Hardware.LoadTeachpoints( controller.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == device_name));
                top_teachpoint = controller.Hardware.GetTeachpoint( device_name, top_left);
                bottom_left_teachpoint = controller.Hardware.GetTeachpoint( device_name, bottom_left);
                bottom_right_teachpoint = bottom_right == "" ? bottom_left_teachpoint : controller.Hardware.GetTeachpoint( device_name, bottom_right);
            } catch( TeachpointNotFoundException){
                return new List< InterpolatedTeachpointInfo>(); // can't be done, invalid teachpoint names
            }
            int first_rack = 1;
            int first_slot = 1;
            top_left.GetLastTwoNumbers( ref first_rack, ref first_slot);
            return GetInterpolatedTeachpoints( top_teachpoint, bottom_left_teachpoint, bottom_right_teachpoint, rows, columns, first_rack, first_slot);
        }

        /// <summary>
        /// Remote version, returns list of teachpoints from remote device
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="device_name"></param>
        /// <param name="dockable_device_barcode"></param>
        /// <param name="top_left"></param>
        /// <param name="bottom_left"></param>
        /// <param name="bottom_right"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static List< InterpolatedTeachpointInfo> GetInterpolatedTeachpoints( TeachpointXmlRpcClient proxy, string device_name, string dockable_device_barcode, string top_left, string bottom_left, string bottom_right, int rows, int columns)
        {
            if (device_name == "" || top_left == "" || bottom_left == "")
                return new List< InterpolatedTeachpointInfo>(); // can't be done

            // get teachpoint information
            HiveTeachpoint top_teachpoint;
            HiveTeachpoint bottom_left_teachpoint;
            HiveTeachpoint bottom_right_teachpoint;

            try
            {
                top_teachpoint = proxy.GetTeachpoint( device_name, dockable_device_barcode, top_left);
                bottom_left_teachpoint = proxy.GetTeachpoint( device_name, dockable_device_barcode, bottom_left);
                bottom_right_teachpoint = bottom_right == "" ? bottom_left_teachpoint : proxy.GetTeachpoint( device_name, dockable_device_barcode, bottom_right);
            }
            catch (XmlRpcFaultException)
            {
                return new List< InterpolatedTeachpointInfo>(); // Can't be done, invalid teachpoint names
            }
            int first_rack = 1;
            int first_slot = 1;
            top_left.GetLastTwoNumbers(ref first_rack, ref first_slot);

            return GetInterpolatedTeachpoints( top_teachpoint, bottom_left_teachpoint, bottom_right_teachpoint, rows, columns, first_rack, first_slot);
        }

        static List< InterpolatedTeachpointInfo> GetInterpolatedTeachpoints( HiveTeachpoint top_teachpoint, HiveTeachpoint bottom_left_teachpoint, HiveTeachpoint bottom_right_teachpoint, int rows, int columns, int first_rack, int first_slot)
        {
            var teachpoints = new List< InterpolatedTeachpointInfo>();

            double bottom_right_x = bottom_right_teachpoint.X;
            double bottom_right_y = bottom_right_teachpoint.Y;
            double bottom_right_z = bottom_right_teachpoint.Z;
            double top_x = top_teachpoint.X;
            double top_y = top_teachpoint.Y;
            double top_z = top_teachpoint.Z;
            double bottom_x = bottom_left_teachpoint.X;
            double bottom_y = bottom_left_teachpoint.Y;
            double bottom_z = bottom_left_teachpoint.Z;

            double dx = rows > 1 ? (bottom_x - top_x) / (rows - 1) : 0;
            double dy = rows > 1 ? (bottom_y - top_y) / (rows - 1) : 0;
            double dz = rows > 1 ? (bottom_z - top_z) / (rows - 1) : 0;

            double ddx = columns > 1 ? (bottom_right_x - bottom_x) / (columns - 1) : 0;
            double ddy = columns > 1 ? (bottom_right_y - bottom_y) / (columns - 1) : 0;
            double ddz = columns > 1 ? (bottom_right_z - bottom_z) / (columns - 1) : 0;

            for (int x = 0; x < columns; ++x)
            {
                top_x = top_teachpoint.X + x * ddx;
                top_y = top_teachpoint.Y + x * ddy;
                top_z = top_teachpoint.Z + x * ddz;
                bottom_x = bottom_left_teachpoint.X + x * ddx;
                bottom_y = bottom_left_teachpoint.Y + x * ddy;
                bottom_z = bottom_left_teachpoint.Z + x * ddz;

                for (int y = 0; y < rows; ++y)
                {
                    // create the new teachpointinfo w/ name
                    InterpolatedTeachpointInfo point = new InterpolatedTeachpointInfo();
                    point.Name = top_teachpoint.Name.ReplaceLastTwoNumbersWith(first_rack + x, first_slot + y);
                    point.X = top_x + (y * dx);
                    point.Y = top_y + (y * dy);
                    point.Z = top_z + (y * dz);
                    point.ApproachHeight = top_teachpoint.ApproachHeight;
                    point.Orientation = top_teachpoint.Orientation;
                    teachpoints.Add(point);
                }
            }
            return teachpoints;
        }
    }
}
/*
       private void BilinearInterpolate()
        {
            if (BottomTeachpointView.CurrentItem == null || TopTeachpointView.CurrentItem == null || BottomRightTeachpointView.CurrentItem == null)
                return;

            InterpolatedTeachpoints.Clear();
            // get teachpoint information
            string bottom_left_teachpoint_name = BottomTeachpointView.CurrentItem.ToString();
            string bottom_right_teachpoint_name = BottomRightTeachpointView.CurrentItem.ToString();
            string top_teachpoint_name = TopTeachpointView.CurrentItem.ToString();
            Teachpoint bottom_left_teachpoint = _controller.GetTeachpoint(DeviceName, bottom_left_teachpoint_name);
            Teachpoint bottom_right_teachpoint = _controller.GetTeachpoint(DeviceName, bottom_right_teachpoint_name);
            Teachpoint top_teachpoint = _controller.GetTeachpoint(DeviceName, top_teachpoint_name);

            int first_rack = 1;
            int first_slot = 1;
            top_teachpoint_name.GetLastTwoNumbers(ref first_rack, ref first_slot);

            double bottom_right_x = bottom_right_teachpoint["x"];
            double bottom_right_y = bottom_right_teachpoint["y"];
            double bottom_right_z = bottom_right_teachpoint["z"];
            double top_x = top_teachpoint["x"];
            double top_y = top_teachpoint["y"];
            double top_z = top_teachpoint["z"];
            double bottom_x = bottom_left_teachpoint["x"];
            double bottom_y = bottom_left_teachpoint["y"];
            double bottom_z = bottom_left_teachpoint["z"];

            double dx = (bottom_x - top_x) / (NumberOfShelves - 1);
            double dy = (bottom_y - top_y) / (NumberOfShelves - 1);
            double dz = (bottom_z - top_z) / (NumberOfShelves - 1);

            double ddx = (bottom_right_x - bottom_x) / (NumberOfRacks - 1);
            double ddy = (bottom_right_y - bottom_y) / (NumberOfRacks - 1);
            double ddz = (bottom_right_z - bottom_z) / (NumberOfRacks - 1);

            for (int x = 0; x < NumberOfRacks; ++x)
            {
                top_x = top_teachpoint["x"] + x * ddx;
                top_y = top_teachpoint["y"] + x * ddy;
                top_z = top_teachpoint["z"] + x * ddz;
                bottom_x = bottom_left_teachpoint["x"] + x * ddx;
                bottom_y = bottom_left_teachpoint["y"] + x * ddy;
                bottom_z = bottom_left_teachpoint["z"] + x * ddz;

                for (int y = 0; y < NumberOfShelves; ++y)
                {
                    // create the new teachpointinfo w/ name
                    InterpolatedTeachpointInfo point = new InterpolatedTeachpointInfo();
                    point.Name = top_teachpoint.Name.ReplaceLastTwoNumbersWith(first_rack + x, first_slot + y);
                    point.X = top_x + (y * dx);
                    point.Y = top_y + (y * dy);
                    point.Z = top_z + (y * dz);
                    point.ApproachHeight = top_teachpoint["approach_height"];
                    InterpolatedTeachpoints.Add(point);
                }
            }
        }*/