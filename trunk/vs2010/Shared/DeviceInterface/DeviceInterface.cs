using System;
using System.Collections.Generic;
using System.Windows.Controls;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using DeviceManagerDatabase;

namespace BioNex.Shared.DeviceInterfaces
{
    public class BioNexAttributes
    {
        public const string CompanyName = "BioNex";
    }

    public static class BioNexDeviceNames
    {
        public const string Bumblebee = "Bumblebee";
        public const string Hive = "Hive";
        public const string PlateMover = "PlateMover";
        public const string IODevice= "IODevice";
        public const string HiG = "HiG";
        public const string BeeSure = "BeeSure";
    }

    public class CommandNotFoundException : ApplicationException
    {
        public string CommandName { get; private set; }
        public CommandNotFoundException( string command_name)
            : base( String.Format( "command '{0}' not found", command_name))
        {
            CommandName = command_name;
        }
    }

    public class InvalidCommandParameterException : ApplicationException
    {
        public string ParameterName { get; private set; }
        public InvalidCommandParameterException( string parameter_name)
            : base( String.Format( "parameter '{0}' is invalid", parameter_name))
        {
            ParameterName = parameter_name;
        }
    }

    public class MissingCommandParameterException : ApplicationException
    {
        public string ParameterName { get; private set; }
        public MissingCommandParameterException( string parameter_name)
            : base( String.Format( "parameter '{0}' is missing", parameter_name))
        {
            ParameterName = parameter_name;
        }
    }

    public class DeviceInitializationException : ApplicationException
    {
        public DeviceInitializationException( string initialization_error)
            : base( initialization_error)
        {
        }
    }

    /// <summary>
    /// used as a simple mechanism for notifying parts of a plugin that an abort command was issued by the applications
    /// </summary>
    public class AbortCommand {}
    public class PauseCommand {}
    public class ResumeCommand {}
    public class ResetInterlocksMessage {}
    public class SMAbortCommand {}
    public class UnhandledErrorCountMessage
    {
        public int NumberOfUnhandledErrors { get; private set; }

        public UnhandledErrorCountMessage( int num_unhandled_errors)
        {
            NumberOfUnhandledErrors = num_unhandled_errors;
        }
    }
    public class NumberOfTransferCompleteMessage
    {
        public int NumberOfTransfers { get; private set; }
        public NumberOfTransferCompleteMessage( int num_transfers) { NumberOfTransfers = num_transfers; }
    }
    public class TotalTransfersMessage
    {
        public int TotalTransfers { get; private set; }
        public TotalTransfersMessage( int total_transfers) { TotalTransfers = total_transfers; }
    }

    /// <summary>
    /// This is used to allow synapsis to request that all objects / devices reset their flags before
    /// running a protocol.  This could clear Aborting flags, or clear any hardware locks, for example.
    /// </summary>
    public class ResetCommand {}
    /// <summary>
    /// for triggering laser curtain safety via software
    /// </summary>
    public class SoftwareInterlockCommand {}

    public class ReturnPlateMessage
    {
        public string LabwareName { get; set; }
        public string Barcode { get; set; }
        public string TeachpointName { get; set; }
    }

    public class RequestPlateMessage
    {
        public string LabwareName { get; set; }
        public string Barcode { get; set; }
        public string TeachpointName { get; set; }
    }

    public interface IPluginIdentity
    {
        string Name { get; }
        /// <returns>a string that is a valid XML element name (i.e. no spaces, doesn't start with a number!)</returns>
        string ProductName { get; }
        string Manufacturer { get; }
        string Description { get; }

        // load device property data from database after MEF is done
        void SetProperties(DeviceInfo device_info);
    }

    public interface IHasDiagnosticPanel
    {
        UserControl GetDiagnosticsPanel();
        void ShowDiagnostics();
    }

    public interface GenericPlugin : IPluginIdentity, IHasDiagnosticPanel
    {}

    // some day we can make GenericPlugin the export / import interface instead of DeviceInterface

    public class JobCompleteEventArguments
    {
        public string PlateBarcode;
    }
    public delegate void JobCompleteEventHandler( object sender, JobCompleteEventArguments arg);

    public interface PlateSchedulerDeviceInterface
    {
        event JobCompleteEventHandler JobComplete;
        PlateLocation GetAvailableLocation( ActivePlate active_plate);
        bool ReserveLocation( PlateLocation location, ActivePlate active_plate);
        void LockPlace( PlatePlace place);
        void AddJob( ActivePlate active_plate);
        void EnqueueWorklist( Worklist worklist);
    }

    public interface DeviceInterface : GenericPlugin
    {
        bool Connected { get; }
        bool IsHomed { get; }
        void Connect();
        void Home();
        void Close();
        IEnumerable<string> GetCommands();
        bool ExecuteCommand( string command, IDictionary<string,object> parameters);
        void Abort();
        void Pause();
        void Resume();
        void Reset();
    }

    // interface for a device that contains plate locations
    public interface AccessibleDeviceInterface : DeviceInterface, RobotAccessibleInterface
    {}

    public interface ServicesDevice
    {
        void StartServices();
        void StopServices();
    }
}
