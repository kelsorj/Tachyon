using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

namespace BioNex.HiveIntegration
{
    /// <summary>
    /// Implemented by both the remote / server (Synapsis) and client (VWorks, etc)
    /// </summary>
    public interface HiveCommands
    {
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.Ping")]
        void Ping();
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.Initialize")]
        void Initialize( string xml_parameters);
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.Close")]
        void Close();
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.UnloadPlate")]
        void UnloadPlate( string expected_barcode, string labware_name);
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.LoadPlate")]
        void LoadPlate( string expected_barcode, string labware_name);
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.HasBarcode")]
        bool HasBarcode( string barcode);
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.GetInventory")]
        string GetInventory();
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.ScanInventory")]
        void ScanInventory();
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.MovePlate")]
        void MovePlate( string barcode, string labware_name, string destination_group);
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.GetStatus")]
        int GetStatus();
        [XmlRpcMethod("BioNex.HiveIntegration.HiveCommands.PresentStage")]
        void PresentStage();
    }
}
