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
    ///     Input data contains Row/Sensor Column X Y Z representing scanned ridges clustered by sensor, column , outer list represents N samples
    ///     
    ///     1. For each sensor, we have N sets of data.  Fit these to 2nd orders curves, and find the maxima.
    ///     2. For each N, average the maxima's coordinates from each sensor, leaving us with N points on a curve representing the Arc that X travels through when moving to its extrema
    ///     3. Fit a 2nd order through these N points, this is our Y / X correction curve
    /// 
    ///     
    /// </summary>
    public class LLSXArcCorrectionAlgorithm
    {
        const double SPACING_MM = 9.0;
        PolynomialRegression _fit;
        public PolynomialRegression Fit{get{return _fit;}}

        public LLSXArcCorrectionAlgorithm(IList<IDictionary<Coord, List<Measurement>>> measurements, double filter_threshold, bool show_graphs, Dispatcher dispatcher)
        {
            double[] xs = new double[measurements.Count];
            double[] ys = new double[measurements.Count];
            for (int i = 0; i < measurements.Count; ++i)  // iterate over samples
            {
                // 1.          
                var peak_detect = new LLSPeakDetectAlgorithm(measurements[i], false, filter_threshold, show_graphs, dispatcher);
                var well_maxima = peak_detect.Maxima;
                if (well_maxima.Count == 0)
                {
                    _fit = new PolynomialRegression(new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, 1);
                    return;
                }

                // 2.
                var x = 0.0;
                var y = 0.0;
                var count = 0;
                foreach (var channel in well_maxima.Keys)
                {
                    ++count;
                    x += well_maxima[channel][0].x + (SPACING_MM * channel); // remove jig spacing from x channel measurement, result is the error from teachpoint for each channel
                    y += well_maxima[channel][0].y + (SPACING_MM);           // Y is sampled at COLUMN 1, back out the spacing to get to well zero, result is the error from teachpoint for each channel
                }
                x /= count;
                y /= count;

                xs[i] = x;
                ys[i] = y;
            }
            _fit = new PolynomialRegression(xs, ys, 2);
        }
    }
}
