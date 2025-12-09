using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Amulet
{
    public class AmuletCommandEventArgs : EventArgs
    {
        public byte Command { get; private set; }
        public byte[] Variable { get; private set; }
        public byte[] Value { get; private set; }

        public AmuletCommandEventArgs( byte command, byte[] variable_and_value)
        {
            Command = command;
            // parse out the variable and value information from variable_and_value
            // variable is always 2 characters (forms 1 byte)
            Variable = new byte[] { variable_and_value[0], variable_and_value[1] };

            // get the value information
            int value_length = variable_and_value.Length - 2;
            if( value_length > 0) {
                int index = 0;
                Value = new byte[value_length];
                for( int i=2; i<variable_and_value.Length; i++) {
                    Value[index++] = variable_and_value[i];
                }
            }
        }
    }
}
