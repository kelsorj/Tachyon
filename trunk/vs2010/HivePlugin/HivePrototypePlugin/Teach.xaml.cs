using System.Windows.Controls;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for Teach.xaml
    /// </summary>
    public partial class Teach : UserControl
    {
        private HivePlugin _controller;
        public HivePlugin Controller
        {
            get { return _controller; }
            set {
                _controller = value;
                DataContext = _controller;
            }
        }

        public Teach()
        {
            InitializeComponent();
        }
    }
}
