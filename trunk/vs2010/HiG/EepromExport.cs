using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Hig
{
    /// <summary>
    /// Simple class used to export EEPROM settings to an XML file.  No plans to allow users to import settings.
    /// </summary>
    public class EepromExport
    {
        public class EepromKeyValuePair
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public List<EepromKeyValuePair> Settings { get; set; }

        public EepromExport()
        {
            Settings = new List<EepromKeyValuePair>();
        }
    }
}
