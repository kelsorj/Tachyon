using System;

namespace AQ3
{
	/// <summary>
	/// Summary description for PlateProperties.
	/// </summary>
	public class PlateProperties
	{
		public PlateProperties()
		{
			strPlateName = "";
			strPlateType = "0";
			strPlateHeight = "0";
			strPlateDepth = "0";
			strPlateOffset = "0";
			strPlateOffset2 = "0";
			strPlateMaxVolume = "0";
			strPlateDbwc = "0";
			strPlateDbwc2 = "0";
			strPlateASPOffset = "0";
			strPlateBottomWellDiameter = "0";
			strWellShape = "0";
			loBase = false;
		}

		public string strPlateName;
		public string strPlateType;
		public string strPlateHeight;
		public string strPlateDepth;
		public string strPlateOffset;
		public string strPlateOffset2;
		public string strPlateMaxVolume;
		public string strPlateDbwc;
		public string strPlateDbwc2;
		public string strPlateASPOffset;
		public string strPlateBottomWellDiameter;
		public string strWellShape;
		public bool loBase;
	}
}
