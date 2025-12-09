using System.Windows.Controls;
using BioNex.Hive.Hardware;
using GalaSoft.MvvmLight.Command;
using BioNex.Shared.Teachpoints;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for NudgeTool.xaml
    /// All of the databinding stuff is in HiveController!
    /// </summary>
    public partial class NudgeTool : UserControl
    {
        public NudgeTool()
        {
            InitializeComponent();
        }
    }

    public partial class HivePlugin
    {
        double _selected_nudge_increment;
        public double SelectedNudgeIncrement
        {
            get { return _selected_nudge_increment; }
            set {
                _selected_nudge_increment = value;
                OnPropertyChanged( "SelectedNudgeIncrement");
            }
        }

        public RelayCommand<string> NudgeCommand { get; set; }

        internal void InitializeNudgeTool()
        {
            NudgeCommand = new RelayCommand<string>( ExecuteNudgeCommand, CanExecuteNudgeCommand);
            SelectedNudgeIncrement = 0.5;
        }

        private void ExecuteNudgeCommand( string descriptor)
        {
            // load the teachpoint info for teachpoint B
            string device_b_name = SelectedDeviceB.Name;

            // nudge the teachpoint as requested
            HiveTeachpoint teachpoint = Hardware.GetTeachpoint( device_b_name, SelectedTeachpointBName);

            switch( descriptor) {
                case "z_up":
                    teachpoint.Z += SelectedNudgeIncrement;
                    break;
                case "z_down":
                    teachpoint.Z -= SelectedNudgeIncrement;
                    break;
                case "x_left":
                    teachpoint.X -= SelectedNudgeIncrement;
                    break;
                case "x_right":
                    teachpoint.X += SelectedNudgeIncrement;
                    break;
                case "y_towards":
                    teachpoint.Y += SelectedNudgeIncrement;
                    break;
                case "y_away":
                    teachpoint.Y -= SelectedNudgeIncrement;
                    break;
            }

            // save the teachpoint
            // need to make "approach_height" and the others constants
            Hardware.SetTeachpoint( device_b_name, teachpoint);
            SaveTeachpointFile( SelectedDeviceB);
            // reload the teachpoint file
            ReloadAllDeviceTeachpoints( true);
            TeachpointNames.MoveCurrentTo( teachpoint.Name);
        }    

        private bool CanExecuteNudgeCommand( string descriptor)
        {
            try
            {
                return SelectedDeviceB != null && SelectedTeachpointB != null;
            }
            catch (TeachpointNotFoundException)
            {
                return false;
            }
        }
    }
}
