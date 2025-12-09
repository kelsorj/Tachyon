using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Utils
{
    /// <summary>
    /// Used to display different values for an enum than the enum name itself.  Useful
    /// because an enum could be something like ChangeTipsAfterEveryUse, but that's
    /// way less readable and friendly than "Change tips after every use"
    /// </summary>
    /// <remarks>
    /// taken from http://www.ageektrapped.com/blog/the-missing-net-7-displaying-enums-in-wpf/
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DisplayStringAttribute : Attribute
    {
       private readonly string value;
       public string Value
       {
          get { return value; }
       }

       public string ResourceKey { get; set; }

       public DisplayStringAttribute(string v)
       {
          this.value = v;
       }

       public DisplayStringAttribute()
       {
       }
    }
}
