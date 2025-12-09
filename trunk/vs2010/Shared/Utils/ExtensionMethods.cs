using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BioNex.Shared.Utils
{
    public static class ExtensionMethods
    {
        public class HeaderNotInByteArrayException : ApplicationException
        {
            public HeaderNotInByteArrayException()
            {
            }
        }

        public static IEnumerable<T> ReplaceItems<T>( this IEnumerable<T> my_list,
                                                      T to_replace, T replace_with) where T : IEquatable<T>
        {
            return from s in my_list select (s.Equals(to_replace) ? replace_with : s);
        }

        public static bool IsAbsolutePath( this string path)
        {
            return BioNex.Shared.Utils.FileSystem.IsAbsolutePath( path);
        }

        public static string ToAbsoluteAppPath( this string path)
        {
            return FileSystem.ConvertToAbsolutePath( path);
        }

        /// <summary>
        /// e.g. c:\folder\filename.txt would return c:\folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetDirectoryFromFilePath( this string path)
        {
            int pos = path.LastIndexOf( "\\");
            if( pos != -1)
                return path.Substring(0, path.LastIndexOf("\\"));
            return FileSystem.GetModulePath();
        }
        

        // this is not actually an extension method, but I want to make it one.
        public static Byte[] RemoveFromByteArrayUntil( this Byte[] array, Byte[] until)
        {
            Debug.Assert( until.Count() > 0);
            int num_header_bytes = until.Count();
            int header_start_pos = 0; // the position of the header bytes, defined by [until]
            byte first_header_byte = until[0];
            while( header_start_pos != -1) {
                header_start_pos = Array.IndexOf( array, first_header_byte, header_start_pos);
                if( header_start_pos == -1)
                    break;
                // if we get here, then we've found the first header byte, and we need to look
                // for the next ones sequentially
                for( int header_ctr=1; header_ctr<num_header_bytes; header_ctr++) {
                    // we're going to loop over each of the header bytes, but will
                    // bail out of this loop if there isn't a match
                    if( array[header_start_pos + header_ctr] != until[header_ctr]) {
                        // no match, so bail out.  but before doing that, advance
                        // header_start_pos so the outer loop won't find the same
                        // occurrence of the first header byte over and over again
                        header_start_pos++;
                        break;
                    }
                }
                // if we get here, we've found the header!
                // shift all of the bytes over from header_start_pos to the left to index 0

                // now resize the array
                // create a new byte array of the new size
                int new_size = array.Count() - header_start_pos;
                byte[] output_array = new byte[new_size];
                Array.Copy( array, header_start_pos, output_array, 0, new_size);
                return output_array;
            }

            // if we get here, we didn't find a header, so throw an exception
            throw new HeaderNotInByteArrayException();
        }

        public static Byte[] StripBytesAfter( this Byte[] array, Int32 total_bytes_wanted)
        {
            Byte[] copy = new Byte[total_bytes_wanted];
            Array.Copy( array, copy, total_bytes_wanted);
            return copy;
        }

        public static string ReplaceLastNumberWith( this string str, int new_number)
        {
            Regex regex = new Regex( @"(.*\s+)\d+");
            MatchCollection matches = regex.Matches( str);
            if( matches.Count == 0)
                return String.Format( "{0}_{1}", str, new_number);
            Debug.Assert( matches.Count == 1, "Regex error during parsing of location name (1)");
            Debug.Assert( matches[0].Groups.Count == 2, "Regex error during parsing of location name (2)");
            return String.Format( "{0}{1}", matches[0].Groups[1].ToString(), new_number.ToString());
        }

        public static string ReplaceLastTwoNumbersWith(this string str, int first_number, int second_number)
        {
            Regex regex = new Regex(@"(.*\s+)\d+(.*\s+)\d+");
            MatchCollection matches = regex.Matches(str);
            if (matches.Count == 0)
                return String.Format("{0}_{1}_{2}", str, first_number, second_number);
            Debug.Assert(matches.Count == 1, "Regex error during parsing of location name (1)");
            Debug.Assert(matches[0].Groups.Count == 3, "Regex error during parsing of location name (2)");
            return String.Format("{0}{1}{2}{3}", matches[0].Groups[1].ToString(), first_number.ToString(), matches[0].Groups[2].ToString(), second_number.ToString());
        }

        public static void GetLastTwoNumbers(this string str, ref int first_number, ref int second_number)
        {
            Regex regex = new Regex(@".*\s+(\d+).*\s+(\d+)");
            MatchCollection matches = regex.Matches(str);
            if (matches.Count == 0)
                return;
            Debug.Assert(matches.Count == 1, "Regex error during parsing of location name (1)");
            Debug.Assert(matches[0].Groups.Count == 3, "Regex error during parsing of location name (2)");

            first_number = matches[0].Groups[1].ToInt();
            second_number = matches[0].Groups[2].ToInt();
        }

        #region Standard deviation

        /// <summary>
        /// Using this for HiG spindle elposl checking
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double StandardDeviation( this IEnumerable<double> values)
        {
            if( values.Count() <= 1)
                return 0;
            double avg = values.Average();
            double sum = values.Sum(d => Math.Pow(d - avg, 2));
            return Math.Sqrt((sum) / (values.Count() - 1));
        }

        public static double StandardDeviation( this IEnumerable<long> values)
        {
            if( values.Count() <= 1)
                return 0;
            double average = values.Average();
            double sum = values.Sum( d => Math.Pow( d - average, 2));
            return Math.Sqrt( sum / (values.Count() - 1));
        }

        public static double StandardDeviation( this IEnumerable<int> values)
        {
            if( values.Count() <= 1)
                return 0;
            double average = values.Average();
            double sum = values.Sum( d => Math.Pow( d - average, 2));
            return Math.Sqrt( sum / (values.Count() - 1));
        }

        #endregion

        public static IEnumerable<long> GetDeltas( this IEnumerable<long> values)
        {
            List<long> temp_values = values.ToList();
            return temp_values.Skip( 1).Select( (next, index) => next - temp_values[index]);
        }

        public static int ToInt( this object value)
        {
            return int.Parse( value.ToString());
        }

        public static bool ToBool( this object value)
        {
            return value.ToString() != "0" && value.ToString().ToLower() != "false";
        }

        public static double ToDouble( this object value)
        {
            return double.Parse( value.ToString());
        }

        public static string ToCommaSeparatedString( this IEnumerable<string> items)
        {
            if( items.Count() == 0)
                return "";
            if( items.Count() == 1)
                return items.First();
            string output = "";
            foreach( var x in items)
                output = output + x + ", ";
            // remove trailing ,
            return output.Remove( output.LastIndexOf( ","));
        }

        public static string ToCommaSeparatedString( this HashSet<string> items)
        {
            if( items.Count() == 0)
                return "";
            if( items.Count() == 1)
                return items.First();
            string output = "";
            foreach( var x in items)
                output = output + x + ", ";
            // remove trailing ,
            return output.Remove( output.LastIndexOf( ","));
        }

        public static string ToDictionaryString<T,U>( this Dictionary<T,U> dic)
        {
            StringBuilder sb = new StringBuilder();
            foreach( KeyValuePair<T,U> kvp in dic) {
                sb.Append( String.Format( " [{0}, {1}]", kvp.Key, kvp.Value));
            }
            return sb.ToString();
        }
    }
}
