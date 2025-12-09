using System;
using System.Windows.Forms;

namespace AQ3
{
	public class PlateRow
	{
		public PlateRow(bool bSelected, UserControl uc)
		{
			m_bSelected = bSelected;
			m_uc = uc;
		}

		public UserControl m_uc; // the program row itself
		public bool m_bSelected;
	}
}
