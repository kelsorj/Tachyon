using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.TechnosoftLibrary
{
    public interface ITechnosoftConnectionSharer
    {
        string GetConnectionSharerName();
        List<IAxis> LoadBuddyConfiguration( string buddy_name, bool simulate, string motor_settings_path, string tsm_setup_folder);
        void CloseBuddyConnection( string buddy_name);
    }
}
