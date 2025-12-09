using System.Windows;
using System.Windows.Input;
using BioNex.BumblebeePlugin.ViewModel;

namespace BioNex.BumblebeePlugin
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    partial class GUI
    {
        private MainViewModel ViewModel { get; set; }
        
        public GUI( Model.MainModel model)
        {
            InitializeComponent();
            ViewModel = new MainViewModel( model, Dispatcher);
            ViewModel.Initialize();
            DataContext = ViewModel;
            diagnostics_toolbar.SetViewModel( ViewModel);
            cycler.SetTechnosoftConnection( model.TechnosoftConnection);
        }

        private void diagnostics_toolbar_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.StartBackgroundUpdates();
        }

        private void diagnostics_toolbar_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.StopBackgroundUpdates();
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if( e.Key == Key.B) {
                if( (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                    (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt &&
                    (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) {

                    ViewModel.MaintenanceTabVisibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// this is used to intercept the spacebar so we can tell the motors to stop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if( e.Key == Key.Space) {
                ViewModel.ExecuteStopAll();
                e.Handled = true;
            }
        }
    }
}
