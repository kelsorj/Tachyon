using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Utils
{
    public class PolynomialRegression
    {
        public double[] Coefficients { get; private set; }
        public double Correlation { get; private set; }

        public PolynomialRegression(double[] x, double[] y, int order)
        {
            Coefficients = new double[order + 1];
            int max_order = 2 * order;
            double[] x_terms = new double[max_order+1];
            double[] xy_terms = new double[order+1];

            // compute the matrix terms
            x_terms[0] = x.Length;
            xy_terms[0] = y.Sum();

            for(int i=0; i<x.Length; ++i)
            {
                double x_mult = 1;
                for (int j = 1; j <= order; ++j)
                {
                    x_mult *= x[i];
                    x_terms[j] += x_mult;
                    xy_terms[j] += y[i] * x_mult;
                }
                for (int j = order + 1; j <= max_order; ++j)
                {
                    x_mult *= x[i];
                    x_terms[j] += x_mult;
                }                   
            }

            // build matrix
            double[,] matrix = new double[order + 1, order + 2];
            for (int i = 0; i <= order; ++i)
            {
                for (int j = 0; j <= order; ++j)
                    matrix[i, j] = x_terms[i + j];
                matrix[i, order + 1] = xy_terms[i];
            }

            // Solve by Gaussian Elimination
            
            // Triangulate the matrix!
            for (int i = 0; i <= order; ++i)
            {
                int max_index = i;
                double temp = Math.Abs(matrix[max_index, i]);
                // find the row with the largest absolute value in the current column
                for (int j = i + 1; j <= order; ++j)
                {
                    if (temp < Math.Abs(matrix[j, i]))
                    {
                        max_index = j;
                        temp = Math.Abs(matrix[j, i]);
                    }
                }

                // if we found a bigger value, exchange the two rows
                if (i < max_index)
                {
                    for (int j = i; j <= order + 1; ++j)
                    {
                        temp = matrix[i, j];
                        matrix[i, j] = matrix[max_index, j];
                        matrix[max_index, j] = temp;
                    }
                }

                // subtract scaled current row from all following rows to get a leading zero
                for (int j = i + 1; j <= order; ++j)
                {
                    temp = matrix[j, i] / matrix[i, i];
                    matrix[j, i] = 0.0;
                    for (int k = i + 1; k <= order + 1; ++k)
                    {
                        matrix[j, k] -= temp * matrix[i, k];
                    }
                }
            }

            // finally get coefficients by solving equations using substitution
            for (int i = order; i >= 0; --i)
            {
                double temp = matrix[i, order + 1];
                for (int j = i + 1; j <= order; ++j)
                {
                    temp -= matrix[i, j] * Coefficients[j];
                }
                Coefficients[i] = temp / matrix[i, i];
            }

            // estimate error in fit
            var Stot = 0.0;
            var Serr = 0.0;
            var avg_y = y.Sum() / y.Length;
            for (int i = 0; i < x.Length; ++i)
            {
                Stot += (y[i] - avg_y) * (y[i] - avg_y);
                var f = FitPoint(x[i]);
                Serr += (y[i] - f) * (y[i] - f);
            }
            Correlation = 1 - Serr / Stot;
        }

        // given a value for the independent variable return an extrapolated point based on the fit
        public double FitPoint(double x)
        {
            var y = 0.0;
            for (int i = 0; i < Coefficients.Length; ++i)
                y += Coefficients[i] * (i == 0 ? 1.0 : Math.Pow(x, i));
            return y;
        }
    }
}
