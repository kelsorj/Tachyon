using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using System.Windows.Threading;

namespace BioNex.LiquidLevelDevice
{
    public class LLSPeakDetectAlgorithm
    {
        public struct pair<T>
        {
            public T x;
            public T y;
        };

        public IDictionary<int, List<pair<double>>> Maxima { get; private set; }


        // passed in measurements represent a sample of data along a line:
        //   - either x or y is varying while the other remains fixed
        //   - Z value represents the measurement at the x, y coordinate
        // for each channel,
        // 1. fit a 2nd order polynomial that fits the varying coordinate (x or y) with the measurement (z)

        public LLSPeakDetectAlgorithm(IDictionary<Coord, List<Measurement>> measurements, bool x_varies, double filter_threshold, bool show_graphs, Dispatcher dispatcher)
        {
            GraphWindow graph = null;
            Maxima = new SortedDictionary<int, List<pair<double>>>();
            foreach (var well in measurements.Keys)
            {
                // 1. filter out values that are more than a threshold distance away from the max value for this well -- this attempts to isolate the 'peak' and remove any 'shoulder'
                var filter_value = measurements[well].Max((m) => m.measured_value) - filter_threshold;
                var filtered_measurements = new List<Measurement>();
                foreach (var measurement in measurements[well])
                {
                    if (measurement.measured_value >= filter_value)
                        filtered_measurements.Add(measurement);
                }
                if (filtered_measurements.Count == 0)
                    continue;

                // 2. convert the measurement for this well into an X and Z array to hand off to the regression algorithm
                var xs = filtered_measurements.Select((m) => x_varies ? m.x : m.y).ToArray();
                var zs = filtered_measurements.Select((m) => m.measured_value).ToArray();
                var fit = new PolynomialRegression(xs, zs, 2);

                if (show_graphs)
                {
                    Action action = () =>
                    {
                        if (graph == null)
                        {
                            var title = string.Format("Peak Detect Column {0} '{1}'", well.column, x_varies ? "X vs. Z" : "Y vs. Z");
                            graph = new GraphWindow(title);
                        }

                        for (int i = 0; i < xs.Length; ++i)
                            graph.Graph.AddPoint(xs[i], zs[i], string.Format("{0},{1}", well.channel, well.column));

                        var min_x = xs.Min();
                        var max_x = xs.Max();
                        for( int i=0; i<100; ++i)
                        {
                            var x = min_x + i * ((max_x -  min_x) / 99.0);
                            var z = fit.FitPoint(x);
                            graph.Graph.AddPoint(x, z, string.Format("fit {0},{1}", well.channel, well.column));
                        }
                        graph.Show();
                    };
                    if (dispatcher.CheckAccess()) action(); else dispatcher.Invoke(action);
                }

                // 3. Locate the maxima
                // dy/dx = 0 for a 2nd order polynomial is -C[1] / (2*C[2])  i.e. d/dx AX^2 + BX + C = 2AX + B so if 2AX + B = 0, X = B / 2A
                var maxima = -fit.Coefficients[1] / (2 * fit.Coefficients[2]);
                
                // 4. Save the x & y coordinates of the peak value for this well.  
                //    The result is a list of peaks for each channel, which can be used to determine a line on that channel in the Alignment algorithm
                //    The Teachpoint locator algorithm gets a list with 1 value, since it's only scanning one well

                var avg_y = filtered_measurements.Select((m) => x_varies ? m.y : m.x).Average();
                if (!Maxima.ContainsKey(well.channel))
                    Maxima[well.channel] = new List<pair<double>>();
                Maxima[well.channel].Add(new pair<double>() { x = x_varies ? maxima : avg_y, y = x_varies ? avg_y : maxima });
            }
        }
    }
}
