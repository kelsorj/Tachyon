using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace BioNex.Utilities.BioNexFirmwareUpdater
{
    public class FirmwareVersionsFormatter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            List<BioNex.Shared.LibraryInterfaces.FirmwareVersionInfo> versions = value as List<BioNex.Shared.LibraryInterfaces.FirmwareVersionInfo>;
            if( versions == null)
                return "";
            else {
                StringBuilder sb = new StringBuilder();
                foreach( var v in versions) {
                    sb.AppendFormat( "{0}: {1}.{2} ", v.AxisName, v.CurrentMajorVersion, v.CurrentMinorVersion);
                }
                return sb.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // we aren't ever going to need to convert back for this formatter!
            return null;
        }

        #endregion
    }}
