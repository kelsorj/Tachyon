using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;

namespace BioNex.Plugins.Dock
{
    public interface DockExternalCommunications
    {
        /// <summary>
        /// Transitions BehaviorEngine and parks the robot
        /// </summary>
        /// <returns>false if prepare for dock fails, i.e. we couldn't transition into the desired state</returns>
        bool PrepareForDock(string dock_name);
        void ReadyToDock(string dock_name);
        /// <summary>
        /// Transitions BehaviorEngine and parks the robot
        /// </summary>
        /// <returns>false if prepare for dock fails, i.e. we couldn't transition into the desired state</returns>
        bool PrepareForUndock();
        void ReadyToUndock();
        bool BarcodeReaderAvailable { get; }
        bool Reinventorying { get; }

        bool ReinventoryAllowed( string dock_name, out string reason_not_allowed);
        bool UndockAllowed( string dock_name, out string reason_not_allowed);
        bool DockAllowed( string dock_name, out string reason_not_allowed);

        /// <summary>
        /// called when the user clicks the button in the GUI.  This tells the main app that we're
        /// going to be using a robot, and the main app will block until the robot is doing
        /// whatever it's busy doing.
        /// </summary>
        /// <param name="dock_name"></param>
        /// <returns>False if the reinventory shouldn't happen (due to shutdown / abort)</returns>
        bool Reinventory( string dock_name);

        RobotInterface GetRobotForDock(string dock_name);
    }
}
