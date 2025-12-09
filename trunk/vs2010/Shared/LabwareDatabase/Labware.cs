using System;
using System.Collections.Generic;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;

namespace BioNex.Shared.LabwareDatabase
{
    [DatabaseTable( "labwares")]
    public class Labware : ILabware
    {
        // database-specific properties
        [DatabaseColumn( "id", DatabaseColumnType.INTEGER, DatabaseColumnFlags.PRIMARY_KEY | DatabaseColumnFlags.AUTOINCREMENT)]
        public long Id { get; set; }
        [DatabaseColumn( "name", DatabaseColumnType.TEXT, DatabaseColumnFlags.UNIQUE)]
        public string Name { get; set; }
        [DatabaseColumn( "notes", DatabaseColumnType.TEXT)]
        public string Notes { get; set; }
        [DatabaseColumn( "tags", DatabaseColumnType.TEXT)]
        public string Tags { get; set; }
        [DatabaseColumn( "lid_id", DatabaseColumnType.INTEGER)]
        public long LidId { get; internal set; }

        public Dictionary<string, object> Properties { get; internal set; }

        public Labware()
        {
            Properties = new Dictionary<string,object>();
            Notes = "";
            Tags = "";
            LidId = 0;
        }

        public Labware( string name, string notes, string tags)
            : this()
        {
            Name = name;
            Notes = notes;
            Tags = tags;
        }

        public Labware( Labware other)
            : this( other.Name, other.Notes, other.Tags)
        {
            Properties = other.Properties;
        }

        public Labware( Labware other, string new_name)
            : this( new_name, other.Notes, other.Tags)
        {
            Properties = other.Properties;
        }

        public Labware( string name, int num_wells, double thickness, double well_depth)
            : this( name, "", "")
        {
            Properties = new Dictionary<string,object>();
            Properties.Add( LabwarePropertyNames.NumberOfWells, num_wells);
            Properties.Add( LabwarePropertyNames.Thickness, thickness);
            Properties.Add( LabwarePropertyNames.WellDepth, well_depth);
        }

