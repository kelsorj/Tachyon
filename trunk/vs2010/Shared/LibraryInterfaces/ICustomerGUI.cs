using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.LibraryInterfaces
{
    public interface ICustomerGUI
    {
        /// <summary>
        /// Main app hooks into this event to be notified when the customer GUI is done
        /// running its protocol
        /// </summary>
        event EventHandler ProtocolComplete;
        
        string GUIName { get; }
        //! \todo need to think about whether or not "Busy" is too generic.  For example, there could be
        //! cases where the customer plugin says the system is busy, but we could really do something else,
        //! like unlock a BPS140 that's not near the robot in motion.  This came up because I do actually
        //! need to allow Synapsis to determine whether or not it's safe to unlock the BPS140, and it
        //! used to check the Reinventorying flag, which is actually now a customer plugin detail.
        bool Busy { get; }
        string BusyReason { get; }

        /// <summary>
        /// whether or not the customer GUI has determined that it's ready to start a process
        /// </summary>
        /// <param name="failure_reasons"></param>
        /// <returns></returns>
        bool CanExecuteStart( out IEnumerable<string> failure_reasons);
        /// <summary>
        /// Starts the customer-specific process
        /// </summary>
        /// <returns>true if process starts OK, false if a failure occurred</returns>
        bool ExecuteStart();

        /// <summary>
        /// return false if you don't even want to give the option of starting a protocol (e.g. controlling Synapsis from some other software)
        /// </summary>
        /// <returns></returns>
        bool ShowProtocolExecuteButtons();

        bool CanClose();
        /// <summary>
        /// Allows a customer GUI to determine whether or not the Pause/Resume button should be disabled.  It
        /// cannot force an enabled state, for safety reasons.  In the case where it wants to allow the
        /// system to be paused, the ultimate decision is up to the AbortRetryIgnoreStateMachine.
        /// </summary>
        /// <returns></returns>
        bool CanPause();
        void Close();

        /// <summary>
        /// Called by Synapsis after the GUI has been constructed and loaded into the main window
        /// </summary>
        void CompositionComplete();

        bool AllowDiagnostics();
    }

    public interface ICustomerGUIPauseListener
    {
        void Pause();
        void Resume();
        void Abort();
    }
}
