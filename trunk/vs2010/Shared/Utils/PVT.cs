using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Utils.PVT
{
    public class PVTPoint
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        /// <summary>
        /// The expected position of the object.
        /// </summary>
        public double Position { get; private set; }
        // ----------------------------------------------------------------------
        /// <summary>
        /// The expected velocity of the object.
        /// </summary>
        public double Velocity { get; private set; }
        // ----------------------------------------------------------------------
        /// <summary>
        /// The amount of time given to the object to reach the expected position and velocity.
        /// (Truly, this is not a "time" but rather a "duration.")
        /// </summary>
        public double Time { get; private set; }
        // ----------------------------------------------------------------------
        /// <summary>
        /// An additional marker allowing application-specific tagging of the PVTPoint.
        /// </summary>
        public int Marker { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public PVTPoint( double position, double velocity, double time, int marker)
        {
            Position = position;
            Velocity = velocity;
            Time = time;
            Marker = marker;
        }
    }

    public class PVTTrajectory
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected SortedList< double, PVTPoint> PVTPoints { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public PVTTrajectory( double initial_position)
        {
            PVTPoints = new SortedList< double, PVTPoint>();
            PVTPoints.Add( 0.0, new PVTPoint( initial_position, 0.0, 0.0, int.MinValue));
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public double GetDuration()
        {
            return PVTPoints.Last().Key;
        }
        // ----------------------------------------------------------------------
        public int GetNumPVTPoints()
        {
            return PVTPoints.Count;
        }
        // ----------------------------------------------------------------------
        public IEnumerable< PVTPoint> GetPVTPoints()
        {
            return PVTPoints.Values;
        }
        // ----------------------------------------------------------------------
        public void Enqueue( PVTPoint pvt_point)
        {
            double new_duration = GetDuration() + pvt_point.Time;
            PVTPoints.Add( new_duration, pvt_point);
        }
        // ----------------------------------------------------------------------
        public PVTPoint GetPVTPointByIndex( int index)
        {
            return PVTPoints.Values[ index];
        }
        // ----------------------------------------------------------------------
        public int GetMarkerDuringWhichMoveDied( int pvt_pts_buffered)
        {
            int pvt_pts_total = GetNumPVTPoints();
            int pvt_pts_completed = pvt_pts_total - ( pvt_pts_buffered + 1);
            return GetPVTPointByIndex( pvt_pts_completed).Marker;
        }
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach( KeyValuePair< double, PVTPoint> pvt_point in PVTPoints){
                sb.AppendFormat( "{0,10}: (P,V,duration,marker)=({1},{2},{3},{4})", pvt_point.Key, pvt_point.Value.Position, pvt_point.Value.Velocity, pvt_point.Value.Time, pvt_point.Value.Marker);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
