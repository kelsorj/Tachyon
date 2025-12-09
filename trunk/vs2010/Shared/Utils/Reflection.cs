using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class Reflection
    {
        public static Dictionary<string,string> GetPropertiesAndValues( object myobject)
        {
            Dictionary<string,string> props_and_values = new Dictionary<string,string>();
            System.Reflection.PropertyInfo[] members = myobject.GetType().GetProperties();
            foreach( System.Reflection.PropertyInfo pi in members) {
                props_and_values.Add( pi.Name, pi.GetValue( myobject, null).ToString());
            }
            return props_and_values;
        }
    }
#endif
}
