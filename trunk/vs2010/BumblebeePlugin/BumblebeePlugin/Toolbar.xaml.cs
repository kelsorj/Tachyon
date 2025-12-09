namespace BioNex.BumblebeePlugin
{
    /// <summary>
    /// Interaction logic for Toolbar.xaml
    /// </summary>
    partial class Toolbar
    {
        private ViewModel.MainViewModel ViewModel { get; set; }

        public Toolbar()
        {
            InitializeComponent();
        }

        public void SetViewModel( ViewModel.MainViewModel vm)
        {
            ViewModel = vm;
            DataContext = vm;
        }
    }
}
