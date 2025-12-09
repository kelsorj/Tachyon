using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using BioNex.Shared.DeviceInterfaces;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using log4net;
using BioNex.SynapsisPrototype;
using BioNex.BumblebeePlugin.Scheduler;
using System.Threading;

namespace BioNex.DC2DelidCycleTesterGUI
{
    [Export]
    public class Model
    {
        [Import]
        public BioNex.Shared.LibraryInterfaces.ILabwareDatabase LabwareDatabase { get; set; }
        [ImportMany]
        private IEnumerable<ExternalPlateTransferSchedulerInterface> PlateTransferServiceStrategies { get; set; }

        /// <summary>
        /// needed so that the plugin code (checklist.xaml.cs) can forward the event to the model for handling
        /// </summary>
        internal event EventHandler ProtocolComplete;
        
        private static readonly ILog _log = LogManager.GetLogger( typeof( Model));
        [Import]
        internal DeviceManager _device_manager { get; set; }

       
        public bool ExecuteDelidCycleTest( )
        {
            var thread = new Thread(Execute);
            thread.IsBackground = true;
            thread.Start();
            return true;
        }
        private void Execute()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            var robot = (from p in devices where p.Value.ProductName == "Hive" select p.Value).FirstOrDefault();
            var bumble = (from p in devices where p.Value.ProductName == "Bumblebee" select p.Value).FirstOrDefault();

            var labware = LabwareDatabase.GetLabware("96 Greiner 655101");
            var lid = LabwareDatabase.GetLabware("delidding 96");
            var golden = "DELIDDING GOLDEN";

            int rack = 1; int slot = 1;
            new DelidCycleStateMachine(robot, bumble, labware, lid, golden, rack, slot).Start();

            labware = LabwareDatabase.GetLabware("96 Greiner 655101");
            lid = LabwareDatabase.GetLabware("delidding 96");
            golden = "DELIDDING GOLDEN";

            rack = 1; slot = 2;
            new DelidCycleStateMachine(robot, bumble, labware, lid, golden, rack, slot).Start();

            labware = LabwareDatabase.GetLabware("96 Greiner 655101");
            lid = LabwareDatabase.GetLabware("delidding 48");
            golden = "48 DELIDDING GOLDEN";
            
            rack = 1; slot = 3;
            new DelidCycleStateMachine(robot, bumble, labware, lid, golden, rack, slot).Start();

            ProcessComplete();
        }

        /// <summary>
        /// Returns all of the plugins that were successfully loaded and present in the device manager
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DeviceInterface> GetAllDevices()
        {
            return _device_manager.GetAllDevices();
        }

        /// <summary>
        /// This executes when the scheduler is done processing the hitpick list
        /// </summary>
        /// <param name="iar"></param>
        private void ProcessComplete()
        {
            // signal that the protocol is complete
            if( ProtocolComplete != null)
                ProtocolComplete( this, new EventArgs());

            //! \todo not sure if this is the best way to clear the event handler
            ProtocolComplete = null;
            // all of the cleanup and notifications are now handled by the Synapsis app via the ProtocolComplete event handler
        }
    }
}