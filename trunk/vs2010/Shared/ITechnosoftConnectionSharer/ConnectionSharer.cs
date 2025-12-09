using System.Collections.Generic;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.Shared.ITechnosoftConnectionSharer
{
    public interface ITechnosoftConnectionSharer
    {
        string GetConnectionSharerName();
        List<IAxis> LoadBuddyConfiguration( string buddy_name_or_port, bool simulate, string motor_settings_path, string tsm_setup_folder);
        void AbortBuddy();
        void CloseBuddyConnection( string buddy_name);
        void ResetPauseAbort();
    }
}
