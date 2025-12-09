using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class ToolTipHelper
    {
        /// <summary>
        /// In the dictionary, put the functions that should return TRUE for an allowable condition.
        /// </summary>
        /// <param name="can_execute_message"></param>
        /// <param name="tooltip"></param>
        /// <param name="allow_conditions"></param>
        /// <returns></returns>
        public static bool EvaluateToolTip( string can_execute_message, ref string tooltip, IDictionary<Func<bool>, string> allow_conditions)
        {
            StringBuilder sb = new StringBuilder();
            foreach( var conditional in allow_conditions) {
                if( !conditional.Key())
                    sb.AppendLine( conditional.Value);
            }
            if( sb.Length != 0) {
                tooltip = sb.ToString();
                return false;
            }
            tooltip = can_execute_message;
            return true;
        }
    }
#endif
}
