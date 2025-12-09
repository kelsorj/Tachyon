using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.LibraryInterfaces
{
    public enum LabwarePropertyType
    {
        INTEGER,
        STRING,
        DOUBLE,
        BOOL
    }
 
    public static class LabwarePropertyNames
    {
        public static string Thickness = "Thickness";
        public static string WellDepth = "Well depth";
        public static string NumberOfWells = "Number of wells";
        public static string RowSpacing = "Row spacing";
        public static string ColumnSpacing = "Column spacing";
        public static string GripperOffset = "Gripper offset";
        public static string MinPortraitGripperPos = "Min portrait gripper pos";
        public static string MaxPortraitGripperPos = "Max portrait gripper pos";
        public static string MinLandscapeGripperPos = "Min landscape gripper pos";
        public static string MaxLandscapeGripperPos = "Max landscape gripper pos";
        public static string GripperTorque = "Gripper torque";
        public static string MaxAllowableVolume = "Max allowable volume";
        public static string WellShape = "Well shape";
        public static string WellBottomShape = "Well bottom shape";
        public static string SkirtHeight = "Skirt height";
        public static string EmptyMass = "Empty mass";
        public static string BarcodeSides = "Side(s) with a barcode";
        public static string LidOffset = "Lid offset";
        public static string WellRadius = "Well radius";  //optional, for beesure
        public static string NumberOfRows = "Number of rows"; // optional, for beesure
        public static string NumberOfColumns = "Number of columns"; // optional, for beesure
    }

    public class LabwareException : ApplicationException
    {
        public LabwareException() {}

        public LabwareException( string message)
            : base( message)
        {
        }
    }

    public class DuplicateLabwareException : LabwareException
    {
        public string LabwareName { get; private set; }
        public DuplicateLabwareException( string labware_name)
        {
            LabwareName = labware_name;
        }
    }

    public class ReservedLabwareException : LabwareException
    {
        public string LabwareName { get; private set; }
        public ReservedLabwareException( string labware_name)
            : base( "The labware '" + labware_name + "' is reserved.  Please try a different name.")
        {
            LabwareName = labware_name;
        }
    }

    public class LabwareNotFoundException : LabwareException
    {
        public string LabwareName { get; private set; }
        public LabwareNotFoundException( string name)
            : base( "The labware '" + name + "' is not in the labware database.")
        {
            LabwareName = name;
        }
    }

    public class IllegalLabwarePropertyNameException : LabwareException
    {
        public string PropertyName { get; private set; }
        public IllegalLabwarePropertyNameException( string name)
            : base( "The property '" + name + "' is not allowed, because it is either blank or is a reserved name")
        {
            PropertyName = name;
        }
    }

    public class DuplicateLabwarePropertyException : LabwareException
    {
        public string PropertyName { get; private set; }
        public DuplicateLabwarePropertyException( string name)
            : base( "The property '" + name + "' is in the labware database multiple times.  Please delete one of the duplicates first, then try again.")
        {
            PropertyName = name; 
        }
    }

    public class UnknownLabwarePropertyTypeException : LabwareException
    {
        public string PropertyName { get; private set; }
        public UnknownLabwarePropertyTypeException( string property_name)
            : base( "The property '" + property_name + "' has an unknown type, so it cannot be used.")
        {
            PropertyName = property_name;
        }
    }

    public interface ILabwareDatabase
    {
        bool IsValidLabwareName( string labware_name);
        List<string> GetLabwareNames();
        List<string> GetTipBoxNames();
        ILabware GetLabware( string labware_name);
        ILabware GetLabware( long labware_id);
        List<ILabwareProperty> GetLabwareProperties();
        void ShowEditor();
        void ReloadLabware();
        long UpdateLabware( ILabware labware);
        void UpdateLabwareNotes( ILabware labware, string notes);
        long AddLabware( ILabware labware);
        long CloneLabware( ILabware labware, string new_name);
        void DeleteLabware( string labware_name);
        void RenameLabware( string old_name, string new_name);
        long AddLid( ILabware parent_plate);

        // Cloud Sync / Publish
        DateTime GetLastSyncTime();
        void SetLastSyncTime(DateTime time);
        DateTime GetLastModifiedTime();
        void SetLastModifiedTime(DateTime time);

        // Notifications to clients
        event EventHandler LabwareChanged;
    }

    public interface ILabware
    {
        long Id { get; }
        string Name { get; }
        string Notes { get; set; }
        string Tags { get; set; }
        long LidId { get; }
        object this[string property_name] { get; }
        Dictionary<string, object> Properties { get; }
    }

    public interface ILabwarePropertyValue
    {
        long LabwareId { get; set; }
        long PropertyId { get; set; }
        string PropertyValue { get; set; }
    }

    public interface ISpeedSetting
    {
        long LabwareId { get; set; }
        double FillPercentage { get; set; }
        long Speed { get; set; }
    }

    public interface ILabwareProperty
    {
        long Id { get; set; }
        long ModuleId { get; set; }
        string Name { get; set; }
        // DKM 2010-08-12 need to make this a LabwarePropertyType, but I am having
        //                issues with the reflection code that assigns this value from the database
        //LabwarePropertyType Type { get; set; }
        Int64 Type { get; set; }
        long DisplayOrder { get; set; }
    }

    public interface ITipProperties
    {
        long LabwareId { get; set; }
        double LengthInMm { get; set; }
        double VolumeInUl { get; set; }
        double XOffset { get; set; } // not needed?
        double YOffset { get; set; } // not needed?
        long PressTimeMs { get; set; }
        double PressVelocity { get; set; }
        double PressAcceleration { get; set; }
        double PressTorquePercentage { get; set; }
        double PressStartTorquePosition { get; set; }
        double PressTargetPosition { get; set; }
        double PressMinAcceptablePosition { get; set; }
        double PressMaxAcceptablePosition { get; set; }
        double CurrentLimit { get; set; }
    }

    public interface ILabwareTimeStamps    
    {
        string Name { get; set; }
        DateTime TimeStamp { get; set; }
    }
}
