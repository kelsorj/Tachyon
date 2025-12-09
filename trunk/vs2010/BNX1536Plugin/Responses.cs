using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.BNX1536Plugin
{
    public partial class Controller
    {
        private Dictionary<string,string> ResponseDefinitions { get; set; }
        // this device responds with \n\r instead of \r\n like most devices I've used
        private readonly string ResponseDelimiter = "\n\r";

        private static class NormalResponses 
        {
            public const string Idle = ">OK0";
            public const string Ready = ">OK1";
            public const string CommandAck = ">OK2";
            public const string StopCommandAck = ">OK3";
            public const string Busy = ">BSY";
            public const string CommandComplete = ">END";
        }


        private void InitializeResponses()
        {
            ResponseDefinitions = new Dictionary<string,string>();
            ResponseDefinitions.Add( ">OK0", "Idle, no alarms, no pressure");
            ResponseDefinitions.Add( ">OK1", "Ready, no alarms, pressure OK");
            ResponseDefinitions.Add( ">OK2", "Command ACK");
            ResponseDefinitions.Add( ">OK3", "Stop command ACK");
            ResponseDefinitions.Add( ">BSY", "Busy, program is running");
            ResponseDefinitions.Add( ">END", "Program finished running");
            ResponseDefinitions.Add( ">E01", "Syntax error, command not recognized");
            ResponseDefinitions.Add( ">E02", "Invalid program number"); // this shouldn't ever happen since I validate the command string
            ResponseDefinitions.Add( ">E03", "Plate is missing");
            ResponseDefinitions.Add( ">E04", "Plate is too tall");
            ResponseDefinitions.Add( ">E11", "Liquid reservoir #1 is empty");
            ResponseDefinitions.Add( ">E12", "Liquid reservoir #2 is empty");
            ResponseDefinitions.Add( ">E13", "Liquid reservoir #3 is empty");
            ResponseDefinitions.Add( ">E14", "Liquid reservoir #4 is empty");
            ResponseDefinitions.Add( ">E15", "Waste reservoir is full");
            ResponseDefinitions.Add( ">E21", "Head #1 is missing, or the wrong type");
            ResponseDefinitions.Add( ">E22", "Head #2 is missing, or the wrong type");
            ResponseDefinitions.Add( ">E23", "Head #3 is missing, or the wrong type");
            ResponseDefinitions.Add( ">E24", "Head #4 is missing, or the wrong type");
            ResponseDefinitions.Add( ">E25", "Head #5 is missing, or the wrong type");
            ResponseDefinitions.Add( ">E31", "Head #1 pressure error");
            ResponseDefinitions.Add( ">E32", "Head #2 pressure error");
            ResponseDefinitions.Add( ">E33", "Head #3 pressure error");
            ResponseDefinitions.Add( ">E34", "Head #4 pressure error");
            ResponseDefinitions.Add( ">E35", "Program is paused, pressure is out of range");
            ResponseDefinitions.Add( ">E99", "Stop mode requested");
        }
    }
}
