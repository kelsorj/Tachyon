using System.Windows;
using GalaSoft.MvvmLight.Command;

namespace BioNex.Shared.LiquidProfileLibrary
{
    /// <summary>
    /// Interaction logic for NewObject.xaml
    /// </summary>
    public partial class NewObjectNameWindow : Window
    {
        public string TitleString { get; set; }
        public string LabelString { get; set; }
        public string NameString { get; set; }

        public RelayCommand OKCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        public string NewName { get; set; }

        public NewObjectNameWindow( string title, string label, string name)
        {
            TitleString = title;
            LabelString = label;
            NameString = name;

            InitializeComponent();

            DataContext = this;

            OKCommand = new RelayCommand( DoOK );
            CancelCommand = new RelayCommand( DoCancel );
        }

        void DoOK()
        {
            NewName = NameString;
            Close();
        }

        void DoCancel()
        {
            Close();
        }
    }
}
