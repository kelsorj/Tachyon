using System;
using System.Collections.Generic;
using System.Linq;

namespace BioNex.Shared.Utils
{
    public static class MathUtil
    {
        #region RADIANS_DEGREES
        public static double DegreesToRadians( double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public static double RadiansToDegrees( double radians)
        {
            return radians * 180.0 / Math.PI;
        }
        #endregion

        #region NORMALIZE_ANGLE
        /// <summary>
        /// Normalize angle to [-PI, +PI].
        /// </summary>
        /// <param name="angle">The angle to be normalized.</param>
        /// <returns>The normalized angle.</returns>
        public static double NormalizeAngle( double angle)
        {
            return Math.IEEERemainder( angle, 2.0 * Math.PI);
        }
        #endregion

        #region APPROXIMATELY_EQUAL
        public static bool ApproximatelyEqual( double a, double b, double delta)
        {
            return Math.Abs( a - b) < delta;
        }

        public static bool ApproximatelyIntegral( double x, double delta)
        {
            return ApproximatelyEqual( Math.Round( x), x, delta);
        }

        public class ApproximatelyEqualComparer : IComparer< double>
        {
            public double Delta { get; private set; }

            public ApproximatelyEqualComparer( double delta = 0.0)
            {
                Delta = delta;
            }

            public int Compare( double a, double b)
            {
                if( ApproximatelyEqual( a, b, Delta))
                    return 0;
                if( a < b)
                    return -1;
                if( a > b)
                    return 1;
                return 0;
            }
        }
        #endregion

        #region QUANTUM_MATH
        public class QuantumMath
        {
            protected double Quantum { get; set; }
            protected int Roundoff { get; set; }

            public QuantumMath( double quantum, int roundoff = 6)
            {
                Quantum = quantum;
                Roundoff = roundoff;
            }

            public double Round( double the_number)
            {
                return Math.Round( the_number / Quantum) * Quantum;
            }

            public double Ceiling( double the_number)
            {
                return Math.Ceiling( Math.Round( the_number / Quantum, Roundoff)) * Quantum;
            }

            public double Floor( double the_number)
            {
                return Math.Floor( Math.Round( the_number / Quantum, Roundoff)) * Quantum;
            }

            public int Compare( double lhs, double rhs)
            {
                double lhs_quanta = Math.Round( lhs / Quantum, Roundoff);
                double rhs_quanta = Math.Round( rhs / Quantum, Roundoff);
                return ( lhs_quanta > rhs_quanta) ? 1 : ( lhs_quanta < rhs_quanta) ? -1 : 0;
            }

            public bool Between( double the_number, double min, double max)
            {
                double the_number_quanta = Math.Round( the_number / Quantum, Roundoff);
                double min_quanta = Math.Round( min / Quantum, Roundoff);
                double max_quanta = Math.Round( max / Quantum, Roundoff);
                return (( the_number_quanta >= min_quanta) && ( the_number_quanta <= max_quanta));
            }

            /*
            public static void Test()
            {
                QuantumMath qm = new QuantumMath( 0.01);
                for( double loop = 0.0; loop < 2000.0; loop += 0.01){
                    double ceiling = qm.Ceiling( loop);
                    double round = qm.Round( loop);
                    int compare1 = qm.Compare( loop, ceiling);
                    int compare2 = qm.Compare( loop, round);
                    int compare3 = qm.Compare( ceiling, round);
                }
            }
            */
        }
        #endregion

        #region SOLVE_SYSTEM_OF_LINEAR_EQUATIONS
        public static List< double> SolveSystemOfLinearEquations( List< List< double>> equations)
        {
            int num_equations = equations.Count();
            if( num_equations == 1){
                return new List< double>{ equations[ 0][ 1] / equations[ 0][ 0]};
            }
            List< List< double>> reduced_equations = ( from i in Enumerable.Range( 0, num_equations - 1)
                                                       select ReduceFirst( equations[ i], equations[ i + 1])).ToList();
            List< double> answer = SolveSystemOfLinearEquations( reduced_equations);
            double solution = equations[ 0].Last();
            for( int i = 0; i < answer.Count(); ++i){
                solution -= answer[ i] * equations[ 0][ i + 1];
            }
            solution /= equations[ 0].First();
            answer.Insert( 0, solution);
            return answer;
        }

        private static List< double> ReduceFirst( IList< double> equation1, IList< double> equation2)
        {
            int num_coefficients = equation1.Count();
            return ( from i in Enumerable.Range( 1, num_coefficients - 1)
                     select ( equation1[ i] * equation2[ 0]) + ( equation2[ i] * -equation1[ 0])).ToList();
        }
        #endregion

        #region SOLVE_POLYNOMIAL_FUNCTIONS
        /// <summary>
        /// Find the root of the linear function f(x) = ax + b.
        /// </summary>
        /// <param name="a">The linear coefficient "a".</param>
        /// <param name="b">The constant coefficient "b".</param>
        /// <returns>The solution for x in the equation ax + b = 0.</returns>
        public static double SolveLinearFunction( double a, double b)
        {
            return -b / a;
        }

        /// <summary>
        /// Find the root(s) of the quadratic function f(x) = ax^2 + bx + c.
        /// </summary>
        /// <param name="a">The quadratic coefficient "a".</param>
        /// <param name="b">The linear coefficient "b".</param>
        /// <param name="c">The constant coefficient "c".</param>
        /// <returns>The solution(s) for x in the equation ax^2 + bx + c = 0.</returns>
        public static Tuple< double, double> SolveQuadraticFunction( double a, double b, double c)
        {
            // starting with ax^2 + bx + c = 0.
            // if the quadratic coefficient is zero, then solve the remaining linear function.
            if( a == 0.0){
                return Tuple.Create( SolveLinearFunction( b, c),
                                     double.NaN);
            }
            // calculate the root of the discriminant.
            double discriminant_root = Math.Sqrt(( b * b) - ( 4.0 * a * c));
            // calculate the roots of the quadratic function.
            return Tuple.Create< double, double>(( -b + discriminant_root) / ( 2.0 * a),
                                                 ( -b - discriminant_root) / ( 2.0 * a));
        }

        /// <summary>
        /// Find the root(s) of the cubic function f(x) = ax^3 + bx^2 + cx + d.
        /// </summary>
        /// <param name="a">The cubic coefficient "a".</param>
        /// <param name="b">The quadratic coefficient "b".</param>
        /// <param name="c">The linear coefficient "c".</param>
        /// <param name="d">The constant coefficient "d".</param>
        /// <returns>The solution(s) for x in the equation ax^3 + bx^2 + cx + d = 0.</returns>
        public static Tuple< double, double, double> SolveCubicFunction( double a, double b, double c, double d)
        {
            // starting with ax^3 + bx^2 + cx + d = 0.
            // if the cubic coefficient is zero, then solve the remaining quadratic function.
            if( a == 0.0){
                Tuple< double, double> quadratic_solution = SolveQuadraticFunction( b, c, d);
                return Tuple.Create( quadratic_solution.Item1,
                                     quadratic_solution.Item2,
                                     double.NaN);
            }
            // normalize the cubic function (i.e., make the cubic coefficient 1).
            // x^3 + (b/a)x^2 + (c/a)x + d = 0.
            b = b / a;
            c = c / a;
            d = d / a;
            // transform the cubic function into an equivalent function of depressed form (i.e., having a quadratic coefficient of 0) by substitution of x = y - b/3.
            // end up with y^3 + Ay = B by allowing the following substitutions:
            double A = c - ( b * b / 3.0);
            double B = (( b * c / 3.0) - ( b * b * b / 13.5)) - d;
            // now make the following substitution for y: y = z - A/3z
            // and multiply through with z^3.
            // end up with z^6 - Bz^3 - A^3/27 = 0.
            // solve for z^3 by quadratic equation:
            // z^3 = 1/2(B +/- sqrt(B^2 + 4(A^3/27)).
            // distribute 1/2 through sqrt:
            // z^3 = B/2 +/- sqrt(B^2/4 + A^3/27).
            // end up with Z^3 = R +/- sqrt(R^2 + Q^3) by allowing the following substitutions:
            double R = B / 2.0;
            double Q = A / 3.0;
            double discriminant = R * R + Q * Q * Q;
            if( discriminant < 0.0){
                // case 1: discriminant is negative.
                double theta = Math.Acos( R / Math.Sqrt( -Q * Q * Q));
                double negative_one_third_b = -b / 3.0;
                double twice_negative_q_root = 2 * Math.Sqrt( -Q);
                // return three real roots:
                return Tuple.Create( negative_one_third_b + twice_negative_q_root * Math.Cos( theta / 3.0),
                                     negative_one_third_b + twice_negative_q_root * Math.Cos( ( theta + 2.0 * Math.PI) / 3.0),
                                     negative_one_third_b + twice_negative_q_root * Math.Cos( ( theta + 4.0 * Math.PI) / 3.0));
            } else{
                // case 2: discriminant is non-negative.
                double discriminant_root = Math.Sqrt( discriminant);
                double S = Math.Pow( R + discriminant_root, 1.0 / 3.0);
                double T = -Math.Pow( discriminant_root - R, 1.0 / 3.0);
                // return one real root and two NaNs representing two complex roots.
                return Tuple.Create( -b / 3.0 + ( S + T),
                                     double.NaN,
                                     double.NaN);
            }
        }
        #endregion
    }
}
