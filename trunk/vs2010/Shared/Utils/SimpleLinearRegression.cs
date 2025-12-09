using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// A class for doing simple linear regression for single variable using Least-Squares

namespace BioNex.Shared.Utils
{
    public class SimpleLinearRegression
    {
        /// <summary>
        /// A class for simple linear regression with a single dependent (measured) variable
        /// 
        /// Calculates slope, intercept, and correlation coefficient
        /// 
        /// Slope = (Sum(X*Y) - 1/n * Sum(X)*Sum(Y)) / (Sum(X^2) - 1/n * (Sum(X))^2)
        /// Intercept = Avg(Y) - Slope * Avg(X)
        /// Correlation (R^2) = 1 - Sum((Y - F(X))^2) / Sum((Y - Avg(Y))^2)
        /// 
        /// </summary>
        /// <param name="X">Independent variable (no error)</param>
        /// <param name="Y">Measured variable with error</param>
        public SimpleLinearRegression(double[] X, double[] Y)
        {
            double Sa = 0.0;
            double Sb = 0.0;
            double Sc = 0.0;
            double Sd = 0.0;
            double Se = 0.0;
            int n = X.Length;
            double N = 1.0 / n;
            for (int i = 0; i < n; ++i)
            {
                double x = X[i];
                double y = Y[i];
                Sa += x * y;
                Sb += x;
                Sc += y;
                Sd += x * x;
            }
            Se = Sb * Sb;
            double avg_x = N *Sb;
            double avg_y = N *Sc;
            Slope = (Sa - N * Sb * Sc) / (Sd - N * Se);
            Intercept = avg_y - Slope * avg_x;

            double Stot = 0.0;
            double Serr = 0.0;
            for (int i = 0; i < n; ++i)
            {
                double y = Y[i];
                double x = X[i];
                Stot += (y - avg_y) * (y - avg_y);
                double f = Intercept + Slope * x;
                Serr += (y - f) * (y - f);
            }
            Correlation = 1 - Serr / Stot;
        }

        public double Slope { get; private set; }
        public double Intercept { get; private set; }
        public double Correlation { get; private set; }
    }
}
