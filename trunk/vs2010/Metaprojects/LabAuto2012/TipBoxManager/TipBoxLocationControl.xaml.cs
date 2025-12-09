using System.ComponentModel;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Command;

namespace BioNex.TipBoxManager
{
    /// <summary>
    /// Interaction logic for TipBoxLocationControl.xaml
    /// </summary>
    public partial class TipBoxLocationControl : UserControl, INotifyPropertyChanged
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum LocationStatus
        {
            Empty = 0,
            New,
            InUse,
            Used,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public string Location { get; set; }
        private LocationStatus _status;
        public LocationStatus Status{
            get{
                return _status;
            }
            set{
                _status = value;
                OnPropertyChanged( "Status");
            }
        }
        public object LockObject { get; set; }
        public RelayCommand SetNewCommand { get; set; }
        public RelayCommand SetEmptyCommand { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipBoxLocationControl( string location, LocationStatus status, object lock_object)
        {
            Location = location;
            Status = status;
            LockObject = lock_object;
            InitializeCommands();
            InitializeComponent();
            DataContext = this;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeCommands()
        {
            SetNewCommand = new RelayCommand( ExecuteSetNew, CanExecuteSetNew);
            SetEmptyCommand = new RelayCommand( ExecuteSetEmpty, CanExecuteSetEmpty);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteSetNew()
        {
            lock( LockObject){
                return Status == LocationStatus.Empty || Status == LocationStatus.Used;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteSetNew()
        {
            lock( LockObject){
                Status = LocationStatus.New;
            }
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteSetEmpty()
        {
            lock( LockObject){
                return Status == LocationStatus.New || Status == LocationStatus.Used;
            }
        }
        // ----------------------------------------------------------------------
        private void ExecuteSetEmpty()
        {
            lock( LockObject){
                Status = LocationStatus.Empty;
            }
        }
        // ----------------------------------------------------------------------
        #region INotifyPropertyChanged Members
        // ----------------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;
        // ----------------------------------------------------------------------
        public void OnPropertyChanged( string property_name)
        {
            if ( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }
        // ----------------------------------------------------------------------
        #endregion
    }
}
