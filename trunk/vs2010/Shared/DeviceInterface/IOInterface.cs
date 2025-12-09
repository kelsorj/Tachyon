using System;
using System.Collections.Generic;

namespace BioNex.Shared.DeviceInterfaces
{
    public delegate void InputChangedEventHandler( object sender, InputChangedEventArgs e);
    public class InputChangedEventArgs : EventArgs
    {
        public int BitIndex { get; private set; }
        public bool BitState { get; private set; }

        public InputChangedEventArgs( int bit_index, bool new_bit_state)
        {
            BitIndex = bit_index;
            BitState = new_bit_state;
        }
    }

    public class BitNameMapping
    {
        public string BitName { get; set; }
        public int BitNumber { get; set; }
    }

    public interface IOInterface
    {

        event InputChangedEventHandler InputChanged;

        /// <summary>
        /// how many inputs the device supports
        /// </summary>
        int NumberOfInputs { get; }
        /// <summary>
        /// how many outputs the device supports
        /// </summary>
        int NumberOfOutputs { get; }
        /// <summary>
        /// set the specified bit's state
        /// </summary>
        /// <param name="bit_index">0-based output to set</param>
        /// <param name="state">true = on, false = off</param>
        void SetOutputState( int bit_index, bool state);
        /// <summary>
        /// sets masked bits
        /// </summary>
        /// <param name="bitmask">1 = on, 0 = off</param>
        void SetOutputs( byte[] bitmask);
        /// <summary>
        /// clears masked bits
        /// </summary>
        /// <param name="bitmask">1 = on, 0 = off</param>
        void ClearOutputs( byte[] bitmask);
        /// <summary>
        /// retrieves the state of the specified input
        /// </summary>
        /// <param name="bit"></param>
        /// <returns></returns>
        bool GetInput( int bit);
        /// <summary>
        /// retrieves the states of all inputs
        /// </summary>
        /// <returns></returns>
        byte[] GetInputs();
        /// <summary>
        /// gets all friendly input names
        /// </summary>
        /// <returns></returns>
        List<BitNameMapping> GetInputNames();
        /// <summary>
        /// gets all friendly output names
        /// </summary>
        /// <returns></returns>
        List<BitNameMapping> GetOutputNames();
    }
}
