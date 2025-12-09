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
    ///     Input data contains Row/Sensor Column X Y Z representing scanned positions clustered by sensor, column 
    ///     
    ///     1. Extract data by sensor
    ///     
    ///     2. Fit each sensor's data to a line, finding the slope / intercept for each sensor
    ///     
    ///     3. Take the average slope & intercept, call this your linear equation for x vs z, use the equation to correct Z axis when X is moved.
    ///     
    /// </summary>
    public class LLSYZAlignmentAlgorithm
    {
        public double Slope { get; private set; }
        public double Intercept { get; private set; }

        public LLSYZAlignmentAlgorithm(IDictionary<Coord, List<Measurement>> measurements, double filter_threshold, bool show_graphs, Dispatcher dispatcher)
        {
            // 1.
            var channel_data = new Dictionary<int, List<Measurement>>();
            foreach(var well in measurements.Keys)
            {
                var channel = well.channel;
                if( !channel_data.ContainsKey(channel))
                    channel_data[channel] = new List<Measurement>();
                channel_data[channel].AddRange(measurements[well]);
            }
          
            // 2. - 3.
            double slope = 0;
            double intercept = 0;
            foreach (var channel in channel_data.Keys)
            {
                var zs = channel_data[channel].Select((m) => m.measured_value).ToArray();
                var ys = channel_data[channel].Select((m) => m.y).ToArray();
                var fit = new SimpleLinearRegression(ys, zs); // fit y's to z's to make the final slope / intercept fit our desired outcome of correcting Z for Y motion
                slope += fit.Slope;
                intercept += fit.Intercept;
            }

            Slope = slope / channel_data.Count;
            Intercept = intercept / channel_data.Count;
        }
    }
}
