using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace BioNex.TipBoxManager
{
    /// <summary>
    /// Interaction logic for TipBoxManagerEditor.xaml
    /// </summary>
    public partial class TipBoxManagerEditor : UserControl
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public ObservableCollection< TipBoxLocationControl> TipBoxLocations { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipBoxManagerEditor( HashSet< TipBoxLocationControl> tip_box_locations)
        {
            TipBoxLocations = new ObservableCollection< TipBoxLocationControl>( tip_box_locations);
            InitializeComponent();
            DataContext = this;
        }
    }
}
