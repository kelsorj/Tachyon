using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;

namespace BioNex.Shared.Microscan
{
    /// <summary>
    /// Interaction logic for DynamicImaging.xaml
    /// </summary>
    public partial class DynamicImaging : UserControl
    {
        internal MicroscanReader _reader;

        public RelayCommand DeleteAllImagesCommand { get; set; }
        public RelayCommand DownloadSelectedImageCommand { get; set; }
        public RelayCommand RefreshImagesCommand { get; set; }

        public ObservableCollection<MicroscanFilename> ReaderImages { get; set; }

        public DynamicImaging()
        {
            InitializeComponent();
            DataContext = this;

            ReaderImages = new ObservableCollection<MicroscanFilename>();

            DeleteAllImagesCommand = new RelayCommand( ExecuteDeleteAllImages);
            DownloadSelectedImageCommand = new RelayCommand( ExecuteDownloadSelectedImage);
            RefreshImagesCommand = new RelayCommand( ExecuteRefreshImagesCommand);
        }

        private void ExecuteDeleteAllImages()
        {
            _reader.DeleteAllImages();
        }

        private void ExecuteDownloadSelectedImage()
        {
        }

        private void ExecuteRefreshImagesCommand()
        {
            ReaderImages.Clear();

            // get a list of the images in the reader
            List<MicroscanFilename> image_filenames = _reader.GetImageFilenames();

            // download each file and store the file in the temp folder
            //ImagePath = System.IO.Path.GetTempPath() + TeachpointName.Replace( ':', '-') + ".jpg";
            foreach( var x in image_filenames) {
                try {
                    string filename = _reader.DownloadImage( x, 0.25, 1);
                    x.Source = filename;
                } catch( Exception ex) {
                    string message = String.Format( "Could not download file '{0}' from the barcode reader: {1}", x, ex.Message);
                    MessageBox.Show( message);
                }
            }

            //! \todo update the list with preview images -- but for now, the image object used will just have a ToString()
            //!       that lets the control at least list the images by name
            foreach( var x in image_filenames) {
                ReaderImages.Add( x);
            }
        }

        private void TextBlock_SourceUpdated(object sender, DataTransferEventArgs e)
        {

        }
    }
}
