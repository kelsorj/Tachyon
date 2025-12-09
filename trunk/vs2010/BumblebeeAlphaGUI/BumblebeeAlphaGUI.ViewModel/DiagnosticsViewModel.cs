using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.BumblebeeAlphaGUI.ViewModel
{
    public class DiagnosticsViewModel : BaseViewModel
    {
        BumblebeeAlphaGUI.Model.Model _model;

        public DiagnosticsViewModel( BumblebeeAlphaGUI.Model.Model model)
        {
            _model = model;
        }

        public byte GetNumberOfChannels()
        {
            return _model.GetNumberOfChannels();
        }

        public byte GetNumberOfStages()
        {
            return _model.GetNumberOfStages();
        }
    }
}
