using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.BumblebeeAlphaGUI.Model
{
    public class ProgressValue
    {
        public int Value { get; private set; }
        public ProgressValue( int value)
        {
            Value = value;
        }
    }

    public class ProgressMax
    {
        public int Maximum { get; private set; }
        public ProgressMax( int max)
        {
            Maximum = max;
        }
    }
}