        public object this[ string property_name]
        {
            get { 
                if( Properties.ContainsKey( property_name))
                    return Properties[property_name];
                else
                    return null;
            }
            set {
                if( Properties.ContainsKey( property_name))
                    Properties[property_name] = value;
                else
                    Properties.Add( property_name, value);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class TipBox : Labware
    {
        public TipBox()
            : base()
        {
            TipProperties = new TipProperties();
        }

        public TipBox( string name, string notes, string tags)
            : base( name, notes, tags)
        {
        }

        public TipBox( Labware labware, TipProperties tip_property)
            : base( labware)
        {
            Id = labware.Id;
            TipProperties = tip_property;
        }

        public TipProperties TipProperties { get; set; }
    }

    [DatabaseTable( "tip_properties", "FOREIGN KEY (labware_id) REFERENCES labwares(id) ON DELETE CASCADE")]
    public class TipProperties : ITipProperties
    {
        [DatabaseColumn( "labware_id", DatabaseColumnType.INTEGER, DatabaseColumnFlags.PRIMARY_KEY)]
        public long LabwareId { get; set; }
        [DatabaseColumn( "length", DatabaseColumnType.FLOAT)]
        public double LengthInMm { get; set; }
        [DatabaseColumn( "volume", DatabaseColumnType.FLOAT)]
        public double VolumeInUl { get; set; }
        [DatabaseColumn( "x_offset", DatabaseColumnType.FLOAT)]
        public double XOffset { get; set; } // not needed?
        [DatabaseColumn( "y_offset", DatabaseColumnType.FLOAT)]
        public double YOffset { get; set; } // not needed?
        [DatabaseColumn( "press_time_ms", DatabaseColumnType.INTEGER)]
        public long PressTimeMs { get; set; }
        [DatabaseColumn( "press_velocity", DatabaseColumnType.FLOAT)]
        public double PressVelocity { get; set; }
        [DatabaseColumn( "press_acceleration", DatabaseColumnType.FLOAT)]
        public double PressAcceleration { get; set; }
        [DatabaseColumn( "press_torque_percentage", DatabaseColumnType.FLOAT)]
        public double PressTorquePercentage { get; set; }
        [DatabaseColumn( "press_start_torque_position", DatabaseColumnType.FLOAT)]
        public double PressStartTorquePosition { get; set; }
        [DatabaseColumn( "press_target_position", DatabaseColumnType.FLOAT)]
        public double PressTargetPosition { get; set; }
        [DatabaseColumn( "press_min_acceptable_position", DatabaseColumnType.FLOAT)]
        public double PressMinAcceptablePosition { get; set; }
        [DatabaseColumn( "press_max_acceptable_position", DatabaseColumnType.FLOAT)]
        public double PressMaxAcceptablePosition { get; set; }
        [DatabaseColumn( "current_limit", DatabaseColumnType.FLOAT)]
        public double CurrentLimit { get; set; }
        [DatabaseColumn( "seal_offset", DatabaseColumnType.FLOAT)]
        public double SealOffset { get; set; }
    }

    [DatabaseTable( "speed_settings")]
    public class SpeedSetting : ISpeedSetting
    {
        [DatabaseColumn( "labware_id", DatabaseColumnType.INTEGER)]
        public long LabwareId { get; set; }
        [DatabaseColumn( "fill_percentage", DatabaseColumnType.FLOAT)]
        public double FillPercentage { get; set; }
        [DatabaseColumn( "speed", DatabaseColumnType.INTEGER)]
        public long Speed { get; set; }
    }

    [DatabaseTable( "labware_property_values", "CONSTRAINT [labware_property] UNIQUE (labware_id,labware_property_id)")]
    public class LabwarePropertyValue : ILabwarePropertyValue
    {
        [DatabaseColumn( "labware_id", DatabaseColumnType.INTEGER)]
        public long LabwareId { get; set; }
        [DatabaseColumn( "labware_property_id", DatabaseColumnType.INTEGER)]
        public long PropertyId { get; set; }
        [DatabaseColumn( "labware_property_value", DatabaseColumnType.TEXT)]
        public string PropertyValue { get; set; }
    }

    [DatabaseTable( "labware_properties")]
    public class LabwareProperty : ILabwareProperty
    {
        [DatabaseColumn( "id", DatabaseColumnType.INTEGER, DatabaseColumnFlags.PRIMARY_KEY | DatabaseColumnFlags.AUTOINCREMENT)]
        public long Id { get; set; }
        [DatabaseColumn( "module_id", DatabaseColumnType.INTEGER)]
        public long ModuleId { get; set; }
        [DatabaseColumn( "name", DatabaseColumnType.TEXT)]
        public string Name { get; set; }
        [DatabaseColumn( "type", DatabaseColumnType.INTEGER)]
        //public LabwarePropertyType Type { get; set; }
        public Int64 Type { get; set; }
        [DatabaseColumn( "display_order", DatabaseColumnType.INTEGER)]
        public long DisplayOrder { get; set; }

        public LabwareProperty()
            : this( "", (Int64)LabwarePropertyType.STRING, 0, 0)
        {
        }

        public LabwareProperty( string name, Int64 /*LabwarePropertyType*/ type, int module_id, int display_order)
        {
            Name = name;
            Type = type;
            ModuleId = module_id;
            DisplayOrder = display_order;
        }
    }

    [DatabaseTable("labware_timestamps")]
    public class LabwareTimestamps : ILabwareTimeStamps
    {
        [DatabaseColumn("name", DatabaseColumnType.TEXT)]
        public string Name { get; set; }
        [DatabaseColumn("time_stamp", DatabaseColumnType.DATETIME)]
        public DateTime TimeStamp { get; set; }
    }
}
