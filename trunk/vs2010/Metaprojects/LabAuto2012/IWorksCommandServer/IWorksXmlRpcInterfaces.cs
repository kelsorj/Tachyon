using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

// This is the server side XML-RPC interface definition for the IWorks Plugin interface, used in Synapsis -> VWorks comms

namespace BioNex.IWorksCommandServer
{
    public interface IPingBack
    {
        [XmlRpcMethod("BioNex.IPingBack.Ping")]
        void Ping();
    }

    public interface IResetStackHeight
    {
        [XmlRpcMethod("BioNex.IResetStackerHeight.ResetStackHeight")]
        void ResetStackHeight();
    }

    public interface IWorksStackerCommands
    {
        [XmlRpcMethod("BioNex.IWorksStackerCommands.IsLocationAvailable")]
        bool IsLocationAvailable(string location_xml);

        [XmlRpcMethod("BioNex.IWorksStackerCommands.MakeLocationAvailable")]
        int MakeLocationAvailable(string location_xml);

        [XmlRpcMethod("BioNex.IWorksStackerCommands.IsMLAComplete")]
        bool IsMLAComplete();

        [XmlRpcMethod("BioNex.IWorksStackerCommands.SinkPlate")]
        int SinkPlate(string labware, int PlateFlags, string SinkToLocation);

        [XmlRpcMethod("BioNex.IWorksStackerCommands.IsSinkPlateComplete")]
        bool IsSinkPlateComplete();

        [XmlRpcMethod("BioNex.IWorksStackerCommands.SourcePlate")]
        int SourcePlate(string labware, int PlateFlags, string SinkToLocation);

        [XmlRpcMethod("BioNex.IWorksStackerCommands.IsSourcePlateComplete")]
        bool IsSourcePlateComplete();

        [XmlRpcMethod("BioNex.IWorksStackerCommands.IsStackEmpty")]
        int IsStackEmpty( string location);

        [XmlRpcMethod("BioNex.IWorksStackerCommands.IsStackFull")]
        int IsStackFull( string location);

        [XmlRpcMethod("BioNex.IWorksStackerCommands.Abort")]
        void Abort();

        [XmlRpcMethod("BioNex.IWorksStackerCommands.PrepareForRun")]
        void PrepareForRun();
    }

    // Client uses this interface -- client is HiveUpstackerDriver in this perspective
    public interface IWorksStackerDeviceProxy : IPingBack, IWorksStackerCommands, IXmlRpcProxy
    {
    }

    // Server uses this abstract class -- server is Synapsis in this perspective
    public abstract class StackerRpcInterface : SystemMethodsBase, IPingBack, IWorksStackerCommands
    {
        public abstract void Ping();
        public abstract bool IsLocationAvailable(string location_xml);
        public abstract int MakeLocationAvailable(string location_xml);
        public abstract bool IsMLAComplete();
        public abstract int SinkPlate(string labware, int PlateFlags, string SinkToLocation);
        public abstract bool IsSinkPlateComplete();
        public abstract int SourcePlate(string labware, int PlateFlags, string SinkToLocation);
        public abstract bool IsSourcePlateComplete();
        public abstract int IsStackEmpty( string location);
        public abstract int IsStackFull( string location);
        public abstract void Abort();
        public abstract void PrepareForRun();

        public override object InitializeLifetimeService(){ return null; } // The lease to this object shall never expire
    }
}
