using System;
using System.Windows.Data;

namespace BioNex.BumblebeePlugin
{
    /// <summary>
    /// Interaction logic for ServoStatus.xaml
    /// </summary>
    public partial class ServoStatus
    {
        public ServoStatus()
        {
            InitializeComponent();
        }
    }

    public class ServoOnOffAxisParameter
    {
        public string ID { get; set; }
        public bool ServoOn { get; set; }
    }

    public class ServoOnOffAxisMultiConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var param = new ServoOnOffAxisParameter();
            foreach( var obj in values) {
                if( obj is string)
                    param.ID = (string)obj;
                if( obj is bool)
                    param.ServoOn = (bool)obj;
            }
            return param;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            // we aren't ever going to need to convert back for this formatter!
            return null;
        }

        #endregion
    }
}
