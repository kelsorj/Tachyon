using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Configuration;

namespace AQ3
{
	/// <summary>
	/// Summary description for Utilities.
	/// </summary>
	public class Utilities
	{
		public Utilities()
		{
		}
		
		public static byte HiByte(int n)
		{
			n = n >> 8;
			return (byte)n;
		}

		public static byte LoByte(int n)
		{
			return (byte)n;
		}

		public static void PutHiByte(ref int n, byte b)
		{
			int us = b;
			us = us << 8;
			n &= 0x00FF;
			n |= us;
		}

		public static void PutLoByte(ref int n, byte b)
		{
			n &= 0xFF00;
			n |= b;
		}

		public static string CardDisplayRowString( string str, int max )
		{
			if( str.Length == 0 ) return "";
			StringBuilder sb = new StringBuilder();
			int hex = 0;
			bool start = false;
			for( int i=0; i<str.Length; i++ )
			{
				int charVal = (byte)str[i];
				if( charVal == hex+1 || charVal == hex || hex-charVal == 25 )
				{
					hex = charVal;
					continue;
				}
				if( charVal != hex )
				{
					if( start )
					{
						sb.Append( str[i-1] );
						if( i>1 && str[i-1] == str[i-2] )
						{
							sb.Append( str[i-1] );
						}
						sb.AppendFormat( "{0}; ", max );
					}
					start = true;
					sb.Append( str[i] );
					if( i<str.Length-1 && str[i] == str[i+1] ) sb.Append( str[i] );
					sb.Append( "1:" );
					hex = charVal;
				}
			}
			sb.Append( str[str.Length-1] );
			if( str.Length-2 > 0 && str[str.Length-1] == str[str.Length-2] )
			{
				sb.Append( str[str.Length-1] );
			}
			sb.Append( max );
			return sb.ToString();
		}

		public static string CardDisplayColumnString( string columns, int max )
		{
			string[] nums = columns.Split( ' ' );
			ArrayList list = new ArrayList();
			foreach( string n in nums )
			{
				if( n.Trim().Length > 0 )
				{
					list.Add( int.Parse(n) );
				}
			}
			return CardDisplayColumnString( (int[])list.ToArray(typeof(int)), max );
		}

		public static string CardDisplayColumnString( int[] list, int max )
		{
			if( list.Length == 0 ) return "";
			string maxRow = "";
			if( max == 12 ) maxRow = "H";
			if( max == 24 ) maxRow = "P";
			if( max == 48 ) maxRow = "FF";
			if( max == 8 ) maxRow = "H";
			if( max == 16 ) maxRow = "P";
			if( max == 32 ) maxRow = "FF";

			StringBuilder sb = new StringBuilder();
			bool start = false;
			int current = -1;

			for( int i=0; i<list.Length; i++ )
			{
				int column = list[i];
				if( column == current+1 || column == current )
				{
					current = column;
					continue;
				}
				
				if( start )
				{
					sb.AppendFormat( "{0}{1}; ", maxRow,list[i-1] );
				}
				start = true;
				sb.AppendFormat( "A{0}:", list[i] );
				current = column;				
			}
			sb.AppendFormat( "{0}{1}", maxRow, list[list.Length-1] );
			return sb.ToString();
		}

		public static string ColumnsToRows( string columnString )
		{
			if( columnString.Length == 0 ) return "";
			if( Regex.IsMatch( columnString, @"\b[^\d\s]\b" ) ) return columnString; // return if it's already a rowstring
			columnString = columnString.Trim();
			StringBuilder sb = new StringBuilder();
			string[] numbers = Regex.Split( columnString, @"\s+" );
			for( int i=0; i<numbers.Length; i++ )
			{
				int number = int.Parse( numbers[i] );
				if( number<27 )
				{
					sb.Append( (char)(number+64) );
				}
				else
				{
					sb.Append( (char)(number+38) );
					sb.Append( (char)(number+38) );					
				}
			}
			return sb.ToString();
		}

		public static byte GetDeviceCode( int comPort )
		{
			RS232 rs232 = null;

			try 
			{
				rs232 = new RS232(comPort);
				
				byte DeviceCode1;
				byte DeviceCode2;
				byte DeviceCode3;
				rs232.GetDeviceCodes(out DeviceCode1, out DeviceCode2, out DeviceCode3);
				rs232.Dispose();
				return DeviceCode1;
			}
			catch (Exception e)
			{
				e = e;
				if( rs232 != null )
				{
					rs232.Dispose();
				}
				return 0;
			}
		}

		public static bool IsMachineConnected( int comPort )
		{
			RS232 rs232 = null;
			try 
			{
				rs232 = new RS232(comPort);
				rs232.GetSerialNumber();
				rs232.Dispose();
				return true;
			}
			catch (Exception)
			{
				if( rs232 != null )
				{
					rs232.Dispose();
				}
				return false;
			}
			
		}

//		public static int GetDeviceCode( mainForm mf )
//		{
//			return GetDeviceCode( mf.m_xmlData.GetCommPort() );
//		}

