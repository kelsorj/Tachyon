using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace BioNex.LiquidLevelDevice
{
    class LLSLocateTeachpointAlgorithm
    {
        public double XOffset { get; private set; }
        public double YOffset { get; private set; }

        public double[] XDeviation { get; private set; }
        public double[] YDeviation { get; private set; }

        public bool IsAcceptable { get; private set; }

        public LLSLocateTeachpointAlgorithm(double x_spacing, double y_offset, IDictionary<Coord, List<Measurement>> x_measurements, IDictionary<Coord, List<Measurement>> y_measurements, double filter_threshold, bool show_graphs, Dispatcher dispatcher)
        {
            double X_SPACING_MM = x_spacing; // spacing of JIG, not labware
            double Y_SPACING_MM = y_offset;
            // we've scanned a crosshair with all channels in X & Y directions
            // 
            // 1. Locate the peaks in the scan, this is the well dead center for each channel

            var x_peak_detect = new LLSPeakDetectAlgorithm(x_measurements, true, filter_threshold, show_graphs, dispatcher);
            var x_well_maxima = x_peak_detect.Maxima;
            var y_peak_detect = new LLSPeakDetectAlgorithm(y_measurements, false, filter_threshold, show_graphs, dispatcher);
            var y_well_maxima = y_peak_detect.Maxima;

            var keys = x_well_maxima.Keys.ToArray();
            var xs = new double[x_well_maxima.Count];
            for (int i = 0; i < xs.Length; ++i)
                xs[i] = x_well_maxima[keys[i]][0].x + (X_SPACING_MM * i); // remove jig spacing from x channel measurement, result is the error from teachpoint for each channel

            var ys = new double[y_well_maxima.Count];
            for(int i=0; i<ys.Length; ++i)
                ys[i] = y_well_maxima[keys[i]][0].y + (Y_SPACING_MM); // Y is sampled at COLUMN 1, back out the spacing to get to well zero, result is the error from teachpoint for each channel

            // return the average offset of all channels
            XOffset = xs.Average();
            YOffset = ys.Average();

            // If Teachpoint is accepted, save Xs and Ys to database as individual sensor offsets for rejection radius calculation
            // subtract X & Y offset first so that deviation is relative to the new teachpoint
            // I expect the sign to be oriented such that we subtract deviation to move to that position, so add deviation to tell where the sensor is relative to current position
            // However, there is a mystery sign flip somewhere, so we experimental results show that we need to Subtract deviation to move to position, and Adding deviation gives the correct
            // offset when trying to get a corrected position.  I am confused.
            bool x_dev_nan = false;
            for (int i = 0; i < xs.Length; ++i)
            {
                xs[i] = XOffset - xs[i];
                x_dev_nan |= double.IsNaN(xs[i]);
            }
            bool y_dev_nan = false;
            for (int i = 0; i < ys.Length; ++i)
            {
                ys[i] = YOffset - ys[i];
                y_dev_nan |= double.IsNaN(ys[i]);
            }

            XDeviation = xs;
            YDeviation = ys;

            IsAcceptable = !(double.IsNaN(XOffset) || double.IsNaN(YOffset) || x_dev_nan || y_dev_nan);
        }
    }
}
