using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

// IGemsData interface served by JemsServer, BioNex test server (this interface will be served by Monsanto GEMS in final version)

// JemsServer is our simulated GEMS Server.  
// JemsServer project has its own Solution for generating a simulation executable
// Synapsis does not use a JemsServer object (it uses JemsClient)
// But this project is included in Synapsis because the IGEMSData interface is defined here


namespace BioNex.GemsRpc
{
    public interface IGemsData
    {
        #region MonsantoPhase2 methods

        [XmlRpcMethod("BioNex.GemsRpc.IGemsData.DestinationPlateComplete", Description = "DestinationPlateComplete method is called by Synapsis after a destination plate has been filled and place in a cart.  Parameters are hive_name, destination_barcode, an array of transfer information which maps destination wells to source wells")]
        void DestinationPlateComplete(string hive_name, string destination_barcode, TransferMap[] mapping);
        
        #endregion

        #region MonsantoPhase1 methods
        
        [XmlRpcMethod("BioNex.GemsRpc.IGemsData.ReinventoryComplete", Description="ReinventoryComplete method is called by Synapsis after an inventory is completed.  Parameters are hive_name, cart_name, and an array of barcodes")]
        void ReinventoryComplete( string hive_name, string cart_name, string[] barcodes);

        [XmlRpcMethod("BioNex.GemsRpc.IGemsData.Ping", Description="Ping method is used to verify connection without performing any action")]
        void Ping();
        
        #endregion
    }

    public struct TransferMap
    {
        public string source_barcode;
        public int source_row;
        public int source_column;
        public int destination_row;
        public int destination_column;
        public double transfer_volume;
    }
}
