using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Systec_IO
{
    public class SimulationIO : InternalIOInterface
    {
        private SimulationIOPanel _panel;
        internal string[] _input_names;
        internal string[] _output_names;

        private int _input_bytes;
        private int _output_bytes;

        public int NumberOfInputs { get { return _input_bytes * 8; } }
        public int NumberOfOutputs { get { return _output_bytes * 8; } }

        public SimulationIO()
        {
        }

        #region InternalIOInterface Members

        public event IOXInputChangedEvent IOXInputChanged { add { } remove { } }

        public int Initialize(int IOX1_base_addr, int device, int channel, int input_bytes = 2, int output_bytes = 1)
        {
            _input_bytes = input_bytes;
            _output_bytes = output_bytes;
            _input_names = new string[NumberOfInputs];
            _output_names = new string[NumberOfOutputs];
            // must have set _input_names and _output_names before constructing SimulationIOPanel
            _panel = new SimulationIOPanel(this);
            _panel.Show();
            return 0;
        }

        public void Close()
        {
            _panel.Close();
        }

        public byte[] ReadInputs()
        {
            int byte_count = NumberOfInputs / 8;
            byte[] inputs = new byte[byte_count];
            for (int i = 0; i < NumberOfInputs; i++)
            {
                if (_panel.GetInputState(i) == IOX1.bit_state.set)
                {
                    int byte_index = i / 8;
                    int bit_index = i % 8;
                    inputs[byte_index] |= (byte)(1 << bit_index);
                }
            }
            return inputs;
        }

        public IOX1.bit_state ReadInput(int channel_0based)
        {
            return _panel.GetInputState(channel_0based);
        }

        public IOX1.bit_state ReadOutput(int channel_0based)
        {
            return _panel.GetOutputState(channel_0based);
        }

        public void WriteOutputs(byte[] outputs)
        {
            for (int i = 0; i < NumberOfOutputs; i++)
            {
                int byte_index = i / 8;
                int bit_index = i % 8;
                _panel.SetOutputState(i, ((outputs[byte_index] | (1 << bit_index)) != 0) ? IOX1.bit_state.set : IOX1.bit_state.clear);
            }
        }

        public void WriteOutput(int channel_0based, IOX1.bit_state state)
        {
            _panel.SetOutputState(channel_0based, state);
        }

        public void ToggleOutput(int channel_0based)
        {
            IOX1.bit_state state = _panel.GetOutputState(channel_0based);
            _panel.SetOutputState(channel_0based, state == IOX1.bit_state.set ? IOX1.bit_state.clear : IOX1.bit_state.set);
        }

        public void SetOutputs(byte[] outputs_bitmask)
        {
            byte[] outputs = GetOutputState();
            for (int i = 0; i < outputs_bitmask.Length; ++i)
                outputs[i] |= outputs_bitmask[i];
            WriteOutputs(outputs);
        }

        public void ClearOutputs(byte[] outputs_bitmask)
        {
            byte[] outputs = GetOutputState();
            for (int i = 0; i < outputs_bitmask.Length; ++i)
                outputs[i] &= (byte)~outputs_bitmask[i];
            WriteOutputs(outputs);
        }

        public byte[] GetOutputState()
        {
            int byte_count = NumberOfOutputs / 8;
            byte[] outputs = new byte[byte_count];
            for (int i = 0; i < NumberOfOutputs; i++)
            {
                if (_panel.GetOutputState(i) == IOX1.bit_state.set)
                {
                    int byte_index = i / 8;
                    int bit_index = i % 8;
                    outputs[byte_index] |= (byte)(1 << bit_index);
                }
            }
            return outputs;
        }

        public void SetInputName(int channel_0based, string name)
        {
            _input_names[channel_0based] = name;
        }

        public void SetOutputName(int channel_0based, string name)
        {
            _output_names[channel_0based] = name;
        }

        #endregion
    }
}
