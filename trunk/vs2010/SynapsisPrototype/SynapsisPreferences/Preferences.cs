using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace BioNex.SynapsisPrototype
{
    public class Preferences
    {
        public Preferences()
        {
            PreferenceProperties = new List<PreferenceProperty>();
        }

        public List<PreferenceProperty> PreferenceProperties { get; set; }

        public class PreferenceProperty
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Type { get; set; }
            public string Description { get; set; }
        }
    }

    public static class PreferenceStrings
    {
        public const string LowBatteryThresholdPercentage = "Low battery threshold percentage";
        public const string PasswordProtectDiagnostics = "Password protect diagnostics";
        public const string LightIODevice = "Christmas Tree I/O device name";
        public const string PreProtocolMessageFilename = "Pre-protocol message filename";
        public const string ReturnTipBoxToOriginalLocation = "Return tip box to original location";
        public const string SelectedCustomerGui = "Selected customer GUI";
        public const string RobotDisableResetBit = "Robot disable reset bit";
        public const string RunForever = "Run forever";
    }
}
