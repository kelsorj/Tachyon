using System;
using System.Collections.Generic;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class PiecewiseLinearFunction : SortedList< double, double>
    {
        public PiecewiseLinearFunction() {}

        public PiecewiseLinearFunction( IDictionary<double,double> data)
            : base( data)
        {
        }

        public class InputOutOfRangeException : Exception
        {
        }

        public void Replace( double input, double output)
        {
            Remove( input);
            Add( input, output);
        }

        /// <summary>Get this piecewise-linear function's domain (minimum and maximum inputs).
        /// </summary>
        /// <param name="min_input">This piecewise-linear function's minimum input.</param>
        /// <param name="max_input">This piecewise-linear function's maximum input.</param>
        public void GetDomain( out double min_input, out double max_input)
        {
            min_input = ( Count == 0) ? double.MaxValue : Keys[ 0];
            max_input = ( Count == 0) ? double.MinValue : Keys[ Count - 1];
        }

        /// <summary>Get this piecewise-linear function's output for a given input.
        /// </summary>
        /// <param name="input">The input for which to get the piecewise-linear function's output.</param>
        /// <param name="is_interpolated">True if the output is the result of interpolation between the piecewise-linear function's data.</param>
        /// <param name="lo_input">If interpolated, the input's low neighbor.</param>
        /// <param name="hi_input">If interpolated, the input's high neighbor.</param>
        /// <param name="lo_output">If interpolated, the input's low neighbor's output.</param>
        /// <param name="hi_output">If interpolated, the input's high neighbor's output.</param>
        /// <returns>The piecewise-linear function's output corresponding to the given input.</returns>
        /// <exception cref="InputOutOfRangeException">Thrown when the given input is not within this piecewise-linear function's domain.</exception>
        public double GetOutput( double input, out bool is_interpolated, out double lo_input, out double hi_input, out double lo_output, out double hi_output)
        {
            // initialize out variables.
            is_interpolated = true;
            lo_input = double.MaxValue;
            hi_input = double.MinValue;
            lo_output = double.MaxValue;
            hi_output = double.MinValue;

            // make sure input is within the domain of the function.
            double min_input = 0, max_input = 0;
            GetDomain( out min_input, out max_input);
            if(( input < min_input) || ( input > max_input)){
                throw new InputOutOfRangeException();
            }

            // an exact solution is available; return it.
            if( ContainsKey( input)){
                double output = this[ input];
                is_interpolated = false;
                lo_input = hi_input = input;
                lo_output = hi_output = output;
                return output;
            }

            // find and return an interpolated solution.
            // below, i use a binary search to find the index (lo_index) where lo_input and lo_output can be found.
            // min_index and max_index are the minimum and maximum indices where the lo_index may reside.
            int min_index = 0;
            int max_index = Count - 2;
            int lo_index = 0;
            // loop until lo_index is identified.
            while( true){
                // try lo_index between min_index and max_index.
                lo_index = ( min_index + max_index) / 2;
                if( Keys[ lo_index] < input){
                    // if lo_index is indeed located at an input that is lower than "input"...
                    if( Keys[ lo_index + 1] > input){
                        // if lo_index's neighbor to the "right" is higher than "input", then we've found our lo_index.
                        break;
                    } else{
                        // otherwise, the minimum index must be greater than this attempt at lo_index.
                        min_index = lo_index + 1;
                    }
                } else{
                    // otherwise, the maximum index must be less than this attempt at lo_index.
                    max_index = lo_index - 1;
                }
            }
            // we will exit the loop with lo_index at the lo_input and lo_output.
            // hi_input and hi_output must be to the "right" of lo_index.
            lo_input = Keys[ lo_index];
            hi_input = Keys[ lo_index + 1];
            lo_output = Values[ lo_index];
            hi_output = Values[ lo_index + 1];
            double ratio_from_lo_to_hi = (( input - lo_input) / ( hi_input - lo_input));
            return lo_output + ( ratio_from_lo_to_hi * ( hi_output - lo_output));
        }

        /// <summary>Get this piecewise-linear function's output for a given input.
        /// </summary>
        /// <param name="input">The input for which to get the piecewise-linear function's output.</param>
        /// <param name="is_interpolated">True if the output is the result of interpolation between the piecewise-linear function's data.</param>
        /// <returns>The piecewise-linear function's output corresponding to the given input.</returns>
        /// <exception cref="InputOutOfRangeException">Thrown when the given input is not within this piecewise-linear function's domain.</exception>
        public double GetOutput( double input, out bool is_interpolated)
        {
            double lo_input = 0.0;
            double hi_input = 0.0;
            double lo_output = 0.0;
            double hi_output = 0.0;
            return GetOutput( input, out is_interpolated, out lo_input, out hi_input, out lo_output, out hi_output);
        }

        /// <summary>Get this piecewise-linear function's output for a given input.
        /// </summary>
        /// <param name="input">The input for which to get the piecewise-linear function's output.</param>
        /// <returns>The piecewise-linear function's output corresponding to the given input.</returns>
        /// <exception cref="InputOutOfRangeException">Thrown when the given input is not within this piecewise-linear function's domain.</exception>
        public double GetOutput( double input)
        {
            bool is_interpolated = false;
            return GetOutput( input, out is_interpolated);
        }
    }
#endif
}
