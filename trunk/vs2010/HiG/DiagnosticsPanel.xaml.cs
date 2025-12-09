using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BioNex.Hig
{
    /// <summary>
    /// Interaction logic for DiagnosticsPanel.xaml
    /// </summary>
    public partial class DiagnosticsPanel : UserControl
    {
        private readonly ViewModel _viewmodel;

        public DiagnosticsPanel( ViewModel viewmodel)
        {
            InitializeComponent();
            DataContext = _viewmodel = viewmodel;
            EngineeringPanel.Model = viewmodel._model;
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if( e.Key == Key.B) {
                if( (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                    (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt &&
                    (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) {

                        var current_visibility = _viewmodel.EngineeringTabVisibility;

                        if (current_visibility == Visibility.Visible)
                            _viewmodel.EngineeringTabVisibility = Visibility.Hidden;
                        else
                            _viewmodel.EngineeringTabVisibility = Visibility.Visible;
                }
            }

        }

    }
}
