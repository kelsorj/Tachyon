using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;

namespace BioNex.IOPlugin
{
    /// <summary>
    /// Interaction logic for DiagnosticsPanel.xaml
    /// </summary>
    public partial class DiagnosticsPanel : UserControl
    {
        private IO Controller { get; set; }

        public InputLEDView[] Inputs { get; set; }
        public ToggleButton[] Outputs { get; set; }

        private DispatcherTimer Timer { get; set; }

        // temp... get rid of this when we add hardware
        private bool LastState { get; set; }

        public DiagnosticsPanel( IO controller)
        {
            InitializeComponent();
            Controller = controller;
            InitializeInputsAndOutputs();

            DataContext = this;

            // timer will deal with GUI updates from sensors
            Timer = new DispatcherTimer();
            Timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
            Timer.Tick += new EventHandler(Timer_Tick);
        }

        private void ExecuteOutputCommand( object index)
        {
            MessageBox.Show( index.ToString());
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            try {
                for( int i=0; i<Controller.NumberOfInputs; i++)
                    Inputs[i].State = Controller.GetInput( i);
                for( int i=0; i<Controller.NumberOfOutputs; i++)
                    Outputs[i].IsChecked = Controller.GetOutput( i);
            } catch( Exception) {
                // fail silently
            }
        }

        private void InitializeInputsAndOutputs()
        {
            Inputs = new InputLEDView[Controller.NumberOfInputs];
            var input_names = Controller.GetInputNames();
            for( int i=0; i<Controller.NumberOfInputs; i++) {
                // if there is a name for the input, use it.  Otherwise, just use the number
                var search_result = input_names.Where( x => x.BitNumber == i + 1).FirstOrDefault();
                if( search_result == null) {
                    Inputs[i] = new InputLEDView { InputLabel = (i + 1).ToString(), State = false };
                } else {
                    Inputs[i] = new InputLEDView { InputLabel = search_result.BitName, State = false };
                }
            }
            Outputs = new ToggleButton[Controller.NumberOfOutputs];
            var output_names = Controller.GetOutputNames();
            for( int i=0; i<Controller.NumberOfOutputs; i++) {
                // if there is a name for the output, use it.  Otherwise, just use the number
                var search_result = output_names.Where( x => x.BitNumber == i + 1).FirstOrDefault();
                ToggleButton tb = new ToggleButton();
                string label;
                if( search_result == null) {
                    label = String.Format( "Output {0}", i + 1);
                } else {
                    label = search_result.BitName;
                }
                tb.Content = String.Format( label);
                tb.Width = tb.Height = 85;
                tb.Margin = new Thickness( 5);
                tb.Click += new RoutedEventHandler(tb_Click);
                Outputs[i] = tb;
            }
        }

        void tb_Click(object sender, RoutedEventArgs e)
        {
            // get the name and strip off the number
            ToggleButton button = sender as ToggleButton;
            if( button == null)
                return;
            string name = button.Content.ToString();
            int bit_index = int.Parse( name.Substring( "Output ".Length)) - 1;
            // also need to get the current state
            bool? current_state = button.IsChecked;
            // now set the opposite state
            Controller.SetOutputState( bit_index, current_state.Value);
            //button.IsChecked = !current_state;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Timer.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Timer.Stop();
        }
    }

    public class InputOutput : UserControl
    {
        private IO Controller { get; set; }

        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register( "Index", typeof(Int32), typeof(InputOutput));
        public static readonly DependencyProperty StateProperty = DependencyProperty.Register( "State", typeof(Boolean), typeof(InputOutput));

        public RelayCommand<object> OutputCommand { get; set; }

        public int Index
        {
            get { return (Int32)GetValue( IndexProperty); }
            set { SetValue( IndexProperty, (Int32)value); }
        }
        
        public bool State
        {
            get { return (Boolean)GetValue( StateProperty); }
            set { SetValue( StateProperty, (Boolean)value); }
        }

        public InputOutput( IO controller, int index, bool initial_state)
        {
            Controller = controller;
            Index = index;
            State = initial_state;
            OutputCommand = new RelayCommand<object>( ExecuteOutputCommand);

            DataContext = this;
        }

        private void ExecuteOutputCommand( object state)
        {
            Controller.SetOutputState( Index, (bool)state);
        }
    }
}
