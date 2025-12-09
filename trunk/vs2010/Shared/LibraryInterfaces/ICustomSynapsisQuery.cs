using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.LibraryInterfaces
{
    /// <summary>
    /// This is the interface used to allow customer GUI plugins to communicate with Synapsis
    /// to get information like device homing status, system check status, abort/pause/resume state
    /// </summary>
    public interface ICustomSynapsisQuery
    {
        bool AllDevicesHomed { get; }
        bool SystemCheckOK( out string reason_not_ok);
        bool ClearToHome ( out string tooltip_text);
        bool Idle { get; }
        bool Running { get; }
        bool Paused { get; }

        //! \todo this is tainting the "Query" part of the name, but for now we'll
        //! put methods here to allow the customer plugins to ask Synapsis to do things,
        //! like home devices
        // returns false if the user selects NO from the prompt
        bool HomeAllDevices( bool show_prompt=true);
    }
}
