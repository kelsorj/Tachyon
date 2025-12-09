// Giles Biddison,  February 2011
/*
 * Vector3D class for basic vector operations
 * 
 * Includes 
 *   Translation (with operator+ and operator-)
 *   Cross Product
 *   Dot Product
 *   Magnitude
 *   Normalization
 *   Angle Between two vectors
 *   Axis-Angle Rotation of a point (vector representation) around an arbitrary axis 
 *   
 * Currently used by Cart-Docking system:
 *   1. Teach 3 points on cart in first robot reference frame
 *   2. Interpolate and refine teachpoints (refinment offsets are refined teachpoint - original interpolated teachpoint)
 *   3. Teach 3 points on same cart in second robot reference frame
 *   4. Interpolate
 *   5. Translate ref frames to common origin (i.e. subtract bottom left coord from all coords -- for 3 ref points)
 *   6. Calculate plane normals from 3 points for both ref. frames
 *   7. Cross product gives rotation axis, dot product gives angle
 *   8. Transform refinement offsets via Axis-Angle rotation
 *   9. Add transformed refinement offsets to second set of interpolated points
*/

using System;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class Vector3D
    {
        private readonly double[] _x = new double[3];
        public double X { get { return _x[0]; } }
        public double Y { get { return _x[1]; } }
        public double Z { get { return _x[2]; } }

        public Vector3D(double x, double y, double z)
        {
            _x[0] = x; _x[1] = y; _x[2] = z;
        }
        public Vector3D(Vector3D copy)
        {
            for (int i = 0; i < 3; ++i) _x[i] = copy._x[i];
        }
        public Vector3D(double[] values)
        {
            for (int i = 0; i < 3; ++i) _x[i] = values[i];
        }

        /// <summary>
        /// Translation of v1 by v2
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector3D operator+(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        /// <summary>
        /// Translation of v1 by -v2
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector3D operator-(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        } 

        /// <summary>
        /// Computes cross product between two vectors
        /// </summary>
        /// <param name="first">Vector 1</param>
        /// <param name="second">Vector 2</param>
        /// <returns>Cross product vector as Vector3d</returns>
        public static Vector3D CrossProduct(Vector3D first, Vector3D second)
        {
            // x0 = y1z2 - z1y2 
            // y0 = z1x2 - x1z2
            // z0 = x1y2 - y1x2
            return new Vector3D(first.Y * second.Z - first.Z * second.Y
                              , first.Z * second.X - first.X * second.Z
                              , first.X * second.Y - first.Y * second.X);
        }
        public Vector3D CrossProduct(Vector3D rhs)
        {
            return Vector3D.CrossProduct(this, rhs);
        }

        /// <summary>
        /// Computes dot product between two vectors
        /// </summary>
        /// <param name="first">Vector 1</param>
        /// <param name="second">Vector 2</param>
        /// <returns>Dot product scalar</returns>
        public static double DotProduct(Vector3D first, Vector3D second)
        {
            // X1*x2 + y1*y2 + z1*z2
            return first.X * second.X + first.Y * second.Y + first.Z * second.Z;
        }
        public double DotProduct(Vector3D rhs)
        {
            return Vector3D.DotProduct(this, rhs);
        }

        /// <summary>
        /// Computes magnitude (norm) of a vector
        /// </summary>
        /// <param name="v">vector</param>
        /// <returns>Magnitude (norm) of the vector</returns>
        public static double Magnitude(Vector3D v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }
        public double Magnitude()
        {
            return Vector3D.Magnitude(this);
        }

        /// <summary>
        /// Computes the normalized vector (magnitude 1)
        /// </summary>
        /// <param name="v">vector</param>
        /// <returns>Normalized vector as Vector3D</returns>
        public static Vector3D Normalize(Vector3D v)
        {
            double norm = v.Magnitude();
            if (norm == 0.0)
                return new Vector3D(0.0, 0.0, 0.0);
            return new Vector3D(v.X / norm, v.Y / norm, v.Z / norm);
        }
        public Vector3D Normalize()
        {
            return Vector3D.Normalize(this);
        }

        /// <summary>
        /// Computes the angle between two vectors (pre-normalizes).  Vectors should be translated to the origin for this to make sense.
        /// Chokes if the angle is > 180 (NAN exception)
        /// </summary>
        /// <param name="first">vector 1</param>
        /// <param name="second">vector 2</param>
        /// <returns>The angle between the vectors in radians</returns>
        public static double AngleBetween(Vector3D first, Vector3D second)
        {
            if (first.X == second.X && first.Y == second.Y && first.Z == second.Z)
                return 0.0;
            return Math.Acos(DotProduct(first.Normalize(), second.Normalize()));
        }
        public double AngleBetween(Vector3D rhs)
        {
            return AngleBetween(this, rhs);
        }

        /// <summary>
        /// Rotate a point about an arbitrary axis by an angle
        /// </summary>
        /// <param name="point">The point to rotate</param>
        /// <param name="axis">The axis around which to rotate</param>
        /// <param name="angle">The angle (in radians) by which to rotate</param>
        /// <returns>The transformed point as Vector3D</returns>
        public static Vector3D RotateByAxisAngle(Vector3D point, Vector3D axis, double angle)
        {
            if (axis.X == 0.0 && axis.Y == 0.0 && axis.Z == 0.0)
                return new Vector3D(point);

            axis = axis.Normalize();  // ensure that the axis has been normalized

            // compute some reusable values
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);
            double t = 1.0 - c;
            double xyt = axis.X * axis.Y * t;
            double zs = axis.Z * s;
            double xzt = axis.X * axis.Z * t;
            double ys = axis.Y * s;
            double yzt = axis.Y * axis.Z * t;
            double xs = axis.X * s;

            // compute 3x3 rotation matrix 
            double[,] m = new double[3, 3];
            m[0, 0] = c + axis.X * axis.X * t;
            m[0, 1] = xyt - zs;
            m[0, 2] = xzt + ys;
            m[1, 0] = xyt + zs;
            m[1, 1] = c + axis.Y * axis.Y * t;
            m[1, 2] = yzt - xs;
            m[2, 0] = xzt - ys;
            m[2, 1] = yzt + xs;
            m[2, 2] = c + axis.Z * axis.Z * t;

            // multiply matrix times vector
            return new Vector3D(
                      m[0, 0] * point.X + m[0, 1] * point.Y + m[0, 2] * point.Z
                    , m[1, 0] * point.X + m[1, 1] * point.Y + m[1, 2] * point.Z
                    , m[2, 0] * point.X + m[2, 1] * point.Y + m[2, 2] * point.Z
                );
        }
        public Vector3D RotateByAxisAngle(Vector3D axis, double angle)
        {
            return RotateByAxisAngle(this, axis, angle);
        }
    }
#endif
}
