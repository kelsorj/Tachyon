using System;
using System.Collections.Generic;

namespace BioNex.Shared.DeviceInterfaces
{
    public delegate List<BarcodeReadErrorInfo> ReinventoryDelegate( IEnumerable<int> selected_rack_numbers, Action update_callback, bool called_from_diags);

    public class ReinventoryEventArgs : EventArgs
    {
        public bool CalledFromDiags { get; private set; }

        public ReinventoryEventArgs( bool called_from_diags)
        {
            CalledFromDiags = called_from_diags;
        }
    }

    public interface IReinventoryStrategy
    {
        event EventHandler ReinventoryStrategyBegin;
        event EventHandler ReinventoryStrategyComplete;
        event EventHandler ReinventoryStrategyError;

        void ReinventoryThreadComplete( IAsyncResult iar);
        List<BarcodeReadErrorInfo> ReinventorySelectedRacksThread( IEnumerable<int> selected_rack_numbers, Action update_callback, bool called_from_diags);
    }
}
