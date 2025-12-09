using System;
using System.Linq;

namespace BioNex.Shared.Utils
{
    public class StringUtil
    {
        #region FIND_FIRST_NOT_OF
        public static int FindFirstNotOf( string subject_string, char[] any_of, int start_index = 0, int count = -1)
        {
            int end_index = ( count < 0 ? subject_string.Length : Math.Min( start_index + count, subject_string.Length));
            int index = start_index;
            while( index < end_index){
                if( !any_of.Contains( subject_string[ index])){
                    return index;
                }
                ++index;
            }
            return -1;
        }
        #endregion
    }
}
