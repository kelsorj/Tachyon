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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;

namespace Systec_IO
{
    public class ToggleButtonWrapper : INotifyPropertyChanged
    {
        private bool _is_checked;
        public bool IsChecked
        {
            get { return _is_checked; }
            set {
                _is_checked = value;
                OnPropertyChanged( "IsChecked");
            }
        }

        private string _label;
        public string Label
        {
            get { return _label; }
            set {
                _label = value;
                OnPropertyChanged( "Label");
            }
        }

        public ToggleButtonWrapper( string text)
        {
            Label = text;
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler  PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }
        #endregion
    }

    /// <summary>
    /// Interaction logic for SimulationIOPanel.xaml
    /// </summary>
    public partial class SimulationIOPanel : Window
    {
        public List<ToggleButtonWrapper> Inputs { get; set; }
        public List<ToggleButtonWrapper> Outputs { get; set; }

        public SimulationIOPanel( SimulationIO io)
        {
            InitializeComponent();
            DataContext = this;

            Inputs = new List<ToggleButtonWrapper>();
            for( int i=0; i<io.NumberOfInputs; i++) {
                Inputs.Add( new ToggleButtonWrapper( io._input_names[i] ?? (i + 1).ToString()));
            }
            
            Outputs = new List<ToggleButtonWrapper>();
            for( int i=0; i<io.NumberOfOutputs; i++) {
                Outputs.Add( new ToggleButtonWrapper( io._output_names[i] ?? (i + 1).ToString()));
            }
        }

        public IOX1.bit_state GetInputState( int index)
        {
            return (IOX1.bit_state)(Inputs[index].IsChecked ? IOX1.bit_state.set : IOX1.bit_state.clear);
        }

        public IOX1.bit_state GetOutputState( int index)
        {
            return (IOX1.bit_state)(Outputs[index].IsChecked ? IOX1.bit_state.set : IOX1.bit_state.clear);
        }

        public void SetInputState( int index, IOX1.bit_state state)
        {
            Inputs[index].IsChecked = state == IOX1.bit_state.set ? true : false;
        }

        public void SetOutputState( int index, IOX1.bit_state state)
        {
            Outputs[index].IsChecked = state == IOX1.bit_state.set ? true : false;
        }
    }
}
