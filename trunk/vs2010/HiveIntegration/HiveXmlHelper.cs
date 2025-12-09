using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("HiveIntegrationGUI")]
[assembly:InternalsVisibleTo("HiveIntegrationTestApp")]
[assembly:InternalsVisibleTo("HiveIntegrationUnitTests")]
namespace BioNex.HiveIntegration
{
    public static class HiveXmlHelper
    {
        #region Initialization
        [DataContract(Namespace="BioNex.HiveIntegration.HiveXmlHelper.InitializeParams")]
        internal class InitializeParams
        {
            [DataMember]
            public string IpAddress;
            [DataMember]
            public int Port;
            [DataMember]
            public int TimeoutSec;
        }

        /// <summary>
        /// Given the specified IP address, port, and homing timeout (in seconds), creates an XML string
        /// that the integration API can parse to get the initialization parameters.
        /// </summary>
        /// <param name="ip_address">the IP address of the Hive</param>
        /// <param name="port">which port the Hive is listening on for RPC commands</param>
        /// <param name="timeout_sec">defaults to 30, which is the shortest allowable timeout for the Hive</param>
        /// <returns></returns>
        public static string InitializeParamsToXml( string ip_address, int port, int timeout_sec=30)
        {
            System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer( typeof( InitializeParams));
            InitializeParams init = new InitializeParams { IpAddress = ip_address, Port = port };
            string xml;
            using( System.IO.MemoryStream ms = new System.IO.MemoryStream()) {
                serializer.WriteObject( ms, init);
                ms.Position = 0; // need to do this or the memorystream will just point at the end of the data that was just serialized
                using( System.IO.StreamReader sr = new System.IO.StreamReader( ms)) {
                    xml = sr.ReadToEnd();
                    sr.Close();
                }
                ms.Close();
            }
            return xml;
        }

        /// <summary>
        /// Used only by the RPC layer
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        internal static InitializeParams XmlToInitializeParams( string xml)
        {
            System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer( typeof( InitializeParams));
            using( System.IO.MemoryStream ms = new System.IO.MemoryStream( Encoding.ASCII.GetBytes( xml))) {
                InitializeParams init = (InitializeParams)serializer.ReadObject( ms);
                return init;
            }
        }
        #endregion

        #region Inventory
        [DataContract(Namespace="BioNex.HiveIntegration.HiveXmlHelper.InitializeParams")]
        internal class InventoryHelper
        {
            [DataMember]
            public Dictionary<string,List<string>> Inventory;

            public InventoryHelper( Dictionary<string,List<string>> inventory)
            {
                Inventory = inventory;
            }
        }

        public static string InventoryToXml( Dictionary<string,List<string>> inventory)
        {
            System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer( typeof( InventoryHelper));
            InventoryHelper ih = new InventoryHelper( inventory);
            string xml;
            using( System.IO.MemoryStream ms = new System.IO.MemoryStream()) {
                serializer.WriteObject( ms, ih);
                ms.Position = 0; // need to do this or the memorystream will just point at the end of the data that was just serialized
                using( System.IO.StreamReader sr = new System.IO.StreamReader( ms)) {
                    xml = sr.ReadToEnd();
                    sr.Close();
                }
                ms.Close();
            }
            return xml;
        }

        public static void XmlToInventory( string xml)
        {

        }
        #endregion
    }
}
