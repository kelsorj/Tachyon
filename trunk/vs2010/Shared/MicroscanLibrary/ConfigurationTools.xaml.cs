using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Command;

namespace BioNex.Shared.Microscan
{
    /// <summary>
    /// Interaction logic for ConfigurationTools.xaml
    /// </summary>
    public partial class ConfigurationTools : UserControl, INotifyPropertyChanged
    {
        private MicroscanReader _reader;
        internal MicroscanReader Reader
        {
            get { return _reader; }
            set {
                _reader = value;
                dynamic_imaging_control._reader = _reader;
            }
        }

        private string _imagepath;
        public string ImagePath
        {
            get { return _imagepath; }
            set {
                _imagepath = value;
                OnPropertyChanged( "ImagePath");
            }
        }

        private string _bigimagepath;
        public string BigImagePath
        {
            get { return _bigimagepath; }
            set {
                _bigimagepath = value;
                OnPropertyChanged( "BigImagePath");
            }
        }

        private string _saveimagetime;
        public string SaveImageTime
        {
            get { return _saveimagetime; }
            set {
                _saveimagetime = value;
                OnPropertyChanged( "SaveImageTime");
            }
        }

        private int _imageformatindex;
        public int ImageFormatIndex
        {
            get { return _imageformatindex; }
            set {
                _imageformatindex = value;
                OnPropertyChanged( "ImageFormatIndex");
            }
        }

        private byte _imagequality;
        public byte ImageQuality
        {
            get { return _imagequality; }
            set {
                _imagequality = value;
                OnPropertyChanged( "ImageQuality");
            }
        }

        private int _rowpointer;
        public int RowPointer
        {
            get { return _rowpointer; }
            set {
                _rowpointer = value;
                OnPropertyChanged( "RowPointer");
            }
        }

        private int _columnpointer;
        public int ColumnPointer
        {
            get { return _columnpointer; }
            set {
                _columnpointer = value;
                OnPropertyChanged( "ColumnPointer");
            }
        }

        private int _configurationindex;
        public int ConfigurationIndex
        {
            get { return _configurationindex; }
            set {
                _configurationindex = value;
                OnPropertyChanged( "ConfigurationIndex");
            }
        }

        private bool _enableoverview;
        public bool EnableOverview
        {
            get { return _enableoverview; }
            set {
                _enableoverview = value;
                OnPropertyChanged( "EnableOverview");
            }
        }

        private double _previewxscale;
        public double PreviewXScale
        {
            get { return _previewxscale; }
            set {
                _previewxscale = value;
                OnPropertyChanged( "PreviewXScale");
            }
        }

        private double _previewyscale;
        public double PreviewYScale
        {
            get { return _previewyscale; }
            set {
                _previewyscale = value;
                OnPropertyChanged( "PreviewYScale");
            }
        }

        public RelayCommand TakeStaticImageCommand { get; set; }

        public ConfigurationTools()
        {
            InitializeComponent();
            DataContext = this;

            ImageFormatIndex = 1;
            ImageQuality = 10;
            TakeStaticImageCommand = new RelayCommand( ExecuteTakeStaticImageCommand);
        }

        private void ExecuteTakeStaticImageCommand()
        {
            try {
                string filename = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\test.jpg";
                // pull the configuration index settings
                MicroscanReader.DecodeSettings settings = _reader.GetConfigurationIndexSettings(ConfigurationIndex);
                _reader.ShutterSpeed = settings.ShutterSpeed;
                _reader.Gain = settings.Gain;
                _reader.FocalDistanceInches = settings.FocalDistanceInches;
                _reader.NarrowMargins = settings.NarrowMargins;
                _reader.BackgroundColor = settings.BackgroundColor;
                // reader.WOI is set later on!  we have to do it twice, once for the big image, and again for the real WOI
                // reader.SubSampling is also set later on, because I wanted to do massive compression on the preview image
                
                if( EnableOverview) {
                    // take a big, low quality image first
                    //! \todo figure out why ColumnWidth of 2048 resets the reader
                    _reader.WOI = new MicroscanReader.WindowOfInterest( 0, 0, 1536, 1536);
                    //! \todo enum for this stuff
                    const int subsampling = 2;
                    _reader.SubSampling = subsampling;
                    // since we are subsampling at 16:1, we need to scale the width and heights accordingly
                    if( subsampling == 1 || subsampling == 2) {
                        PreviewXScale = PreviewYScale = subsampling * 2;
                    }
                    string bigimage_filename = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\bigimage.jpg";
                    _reader.SaveImage( bigimage_filename, (MicroscanCommands.ImageFormat)ImageFormatIndex, 10);
                    BigImagePath = bigimage_filename;
                } else {
                    BigImagePath = "";
                }

                // take a picture of the WOI
                _reader.WOI = new MicroscanReader.WindowOfInterest( settings.WOI.RowPointer, settings.WOI.ColumnPointer, settings.WOI.RowDepth, settings.WOI.ColumnWidth);
                _reader.SubSampling = settings.SubSampling;
                _reader.SaveImage( filename, (MicroscanCommands.ImageFormat)ImageFormatIndex, ImageQuality);
                SaveImageTime = String.Format( "{0:0.00}", _reader.LastImageTime.TotalSeconds);
                // set up the image preview -- need to set the WOI for the canvas
                RowPointer = settings.WOI.RowPointer;
                ColumnPointer = settings.WOI.ColumnPointer;
                ImagePath = filename;
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
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
}
