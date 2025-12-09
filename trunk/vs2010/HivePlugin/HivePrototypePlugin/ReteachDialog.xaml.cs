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
using System.Windows.Shapes;

namespace BioNex.HivePrototypePlugin
{
    // using anonymous types instead
    // UPDATE - no longer using anonymous types because I needed to add an IsSelected property
    //          to allow multi-selection with MVVM
    /// <summary>
    /// this allows me to store old and new information about a teachpoint so the
    /// user can preview the changes before applying the offset to all teachpoints.
    /// </summary>
    public class ReteachPreviewInfo
    {
        public string Name { get; set; }
        public double OldX { get; set; }
        public double OldY { get; set; }
        public double OldZ { get; set; }
        public double NewX { get; set; }
        public double NewY { get; set; }
        public double NewZ { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Interaction logic for ReteachDialog.xaml
    /// </summary>
    public partial class ReteachDialog : Window
    {
        // wish there was some way to use Nullable here
        public enum Selection { ApplyAll, ApplySelected, DoNotApply }

        public Selection UserSelection { get; set; }

        public ReteachDialog( HivePlugin vm)
        {
            UserSelection = Selection.DoNotApply;
            InitializeComponent();
            DataContext = vm;
        }

        private void ApplyAll_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = Selection.ApplyAll;
            Hide();
        }

        private void ApplySelected_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = Selection.ApplySelected;
            Hide();
        }

        private void DoNotApply_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = Selection.DoNotApply;
            Hide();
        }
    }
}
