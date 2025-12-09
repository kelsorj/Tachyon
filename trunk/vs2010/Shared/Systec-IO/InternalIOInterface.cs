using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Systec_IO
{
    public delegate void IOXInputChangedEvent( object sender, IOXEventArgs e);

    public class IOXEventArgs : EventArgs
    {
        public List<int> BitIndexes { get; private set; }
        public List<bool> BitValues { get; private set; }

        public IOXEventArgs( List<int> bit_indexes, List<bool> new_values)
        {
            Debug.Assert( bit_indexes.Count() == new_values.Count(), "The number of bits that reported a state change doesn't match the number of new states");
            BitIndexes = new List<int>();
            BitIndexes.AddRange( bit_indexes);
            BitValues = new List<bool>();
            BitValues.AddRange( new_values);
        }
    }

    public interface InternalIOInterface
    {
        event IOXInputChangedEvent IOXInputChanged;
        int NumberOfInputs { get; }
        int NumberOfOutputs { get; }

        int Initialize(int IOX1_base_addr, int device, int channel, int input_bytes=2, int output_bytes=1);

        void Close();
        byte[] ReadInputs();
        Systec_IO.IOX1.bit_state ReadInput(int channel_0based);
        Systec_IO.IOX1.bit_state ReadOutput( int channel_0based);
        void WriteOutputs(byte[] outputs_8bits);
        void WriteOutput(int channel_0based, Systec_IO.IOX1.bit_state state);
        void ToggleOutput(int channel_0based);
        void SetOutputs(byte[] outputs_bitmask);
        void ClearOutputs(byte[] outputs_bitmask);
        byte[] GetOutputState();
        void SetInputName( int channel_0based, string name);
        void SetOutputName( int channel_0based, string name);
    }
}