		public static string GetDeviceTypeString( mainForm mf )
		{
			string strRetValue = "";
			RS232 rs232 = null;

			try 
			{
				rs232 = new RS232(mf.m_xmlData.GetCommPort());

				byte DeviceCode1;
				byte DeviceCode2;
				byte DeviceCode3;

				rs232.GetDeviceCodes(out DeviceCode1, out DeviceCode2, out DeviceCode3);
				switch(DeviceCode1)
				{
					case 0x10: strRetValue += "AquaMax 12389";	break;
					case 0x11: strRetValue += "AquaMax 12392";	break;
						//case 0x12: strRetValue += "AquaMax 12390";	break;
					case 0x12: strRetValue += "AquaMax DW4";	break;
					case 0x13: strRetValue += "AquaMax DW4C";	break;
					case 0x20: strRetValue += "Embla 12384";	break;
					case 0x21: strRetValue += "Embla 12385";	break;
					case 0x22: strRetValue += "Embla 12386";	break;
					case 0x23: strRetValue += "Embla 12387";	break;
					case 0x24: strRetValue += "Embla 12388";	break;
					default  : strRetValue += "Unknown model";	break;
				}
				/*
				strRetValue += " ";

				switch(DeviceCode2) 
				{
					case 0x10: strRetValue += " (Washer)";			break;
					case 0x11: strRetValue += " (Washer (Robot))";	break;
					case 0x20: strRetValue += " (Dispenser)";		break;
					default  : strRetValue += " (Unknown type)";	break;
				}
				*/

				rs232.Dispose();
			}
			catch (Exception e)
			{
				e = e;
				if( rs232 != null )
				{
					rs232.Dispose();
				}
				strRetValue = "N/A (No Device Connected)";
			}

			return strRetValue;
		}

		public static string GetDeviceFirmwareString( mainForm mf )
		{
			string strRetValue = "";
			RS232 rs232 = null;

			try
			{
				rs232 = new RS232(mf.m_xmlData.GetCommPort());
				strRetValue = rs232.GetFirmwareText();
				rs232.Dispose();
			}
			catch (Exception e)
			{
				e = e;
				if( rs232 != null )
				{
					rs232.Dispose();
				}
				strRetValue = "N/A (No Device Connected)";
			}

			return strRetValue;
		}

		public static string GetDeviceSerialNumberString( mainForm mf )
		{
			string strRetValue = "";
			RS232 rs232 = null;

			try
			{
				rs232 = new RS232(mf.m_xmlData.GetCommPort());
				strRetValue = rs232.GetSerialNumber();
				rs232.Dispose();
			}
			catch (Exception e)
			{
				e = e;
				if( rs232 != null )
				{
					rs232.Dispose();
				}
				strRetValue = "N/A (No Device Connected)";
			}
			
			return strRetValue;
		}

		public static bool SupportColumns
		{
			get
			{
				return bool.Parse( ConfigurationSettings.AppSettings["SupportColumns"] );
			}
		}

		public static string GetRowCharacterString(int nRow)
		{
			string strRowCharacterString = "";

			char c = 'A';
			c += (char)nRow;

			if (nRow < 26)
			{
				strRowCharacterString += c;
			}
			else
			{
				switch (nRow)
				{
					case 26:
						strRowCharacterString += "AA";
						break;
					case 27:
						strRowCharacterString += "BB";
						break;
					case 28:
						strRowCharacterString += "CC";
						break;
					case 29:
						strRowCharacterString += "DD";
						break;
					case 30:
						strRowCharacterString += "EE";
						break;
					case 31:
						strRowCharacterString += "FF";
						break;
					case 32:
						strRowCharacterString += "GG";
						break;
					case 33:
						strRowCharacterString += "HH";
						break;
					case 34:
						strRowCharacterString += "II";
						break;
					case 35:
						strRowCharacterString += "JJ";
						break;
					case 36:
						strRowCharacterString += "KK";
						break;
					case 37:
						strRowCharacterString += "LL";
						break;
					case 38:
						strRowCharacterString += "MM";
						break;
					case 39:
						strRowCharacterString += "NN";
						break;
					case 40:
						strRowCharacterString += "OO";
						break;
					case 41:
						strRowCharacterString += "PP";
						break;
					case 42:
						strRowCharacterString += "QQ";
						break;
					case 43:
						strRowCharacterString += "RR";
						break;
					case 44:
						strRowCharacterString += "SS";
						break;
					case 45:
						strRowCharacterString += "TT";
						break;
					case 46:
						strRowCharacterString += "UU";
						break;
					case 47:
						strRowCharacterString += "VV";
						break;
				}
			}

			return strRowCharacterString;
		}


	}
}
