using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Exceptions
{
    /// <summary>
    /// thrown when a parameter is passed to a device function that is invalid or out-of-range
    /// </summary>
    public class InvalidArgumentException : Exception
    {
        public string ParameterName { get; private set; }
        public string PassedValue { get; private set; }
        public string MinValue { get; private set; }
        public string MaxValue { get; private set; }
        public string AdditionalInfo { get; private set; }

        public InvalidArgumentException( string parameter_name, string value, string min_value, string max_value)
        {
            Init( parameter_name, value, min_value, max_value);
            AdditionalInfo = "";
        }

        public InvalidArgumentException( string parameter_name, string value, string min_value, string max_value, string additional_info)
        {
            Init( parameter_name, value, min_value, max_value);
            AdditionalInfo = additional_info;
        }

        private void Init( string parameter_name, string value, string min_value, string max_value)
        {
            ParameterName = parameter_name;
            PassedValue = value;
            MinValue = min_value;
            MaxValue = max_value;
        }
    }

    public class DeviceConnectionException : Exception
    {
        public DeviceConnectionException( string device_name, string message)
            : base( String.Format( "Could not connect to device '{0}': {1}", device_name, message))
        {
        }
    }

    /// <summary>
    /// thrown when a device is busy and is asked to process another command, but it can't
    /// </summary>
    public class DeviceBusyForCommandException : Exception
    {
        public string RequestedCommand { get; private set; }
        public string CurrentCommand { get; private set; }
        public TimeSpan EstimatedTimeToCompletion { get; private set; }

        public DeviceBusyForCommandException( string requested_command)
        {
            RequestedCommand = requested_command;
            CurrentCommand = "";
            EstimatedTimeToCompletion = new TimeSpan();
        }

        public DeviceBusyForCommandException( string requested_command, string current_command)
        {
            RequestedCommand = requested_command;
            CurrentCommand = current_command;
            EstimatedTimeToCompletion = new TimeSpan();
        }

        public DeviceBusyForCommandException( string requested_command, string current_command, TimeSpan estimated_time_to_completion)
        {
            RequestedCommand = requested_command;
            CurrentCommand = current_command;
            EstimatedTimeToCompletion = estimated_time_to_completion;
        }
    }

    public class ResponseTimeoutException : Exception
    {
        public string CommandName { get; private set; }
        public double TimeoutMS { get; private set; }
        public string AdditionalInfo { get; private set; }

        public ResponseTimeoutException( string command_name, int timeout_ms, string additional_info)
        {
            CommandName = command_name;
            TimeoutMS = timeout_ms;
            AdditionalInfo = additional_info;
        }
    }

    public class CommandException : Exception
    {
        public String CommandName { get; private set; }
        public String AdditionalInfo { get; private set; }

        public CommandException( String command_name, String additional_info)
        {
            CommandName = command_name;
            AdditionalInfo = additional_info;
        }
    }

    public class BarcodeException : Exception
    {
        public BarcodeException( string message) : base(message) {}
    }
}
