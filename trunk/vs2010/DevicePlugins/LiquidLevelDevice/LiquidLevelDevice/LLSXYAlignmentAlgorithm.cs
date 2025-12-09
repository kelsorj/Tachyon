using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using BioNex.Shared.Utils;
using System.Windows.Threading;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// LLSAlignmentAlgorithm
    ///     Input data contains Row/Sensor Column X Y Z representing scanned ridges clustered by row, column with the scan direction in X and the scan height in Z
    ///     
    ///     1. Filter out data points from each cluster that are more than a fixed threshold distance away from the max value (4mm seems to be the sweet spot)
    ///        This throws away points that aren't on the top of the ridge.  Including "shoulder" points leads to a worse fit in step 2.
    ///        
    ///     2. Fit each cluster to a 2nd order curve
    ///     
    ///     3. Find the maxima of the curve (dy/dx = 0) and call this the center of the ridge
    ///     
    ///     4. Re-cluster by Row / Sensor, fit a line to the maxima for each Row - this produces a slope / intercept equation for that Sensor
    ///     
    ///     5. Take the average slope & intercept, call this your linear equation for x vs y, use the equation to correct X axis when Y is moved.
    ///     
    /// </summary>
    public class LLSXYAlignmentAlgorithm
    {
        public double Slope { get; private set; }
        public double Intercept { get; private set; }

        public LLSXYAlignmentAlgorithm(IDictionary<Coord, List<Measurement>> measurements, double filter_threshold, bool show_graphs, Dispatcher dispatcher)
        { 
            // 1. - 3.
            var peak_detect = new LLSPeakDetectAlgorithm(measurements, true, filter_threshold, show_graphs, dispatcher);
            var well_maxima = peak_detect.Maxima;
           
            // 4.
            double slope = 0;
            double intercept = 0;
            foreach (var channel in well_maxima.Keys)
            {
                var xs = well_maxima[channel].Select((m) => m.x).ToArray();
                var ys = well_maxima[channel].Select((m) => m.y).ToArray();
                var fit = new SimpleLinearRegression(ys, xs); // fit y's to x's to make the final slope / intercept fit our desired outcome of correcting X for Y motion
                slope += fit.Slope;
                intercept += fit.Intercept;
            }

            Slope = slope / well_maxima.Count;
            Intercept = intercept / well_maxima.Count;

            if (Double.IsNaN(Slope) || Double.IsNaN(Intercept))
            {
                Slope = 0.0;
                Intercept = 0.0;
            }
        }
    }
}
