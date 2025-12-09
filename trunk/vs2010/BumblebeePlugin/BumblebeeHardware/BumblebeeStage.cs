using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.BumblebeePlugin.Hardware2
{
    public class BumblebeeStage
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        internal IAxis YAxis { get; set; }
        internal IAxis RAxis { get; set; }

        private Stage Stage { get; set; } // to be deprecated.

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public BumblebeeStage( IAxis y_axis, IAxis r_axis)
        {
            YAxis = y_axis;
            RAxis = r_axis;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void HomeY()
        {
        }
        // ----------------------------------------------------------------------
        public void HomeR()
        {
        }
        // ----------------------------------------------------------------------
        public void Home()
        {
        }

        // ----------------------------------------------------------------------
        // internal methods.
        // ----------------------------------------------------------------------
    }
}
