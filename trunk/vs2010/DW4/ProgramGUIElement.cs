using System;
using System.Windows.Forms;

namespace AQ3
{
	public enum CardType
	{
		PlaceHolderBeforeSmall = 0,
		PlaceHolderAfterFull,
		ProgramCard
	}

	public class ProgramGUIElement
	{
		public ProgramGUIElement()
		{
		}

		// common for all cards
		public string strCardName;	// platecard, aspiratecard, ...
		public UserControl uc;		// the program card itself

		// platecard attributes
		public string platecard_name;
		public int platecard_format;
		public double platecard_height;
		public double platecard_depth;
		public double platecard_offset;
		public double platecard_offset2;
		public double platecard_max_volume;
		public double platecard_dbwc;
		public double platecard_dbwc2;
		public double platecard_asp_offset;
		public string platecard_rows;
		public bool platecard_loBase;
		public double platecard_diameter;
		public string platecard_well_shape;

		// aspiratecard attributes
		public int aspiratecard_velocity;
		public double aspiratecard_time;
		public double aspiratecard_probe_height;
		public double aspiratecard_asp_offset;
		public bool aspiratecard_sweep;

		// dispensecard attributes
		public int dispensecard_inlet;
		public double dispensecard_volume;
		public double dispensecard_liquid_factor;
		public double dispensecard_disp_low;
		public string dispensecard_liquid_name;

		// soakcard attributes
		public int soakcard_time;

		// programlinkcard attributes
		public string programlinkcard_program;

		// repeatcard attributes
		public int repeatcard_repeats;
		public int repeatcard_from;
		public int repeatcard_to;

		// platecardrowsonly attributes
		public string platecardrowsonly_rows;

		public bool allowedToChangeWellType = true;
	}
}
