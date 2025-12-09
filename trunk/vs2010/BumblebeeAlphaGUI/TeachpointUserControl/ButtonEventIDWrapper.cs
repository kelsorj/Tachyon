using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeachpointUserControl
{
    class ButtonEventIDWrapper
    {
        public byte Channel { get; set; }
        public byte Stage { get; set; }
        public string ButtonName { get; set; }

        public ButtonEventIDWrapper( byte channel, byte stage, string name)
        {
            Channel = channel;
            Stage = stage;
            ButtonName = name;
        }
    }
}
