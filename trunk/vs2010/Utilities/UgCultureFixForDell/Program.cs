using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace UgCultureFixForDell
{
    class Program
    {
        static void Main(string[] args)
        {
            var culture = new CultureAndRegionInfoBuilder("ug", CultureAndRegionModifiers.None);
            var ci = new CultureInfo("en-US");
            var ri = new RegionInfo("US");
            culture.LoadDataFromCultureInfo(ci);
            culture.LoadDataFromRegionInfo(ri);
            culture.Register();
        }
    }
}
