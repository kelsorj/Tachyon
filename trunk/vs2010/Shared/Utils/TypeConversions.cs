using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public static class TypeConversions
    {
        public static T StringToEnum<T>( String name)
        {
            return (T)Enum.Parse( typeof(T), name);
        }
    }
#endif
}
