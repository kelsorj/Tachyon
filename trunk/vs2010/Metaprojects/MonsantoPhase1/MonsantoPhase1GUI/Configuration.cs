using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.CustomerGUIPlugins
{
    public class Configuration
    {
        /// <summary>
        /// Name reported to GEMS server when a reinventory complete message is sent
        /// </summary>
        public string HiveName { get; set; }
        /// <summary>
        /// the url that the JEMS client uses to connect to the JEMS server
        /// </summary>
        public string JemsServerUrl { get; set; }
        public int JemsServerPort { get; set; }
        /// <summary>
        /// the port that the Monsanto plugin will listen on to get commands from JEMS
        /// </summary>
        public int JemsListenerPort { get; set; }
        /// <summary>
        /// the port that the Monsanto plugin will listen on to get IWorks commands from the Stokes node
        /// </summary>
        public int IWorksListenerPort { get; set; }
        /// <summary>
        /// the url that the stacker pingback client uses to connect to the stacker pingback
        /// the pingback server uses the same port number as the IWorksListenerPort to simplify configuration on the VWorks side
        /// </summary>
        public string VWorksServerUrl { get; set; }
        /// <summary>
        /// the port that the labware cloud server will listen on if this node is configured as the cloud master
        /// </summary>
        public int LabwareCloudListenerPort { get; set; }
        /// <summary>
        /// The host that is the labware cloud master.  If this is "localhost", then this node runs a server and acts as master
        /// </summary>
        public string LabwareCloudHost { get; set; }
        /// <summary>
        /// the name of the PlateMover device in the Synapsis Device Manager that corresponds to the
        /// plate mover that will move plates from the BioCel to the Hive
        /// </summary>
        public string UpstackingPlateMoverName { get; set; }
        /// <summary>
        /// the name of the robot that is going to pick up the plate from the UpstackingPlateMover
        /// </summary>
        public string UpstackingRobotName { get; set; }
        /// <summary>
        /// the name of the file used to convert barcodes to labware types -- See "DecodeLabwareFromBarcode" in MonsantoPhase1Gui.xaml.cs
        /// </summary>
        public string LabwareDecoderFileName { get; set; }
    }
}
