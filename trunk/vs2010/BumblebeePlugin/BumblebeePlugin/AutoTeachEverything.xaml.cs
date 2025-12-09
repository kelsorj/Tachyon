namespace BioNex.BumblebeePlugin
{
    /// <summary>
    /// Interaction logic for AutoTeachEverything.xaml
    /// </summary>
    public partial class AutoTeachEverything
    {
        private Model.MainModel _model { get; set; }
        public double WorkspaceWidth { get; private set; }

        public AutoTeachEverything( Model.MainModel model)
        {
            InitializeComponent();
            _model = model;
            DataContext = this;
        }
    }
}
