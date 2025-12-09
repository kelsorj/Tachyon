using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Amulet
{
    public static class Utils
    {
        static public string ByteToHexString( this byte value)
        {
            return String.Format( "{0:X2}", value);
        }

        static public string WordToHexString( this ushort value)
        {
            return String.Format( "{0:X4}", value);
        }

        static public byte[] ByteToByteArray( this byte value)
        {
            string value_string = value.ByteToHexString();
            return new byte[] { (byte)value_string[0], (byte)value_string[1] };
        }

        static public byte[] WordToByteArray( this ushort value)
        {
            string value_string = value.WordToHexString();
            return new byte[] { (byte)value_string[0], (byte)value_string[1], (byte)value_string[2], (byte)value_string[3] };
        }

        static public byte ByteArrayToByte( this byte[] value)
        {
            byte result = 0;
            for( int i=0; i<2; i++) {
                result |= (byte)(("0123456789ABCDEF".IndexOf( char.ToUpper( (char)value[i]))) << (4 * (1 - i)));
            }
            return result;
        }

        static public ushort ByteArrayToWord( this byte[] value)
        {
            ushort result = 0;
            for( int i=0; i<4; i++) {
                result |= (ushort)(("0123456789ABCDEF".IndexOf( char.ToUpper( (char)value[i]))) << (4 * (3 - i)));
            }
            return result;
        }

        static public string ByteArrayToString( this byte[] value)
        {
            StringBuilder result = new StringBuilder();
            foreach( byte b in value) {
                if( b != '\0')
                    result.Append( (char)b);            
            }
            return result.ToString();
        }
    }
}
