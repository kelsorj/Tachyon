using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.HiveIntegration
{
    public class Configuration
    {
        /// <summary>
        /// Name reported to RPC client when asked
        /// </summary>
        public string HiveName { get; set; }
        /// <summary>
        /// the port to listen on to get RPC commands
        /// </summary>
        public int ListenerPort { get; set; }
        /// <summary>
        /// the name of the Hive robot device in the Device Manager
        /// </summary>
        public string RobotDeviceName { get; set; }
        /// <summary>
        /// the name of the PlateMover device in the Device Manager
        /// </summary>
        public string PlateMoverDeviceName { get; set; }
    }
}
