using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BioNex.Shared.Utils
{
    public class StringFormatter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string format_string = parameter as string;
            if( format_string != null) {
                double pos = double.Parse( value.ToString());
                return string.Format( culture, format_string, value);
            } else {
                return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // we aren't ever going to need to convert back for this formatter!
            return null;
        }

        #endregion
    }

    [ValueConversion(typeof(bool?), typeof(bool))]
    public class RadioButtonBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool param = bool.Parse(parameter.ToString());
            return value == null ? false : (bool)value == param;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool param = bool.Parse(parameter.ToString());
            return value == null ? false : (bool)value == param;
        }
        #endregion
    }

#if !HIG_INTEGRATION
    public class AngleFormatter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string format_string = parameter as string;
            if( format_string != null) {
                double angle = double.Parse( value.ToString());
                return string.Format( culture, format_string, angle);
            } else
                return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // we aren't ever going to need to convert back for this formatter!
            return null;
        }

        #endregion
    }

    public class NegativeFormatter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return -double.Parse( value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // we aren't ever going to need to convert back for this formatter!
            return null;
        }

        #endregion
    }

    public class StringPlus0Formatter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string format_string = parameter as string;
            if( format_string != null) {
                int index = int.Parse( value.ToString());
                return string.Format( culture, format_string, index);
            } else
                return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // we aren't ever going to need to convert back for this formatter!
            return null;
        }

        #endregion
    }


    public class StringPlus1Formatter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string format_string = parameter as string;
            if( format_string != null) {
                int index = int.Parse( value.ToString());
                return string.Format( culture, format_string, index + 1);
            } else
                return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // we aren't ever going to need to convert back for this formatter!
            return null;
        }

        #endregion
    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            try
            {
                //return new Uri((string)value);
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri( value as String);
                // the following option is really important if we are using the same filename for different image data
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                // the following option is really important so that we can overwrite the same file over and over again
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch 
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Useful for databinding to multi-select items controls like Listboxes
    /// </summary>
    public class SelectableString : INotifyPropertyChanged
    {
        private bool _is_selected;
        public bool IsSelected
        { 
            get { return _is_selected; }
            set {
                _is_selected = value;
                OnPropertyChanged( "IsSelected");
            }
        }

        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }

    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.Equals(false))
                return Binding.DoNothing;
            else
                return parameter;
        }
    }

    /// <summary>
    /// This class simply converts a image url into a Image for use within a MenuItem
    /// </summary>
    /// <remarks>
    /// taken from Sasha Barber's Cinch project: https://cinch.svn.codeplex.com
    /// </remarks>
    //[ValueConversio(nValueConversiontypeof(String), typeof(Image))]
    public class MenuIconConverter : IValueConverter
    {
        #region IValueConverter implementation
        /// <summary>
        /// Convert string to Image
        /// </summary>
        public object Convert(object value, Type targetType, 
            object parameter, CultureInfo culture)
        {
            if (value == null)
                return Binding.DoNothing;

            String imageUrl = value.ToString();

            if (String.IsNullOrEmpty(imageUrl))
                return Binding.DoNothing;

            Image img = new Image();
            img.Width = 16;
            img.Height = 16;
            BitmapImage bmp = new BitmapImage(new Uri(imageUrl, UriKind.RelativeOrAbsolute));
            img.Source = bmp;
            return img;
        }

        /// <summary>
        /// Convert back, but its not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, 
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Not implemented");
        }
        #endregion
    }

    public class Events
    {
        public static void WaitForEvents(AutoResetEvent[] events)
        {
            // can only wait on 64 handles at a time
            int num_events_to_wait_on = events.Count();
            int start_event_index = 0;
            const int max_events = 64;
            while (start_event_index < num_events_to_wait_on)
            {
                int num_events_this_batch = num_events_to_wait_on - start_event_index;
                if (num_events_this_batch <= max_events)
                {
                    AutoResetEvent[] temp = new AutoResetEvent[num_events_this_batch];
                    Array.Copy(events, start_event_index, temp, 0, num_events_this_batch);
                    WaitHandle.WaitAll(temp);
                    start_event_index += max_events;
                }
                else
                {
                    AutoResetEvent[] temp = new AutoResetEvent[max_events];
                    Array.Copy(events, start_event_index, temp, 0, max_events);
                    WaitHandle.WaitAll(temp);
                    start_event_index += max_events;
                }
            }
        }
    }
#endif
}

