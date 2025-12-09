using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace BioNex.HivePrototypePlugin
{
    //[Export, DataContract]
    /// <summary>
    /// I had a bunch of issues trying to use the built-in XmlSerializer / DataContractSerializer classes.  So
    /// I ended up using XmlSerializer for now, using public properties, but kept these details private to
    /// the Configuration class.
    /// </summary>
    [Export]
    public class Configuration
    {
        private ConfigurationSerializer _configuration_serializer { get; set; }

        public double ThetaSafe { get { return _configuration_serializer.ThetaSafe; }}
        public double ThetaCamStart { get { return _configuration_serializer.ThetaCamStart; }}
        public double ZCamTableRange { get { return _configuration_serializer.ZCamTableRange; }}
        public double ZCamTableOffset { get { return _configuration_serializer.ZCamTableOffset; }}
        public UInt16 CamTableStep128FuncOfZ { get { return _configuration_serializer.CamTableStep128FuncOfZ; }}
        public int ZCamTjerkMin { get { return _configuration_serializer.ZCamTjerkMin; }}
        public double ZCamAccel { get { return _configuration_serializer.ZCamAccel; }}
        public double GripOpenDelta { get { return _configuration_serializer.GripOpenDelta; }}
        public double TCamX { get { return _configuration_serializer.TCamX; }}
        public double Config.ArmLength { get { return _configuration_serializer.Config.ArmLength; }}
        public double Config.MaxY { get { return _configuration_serializer.Config.MaxY; }}
        public double Config.FingerOffsetZ { get { return _configuration_serializer.Config.FingerOffsetZ; }}
        public double PlateOffsetZ { get { return _configuration_serializer.PlateOffsetZ; }}

        public Configuration()
        {
            try {
                _configuration_serializer = new ConfigurationSerializer();
            } catch( Exception ex) {
                throw;
            }
        }

        // Note: XmlSerializer does not support read-only autoproperties.  So you have
        //       to back the public property with a private one.
        /*
        [DataMember(Name="ThetaSafe")]
        private double _theta_safe;
        public double ThetaSafe { get { return _theta_safe; }}
        [DataMember(Name="ThetaCamStart")]
        private double _theta_cam_start;
        public double ThetaCamStart { get { return _theta_cam_start; }}
        [DataMember(Name="ZCamTableRange")]
        private double _z_cam_table_range;
        public double ZCamTableRange { get { return _z_cam_table_range; }}
        [DataMember(Name="ZCamTableOffset")]
        private double _Config.ZCamTableOffset;
        public double ZCamTableOffset { get { return _Config.ZCamTableOffset; }}
        [DataMember(Name="CamTableStep128FuncOfZ")]
        private UInt16 _Config.CamTableStep128FuncOfZ;
        public UInt16 CamTableStep128FuncOfZ { get { return _Config.CamTableStep128FuncOfZ; }}
        [DataMember(Name="ZCamTjerkMin")]
        private int _Config.ZCamTjerkMin;
        public int ZCamTjerkMin { get { return _Config.ZCamTjerkMin; }}
        [DataMember(Name="ZCamAccel")]
        private double _Config.ZCamAccel;
        public double ZCamAccel { get { return _Config.ZCamAccel; }}
        [DataMember(Name="GripOpenDelta")]
        private double _grip_open_delta;
        public double GripOpenDelta { get { return _grip_open_delta; }}
        [DataMember(Name="TCamX")]
        private double _t_camx;
        public double TCamX { get { return _t_camx; }}

        // Arm geometry constants
        [DataMember(Name="Config.ArmLength")]
        private double _arm_length;
        public double Config.ArmLength { get { return _arm_length; }}
        [DataMember(Name="Config.MaxY")]
        private double _max_y;
        public double Config.MaxY { get { return _max_y; }}
        [DataMember(Name="Config.FingerOffsetZ")]
        private double _finger_offset_z;
        public double Config.FingerOffsetZ { get { return _finger_offset_z; }}
        [DataMember(Name="PlateOffsetZ")]
        private double _plate_offset_z;
        public double PlateOffsetZ { get { return _plate_offset_z; }}
        */

        /*
        [DataMember]
        public double ThetaSafe { get; private set; }
        [DataMember]
        public double ThetaCamStart { get; private set; }
        [DataMember]
        public double ZCamTableRange { get; private set; }
        [DataMember]
        public double ZCamTableOffset { get; private set; }
        [DataMember]
        public UInt16 CamTableStep128FuncOfZ { get; private set; }
        [DataMember]
        public int ZCamTjerkMin { get; private set; }
        [DataMember]
        public double ZCamAccel { get; private set; }
        [DataMember]
        public double GripOpenDelta { get; private set; }
        [DataMember]
        public double TCamX { get; private set; }

        // Arm geometry constants
        [DataMember]
        public double Config.ArmLength { get; private set; }
        [DataMember]
        public double Config.MaxY { get; private set; }
        [DataMember]
        public double Config.FingerOffsetZ { get; private set; }
        [DataMember]
        public double PlateOffsetZ { get; private set; }
        */

        public class ConfigurationSerializer
        {
            public double ThetaSafe { get; set; }
            public double ThetaCamStart { get; set; }
            public double ZCamTableRange { get; set; }
            public double ZCamTableOffset { get; set; }
            public UInt16 CamTableStep128FuncOfZ { get; set; }
            public int ZCamTjerkMin { get; set; }
            public double ZCamAccel { get; set; }
            public double GripOpenDelta { get; set; }
            public double TCamX { get; set; }
            public double Config.ArmLength { get; set; }
            public double Config.MaxY { get; set; }
            public double Config.FingerOffsetZ { get; set; }
            public double PlateOffsetZ { get; set; }

            public ConfigurationSerializer()
            {
                XmlSerializer serializer = new XmlSerializer(this.GetType());
                string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
                FileStream reader = new FileStream( exe_path + "\\config.xml", FileMode.Open);
                ConfigurationSerializer temp = (ConfigurationSerializer)serializer.Deserialize( reader);
                Clone( temp);
                reader.Close();
            }

            private void Clone( ConfigurationSerializer other)
            {
                this.ThetaSafe = other.ThetaSafe;
                this.ThetaCamStart = other.ThetaCamStart;
                this.ZCamTableRange = other.ZCamTableRange;
                this.ZCamTableOffset = other.ZCamTableOffset;
                this.CamTableStep128FuncOfZ = other.CamTableStep128FuncOfZ;
                this.ZCamTjerkMin = other.ZCamTjerkMin;
                this.ZCamAccel = other.ZCamAccel;
                this.GripOpenDelta = other.GripOpenDelta;
                this.TCamX = other.TCamX;
                this.Config.ArmLength = other.Config.ArmLength;
                this.Config.MaxY = other.Config.MaxY;
                this.Config.FingerOffsetZ = other.Config.FingerOffsetZ;
                this.PlateOffsetZ = other.PlateOffsetZ;
            }
        }

        /*
        public Configuration()
        {
            _theta_safe = -14.9; // degree position for tucked/safe position
            _theta_cam_start = 5.3; // degree position for CAM table start
            _z_cam_table_range = 250.0; // 250.0; // 229.553; // mm for length of z move for entire cam table
            _Config.ZCamTableOffset = 1.7909504; // offset for padding the cam table (512 IU)
            _Config.CamTableStep128FuncOfZ = 0x0B8A; // location of step128_func_of_z camtable in Theta drive
            _Config.ZCamTjerkMin = 100; // minimum tjerk value while camming (does not depend on speed or accel)
            _Config.ZCamAccel = 2000.0; // hardcoded accel value for z axis while camming. speed control will cut this down.
            _grip_open_delta = 10.0; // relative mm for gripper to open to let go of labware
            _t_camx = 2.0; // CAMX parameter on slave - Multiplies Master's APOS with CAMX before looking it up in CAM table
            _arm_length = 250.0; // mm
            _max_y = Config.ArmLength - 0.1; // mm
            _finger_offset_z = 0; // mm, should be 28 but I am debugging
            _plate_offset_z = 0; // mm, should be 105 but I am debugging
        }
         */
    }
}
