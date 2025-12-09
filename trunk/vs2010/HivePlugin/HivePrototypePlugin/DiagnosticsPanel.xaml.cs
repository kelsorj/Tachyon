using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for DiagnosticsPanel.xaml
    /// </summary>
    public partial class DiagnosticsPanel : UserControl
    {
        private readonly HivePlugin _controller;
        //private uint msgRecv = 0;

        public DiagnosticsPanel( HivePlugin controller)
        {
            // BioNex.Shared.Utils.Kinematics.SCurve.Test();

            InitializeComponent();
            _controller = controller;
            joghome_control.Controller = controller;
            teach_control.Controller = controller;
            engineering_control.Controller = controller;
            cycler.SetTechnosoftConnection( controller.Hardware.TechnosoftConnection);

            // set up auto-teach panel
            autoteach_control.Plugin = _controller;
            //! \todo this needs to be done differently -- for now, I am assuming that we only have one IO device in the system.
            autoteach_control.IO = controller.DataRequestInterface.Value.GetIOInterfaces().FirstOrDefault();

            // need to pass plugin to teachpoint cycler dialog as well
            cycler_control.Plugin = _controller;

            Debug.Assert( controller.DataRequestInterface != null);
            DataContext = controller;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InventoryView inventory_view = new InventoryView( _controller);
            inventory_tab.DataContext = _controller;
            inventory_tab.Content = inventory_view;
            //_controller.StatusCache.Start();
            // refs #542 although diags shows 20, it wasn't actually changing the property so I am forcing it here
            _controller.Speed = _controller.MaxAllowableSpeed;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            //_controller.StatusCache.Stop();
            // refs #542
            _controller.Speed = 100;
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if( e.Key == Key.B) {
                if( (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                    (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt &&
                    (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) {

                        var current_visibility = _controller.EngineeringTabVisibility;

                        if (current_visibility == Visibility.Visible)
                        {
                            _controller.EngineeringTabVisibility = Visibility.Hidden;
                            _controller.MaintenanceTabVisibility = Visibility.Hidden;
                            _controller.ShowAllSpeeds( false);
                        }
                        else
                        {
                            _controller.EngineeringTabVisibility = Visibility.Visible;
                            _controller.MaintenanceTabVisibility = Visibility.Visible;
                            _controller.ShowAllSpeeds( true);
                        }
                }
            }
        }
    }
}
