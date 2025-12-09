using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class Strings
    {
        public static List<string> GenerateIntermediateTeachpointNames( string first_teachpoint, string second_teachpoint)
        {
            // save up to the last number
            Regex regex = new Regex( @"(.* )(\d+)");
            Match match = regex.Match( first_teachpoint);
            GroupCollection groups = match.Groups;
            string teachpoint_prefix = groups[1].ToString();
            int first_teachpoint_number = groups[2].ToInt();
            match = regex.Match( second_teachpoint);
            groups = match.Groups;

            // if the two teachpoint name formats are incompatible, return empty set
            if( groups[1].ToString() != teachpoint_prefix)
                return new List<string>();

            int last_teachpoint_number = groups[2].ToInt();
            
            var intermediate_names = new List<string>();
            for( int i=first_teachpoint_number; i<=last_teachpoint_number; i++)
                intermediate_names.Add( teachpoint_prefix + i.ToString());
            return intermediate_names;
        }
    }

    /// <summary>
    /// allows the caller to pass in a string (e.g. barcode), yet still allow the callee to modify
    /// the string, such that the caller can use the new, modified string.
    /// </summary>
    /// <remarks>
    /// I needed a way to override the barcode passed in.  See http://msdn.microsoft.com/en-us/library/85w54y0a.aspx
    /// </remarks>
    public class MutableString
    {
        public string Value { get; set; }

        public MutableString()
        {
            Value = "";
        }

        public MutableString( string s)
        {
            Value = s;
        }

        public static explicit operator MutableString( string s)
        {
            MutableString ms = new MutableString(s);
            return ms;
        }

        public static implicit operator string( MutableString ms)
        {
            return ms.Value;
        }

        public override bool Equals(object obj)
        {
            MutableString other = obj as MutableString;
            if( obj == null)
                return false;
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
#endif
}
