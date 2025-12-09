using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using BioNex.Shared.DeviceInterfaces;
using GalaSoft.MvvmLight.Command;
using log4net;

namespace BioNex.Shared.BarcodeMisreadDialog
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class BarcodeMisread : Window, INotifyPropertyChanged
    {
        public ObservableCollection<BarcodeReadErrorInfo> Barcodes { get; private set; }
        public RelayCommand BarcodesResolvedCommand { get; set; }
        private bool UserResolvedEverything { get; set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( BarcodeMisread));

        private Visibility _image_error_visibility;
        public Visibility ImageErrorVisibility
        {
            get { return _image_error_visibility; }
            set {
                _image_error_visibility = value;
                OnPropertyChanged( "ImageErrorVisibility");
            }
        }

        public BarcodeMisread( IEnumerable<BarcodeReadErrorInfo> misread_barcode_info)
        {
            InitializeComponent();
            
            Barcodes = new ObservableCollection<BarcodeReadErrorInfo>();
            BarcodesResolvedCommand = new RelayCommand( ExecuteResolveBarcodes, CanExecuteResolveBarcodes);
            ImageErrorVisibility = Visibility.Collapsed;

            DataContext = this;

            foreach( BarcodeReadErrorInfo info in misread_barcode_info) {
                if( info.ImagePath == null) {
                    _log.InfoFormat( "Could not save image for location '{0}'.  Please reinventory this location again.", info.TeachpointName);
                    ImageErrorVisibility = Visibility.Visible;
                } else if( info.NewBarcode != null && (info.NewBarcode == "##" || LibraryInterfaces.Constants.IsEmpty(info.NewBarcode))) { // post-process the barcode results so we can check boxes and modify textboxes as necessary
                    info.NewBarcode = "";
                    info.NoPlatePresent = true;
                } else {
                    info.NewBarcode = "";
                    info.NoPlatePresent = false;
                }
                Barcodes.Add( info);
            }
        }

        private void ExecuteResolveBarcodes()
        {
            // this actually won't do anything -- when the dialog closes, the caller will take Barcodes
            // and figure out what to do with them.
            UserResolvedEverything = true;
            Close();
        }

        private bool CanExecuteResolveBarcodes()
        {
            return (from x in Barcodes 
                    where (x.NewBarcode != null && x.NewBarcode.Trim().Length != 0) || x.NoPlatePresent
                    select x).Count() == Barcodes.Count();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // only allow the dialog to close if we've determined that the user resolved all
            // of the misreads.  A better way would have been to remove the system menu, but
            // I didn't feel like getting into p/invoke to deal with it right now.
            e.Cancel = !UserResolvedEverything;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
