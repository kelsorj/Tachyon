using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using System.Diagnostics;
using System.IO;

namespace BioNex._3AxisPTGenerator
{
    public class _1AxisPTTable : List<_1AxisPTTable.PTTableRow>
    {
        public class PTTableRow
        {
            double _position;
            double _time;

            public PTTableRow( double position, double time)
            {
                _position = position;
                _time = time;
            }

            public double Position
            {
                get { return _position; }
            }

            public double Time
            {
                get { return _time; }
            }
        }

        public _1AxisPTTable()
        {

        }

        public PTTableRow GetFirstPoint()
        {
            Debug.Assert( Count > 0, "The PT table is empty!");
            return this[0];
        }

        public _1AxisPTTable( string filename)
        {
            ReadFromFile( filename);
        }

        public void ReadFromFile( string filename)
        {
            Clear();
            TextReader reader = new StreamReader( filename);
            while( reader.Peek() != -1) {
                string line = reader.ReadLine();
                string[] values = line.Split( '\t');
                Add( new PTTableRow( double.Parse( values[0]), double.Parse( values[1])));
            }
            reader.Close();
        }
    }

    public class _3AxisPTTable
    {
        private const double _time_increment = 0.1;

        public class PTTableRow
        {
            private double _x;
            private double _y;
            private double _x2;
            private double _theta;

            public PTTableRow( double x, double y, double x2, double theta)
            {
                _x = x;
                _y = y;
                _x2 = x2;
                _theta = theta;
            }

            public double X
            {
                get { return _x; }
            }

            public double Y
            {
                get { return _y; }
            }

            public double Theta
            {
                get { return _theta; }
            }

            public double X2
            {
                get { return _x2; }
            }
        }

        private List<PTTableRow> _table = new List<PTTableRow>();

        public void Clear()
        {
            _table.Clear();
        }

        public void AddRow( PTTableRow row)
        {
            _table.Add( row);
        }

        public void WriteToFile()
        {
            // loop over the table and write each axis to its own file
            TextWriter x = new StreamWriter( @"c:\pt_x.txt");
            TextWriter y = new StreamWriter( @"c:\pt_y.txt");
            TextWriter theta = new StreamWriter( @"c:\pt_theta.txt");
            TextWriter x2 = new StreamWriter( @"c:\pt_x2.txt");

            int index = 1;
            foreach( PTTableRow r in _table) {
                x.WriteLine( String.Format( "{0}\t{1}", r.X, _time_increment * index));
                y.WriteLine( String.Format( "{0}\t{1}", r.Y, _time_increment * index));
                theta.WriteLine( String.Format( "{0}\t{1}", r.Theta / 360.0, _time_increment * index));
                x2.WriteLine( String.Format( "{0}\t{1}", r.X2, _time_increment * index));
                index++;
            }
            x.Close();
            y.Close();
            theta.Close();
            x2.Close();
        }

        public void GetSeparatePTTables( out _1AxisPTTable x, out _1AxisPTTable y,
                                         out _1AxisPTTable theta, out _1AxisPTTable x2)
        {
            // loop over each row in the 3axis table and create the separate axis tables
            x = new _1AxisPTTable();
            y = new _1AxisPTTable();
            theta = new _1AxisPTTable();
            x2 = new _1AxisPTTable();
            foreach( PTTableRow row in _table) {
                x.Add( new _1AxisPTTable.PTTableRow( row.X, _time_increment));
                y.Add( new _1AxisPTTable.PTTableRow( row.Y, _time_increment));
                theta.Add( new _1AxisPTTable.PTTableRow( row.Theta, _time_increment));
                x2.Add( new _1AxisPTTable.PTTableRow( row.X2, _time_increment));
            }
        }
    }

    public class PTGenerator
    {
        private string _first_tip_wellname;
        private int _num_wells;
        private double _x1; /// x position for the first tip, which is in a well
        private double _y1; /// y position for the plate that the first tip is in
        private double _x2; /// x position for the second tip, wherever it is currently
        private double _theta1; /// theta position for the plate that the first tip is in
        private int _channel_index_0_based; /// which channel has its tip in _first_tip_wellname
        private double _tip_spacing;
        private _3AxisPTTable _table = new _3AxisPTTable();
        
        public PTGenerator( string first_tip_wellname, int channel_index, double tip_spacing, int num_wells, double x, double y, double theta, double x2)
        {
            _first_tip_wellname = first_tip_wellname;
            _num_wells = num_wells;
            _x1 = x;
            _y1 = y;
            _x2 = x2;
            _theta1 = theta;
            _channel_index_0_based = channel_index;
            _tip_spacing = tip_spacing;
        }

        public bool GeneratePTTable( string second_tip_wellname, int desired_channel_index)
        {
            _table.Clear();
            double angle_rotation;
            bool rotate_clockwise;
            // figure out if there is a solution
            if( !GetTargetAngle( second_tip_wellname, desired_channel_index, out angle_rotation, out rotate_clockwise))
                return false;

            // NOTE: things might need to change here -- the angle_rotation value is signed, where
            //       CW = + and CCW = -, so obviously rotate_clockwise is extra information that
            //       isn't technically necessary.  For whatever reason, it still seems more
            //       intuitive to use an angle + direction to avoid polarity issues, so I'll
            //       do it that way for now.
            const double encoder_resolution = 4000.0;
            const double oversampling = 100.0;  // generate more data points than we think we need
            double theta_resolution = 360.0 / encoder_resolution / oversampling;
            angle_rotation = Math.Abs( angle_rotation);
            int num_steps = (int)(angle_rotation / theta_resolution);
            // before looping to get the XY values, we need to calculate the INITIAL XY position
            // relative to the plate, so we can get a delta XY.  This delta XY then gets applied to
            // the world coordinates _x1 and _y1 to add to the PT table.
            double start_x1, start_y1, start_x2, start_y2;
            GetWellXYPositionAtAngle( _first_tip_wellname, _num_wells, _theta1, out start_x1, out start_y1);
            // although we don't care about y2 (because it's dependent upon theta and y1),
            // we do need to get the x2 position so that we know where the next tip is supposed to go.
            GetWellXYPositionAtAngle( second_tip_wellname, _num_wells, _theta1, out start_x2, out start_y2);
            for( int i=0; i<num_steps; i++) {
                double temp_angle;
                if( rotate_clockwise) {
                    temp_angle = _theta1 + (theta_resolution * i);
                } else {
                    temp_angle = _theta1 - (theta_resolution * i);
                }
                // figure out what the X and Y positions are for the tip currently in well1
                double temp_x1, temp_y1, temp_x2, temp_y2;
                GetWellXYPositionAtAngle( _first_tip_wellname, _num_wells, temp_angle, out temp_x1, out temp_y1);
                GetWellXYPositionAtAngle( second_tip_wellname, _num_wells, temp_angle, out temp_x2, out temp_y2);
                // how do temp_x and temp_y compare to the initial positions, _x1 and _y1?
                double new_world_x = _x1 + (temp_x1 - start_x1);
                double new_world_y = _y1 - (temp_y1 - start_y1);
                double new_world_x2 = _x2 + (temp_x2 - start_x2); //! \todo this is completely wrong because X2 has no teachpoint reference right now!!!
                // add these values and temp_angle to the PT table
                _table.AddRow( new _3AxisPTTable.PTTableRow( new_world_x, new_world_y, new_world_x2, temp_angle));
            }
            return true;
        }

        public _3AxisPTTable Table
        {
            get { return _table; }
        }

        /// <summary>
        /// Returns a well's x and y coordinates (relative to the CENTER of the PLATE),
        /// given the position reading from the theta axis
        /// </summary>
        /// <param name="well_name"></param>
        /// <param name="angle"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void GetWellXYPositionAtAngle( string well_name, int num_wells, double angle, out double x, out double y)
        {
            // get position of well at 0 degrees
            double x_from_center = 0;
            double y_from_center = 0;
            BioNex.Shared.Utils.Wells.GetWellDistanceFromCenterOfPlate( well_name, num_wells, out x_from_center, out y_from_center);
            // now rotate and get new XY position
            BioNex.Shared.Utils.Wells.GetXYAfterRotation( x_from_center, y_from_center, Math.Abs(angle), angle > 0, out x, out y);
        }

        /// <summary>
        /// Computes the target angle for the plate stage and which direction to rotate in
        /// With this information, the PT generator can then create the X and Y positions
        /// from the angle
        /// </summary>
        /// <param name="second_tip_wellname">where the 2nd tip wants to go</param>
        /// <param name="desired_channel_index">used to figure out which way we need to rotate, and how far apart the tips are</param>
        /// <param name="new_angle">the new theta angle for the plate that the first tip is in</param>
        /// <returns>whether or not a solution is possible.  false = no solution.</returns>
        public bool GetTargetAngle( string second_tip_wellname, int desired_channel_index, out double new_angle,
                                    out bool rotate_clockwise)
        {
            // first, determine whether or not a solution is possible.  A solution is only possible if
            // the current distance between the well centers is GREATER THAN the distance between tips
            double distance_between_centers = Wells.GetDistanceBetweenWells( _num_wells, _first_tip_wellname,
                                                                                   second_tip_wellname);
            double distance_between_tips = (_channel_index_0_based - desired_channel_index) * _tip_spacing;
            new_angle = 0;
            rotate_clockwise = false;
            if( distance_between_centers <  Math.Abs( distance_between_tips))
                return false;

            // 1. the tip spacing (already calculated above for validation check)
            // 2. where the 2nd tip is relative to the first tip (is it above or below?)
            Debug.Assert( distance_between_tips != 0, "You can't do multiple simultaneous dispenses with the same tip!");
            bool want_to_use_tip_above_tip1 = distance_between_tips > 0;
            // also need to figure out which way to rotate to get the desired Y spacing between wells
            // it really just depends on which quadrant well2 is in initially, relative to well1 as the center.
            // NOTE: this is basically going to give the same information as the call to
            // GetXYBetweenWellsAfterRotation(), but this allows us to figure out which quadrant
            // well2 is in, relative to well1.
            double rotated_x1, rotated_y1, rotated_x2, rotated_y2;
            rotated_x1 = rotated_y1 = rotated_x2 = rotated_y2 = 0;
            GetWellXYPositionAtAngle( _first_tip_wellname, _num_wells, _theta1, out rotated_x1, out rotated_y1);
            GetWellXYPositionAtAngle( second_tip_wellname, _num_wells, _theta1, out rotated_x2, out rotated_y2);
            // ok, now that we have the rotation coords, we can determine the quadrant.
            int quadrant = Wells.GetWell2QuadrantRelativeToWell1( rotated_x1, rotated_y1, rotated_x2, rotated_y2);
            // figure out the current distance between the two wells at the CURRENT position
            double initial_x_distance = 0;
            double initial_y_distance = 0;
            Wells.GetXYBetweenWellsAfterRotation( _first_tip_wellname, second_tip_wellname, _num_wells, _theta1,
                                                  _theta1 > 0, out initial_x_distance, out initial_y_distance);
            // based on the quadrant selected, we now know which direction to rotate in
            rotate_clockwise = Wells.GetRotationDirection( rotate_clockwise, distance_between_tips, initial_y_distance, want_to_use_tip_above_tip1, quadrant);
            
            // now we know which direction we need to rotate the plate, but we don't know how much to rotate
            // to get the desired change in Y distance between the two wells.
            // Ben solved this in Mathcad for me.  See Central Desktop for the MathCad and Octave files
            // link to Octave file: http://www.centraldesktop.com/p/aQAAAAAAPvfD
            // link to Mathcad file: 
            // there will be two solutions:
            double a = rotated_x2;
            double b = rotated_y2;
            double c = rotated_x1;
            double d = rotated_y1;
            double e = distance_between_tips;
            // solution 1 is 2 * atan( upper1 / lower1)
            double upper1_sqrt_term = a*a - 2*a*c + b*b - 2*b*d + c*c + d*d - e*e;
            if( upper1_sqrt_term < 0 && upper1_sqrt_term > -0.00001)
                upper1_sqrt_term = 0;
            double upper1 = a - c + Math.Sqrt( upper1_sqrt_term);
            double lower1 = d - b - e;
            double solution1 = Wells.RadiansToDegrees( 2 * Math.Atan(upper1 / lower1));
            // solution 2 is -2 * atan( upper2 / lower2)
            double upper2_sqrt_term = a*a - 2*a*c + b*b - 2*b*d + c*c + d*d - e*e;
            if( upper2_sqrt_term < 0 && upper2_sqrt_term > -0.00001)
                upper2_sqrt_term = 0;
            double upper2 = c - a + Math.Sqrt( upper2_sqrt_term);
            double lower2 = d - b - e;
            double solution2 = Wells.RadiansToDegrees( -2 * Math.Atan(upper2 / lower2));
            Debug.Assert( Math.Abs( solution1 - solution2) < 0.0001, "There should only be one solution for the angle computation");
            new_angle = solution1;
            return true;
        }
    }

    /*
    [TestFixture]
    public class TestPTGenerator
    {
        [Test]
        /// well 2 is in Q1 relative to well 1
        /// tip 2 is below tip 1
        public void TestGetTargetAngleQuadrant1Below()
        {
            PTGenerator pt = new PTGenerator( "D16", 0, 9, 384, 0, 0, -25, 0);
            double new_angle;
            bool rotate_clockwise;
            pt.GetTargetAngle( "D18", 1, out new_angle, out rotate_clockwise);
            Assert.IsTrue( rotate_clockwise);
            // correct answer is to rotate theta 115 degrees CW
            Assert.LessOrEqual( Math.Abs( new_angle - 115.0), 0.01);
        }

        [Test]
        /// well 2 is in Q1 relative to well 1
        /// tip 2 is above tip 1
        public void TestGetTargetAngleQuadrant1Above()
        {
            PTGenerator pt = new PTGenerator( "D16", 1, 9, 384, 0, 0, -25, 0);
            double new_angle;
            bool rotate_clockwise;
            pt.GetTargetAngle( "D18", 0, out new_angle, out rotate_clockwise);
            Assert.IsFalse( rotate_clockwise);
            // correct answer is to rotate theta 65 degrees CCW
            Assert.LessOrEqual( Math.Abs( new_angle + 65.0), 0.01);            
        }

        [Test]
        /// well 2 is in Q2 relative to well 1
        /// tip 2 is below tip 1
        public void TestGetTargetAngleQuadrant2Below()
        {
            PTGenerator pt = new PTGenerator( "D16", 0, 9, 384, 0, 0, -115, 0);
            double new_angle;
            bool rotate_clockwise;
            pt.GetTargetAngle( "D18", 1, out new_angle, out rotate_clockwise);
            Assert.IsFalse( rotate_clockwise);
            // correct answer is to rotate theta 155 degrees CCW
            Assert.LessOrEqual( Math.Abs( new_angle + 155.0), 0.01);
        }

        [Test]
        /// well 2 is in Q2 relative to well 1
        /// tip 2 is above tip 1
        public void TestGetTargetAngleQuadrant2Above()
        {
            PTGenerator pt = new PTGenerator( "D16", 1, 9, 384, 0, 0, -115, 0);
            double new_angle;
            bool rotate_clockwise;
            pt.GetTargetAngle( "D18", 0, out new_angle, out rotate_clockwise);
            Assert.IsTrue( rotate_clockwise);
            // correct answer is to rotate theta 25 degrees CW
            Assert.LessOrEqual( Math.Abs( new_angle - 25.0), 0.01);
        }

        [Test]
        /// well 2 is in Q3 relative to well 1
        /// tip 2 is below tip 1
        public void TestGetTargetAngleQuadrant3Below()
        {
            PTGenerator pt = new PTGenerator( "D16", 0, 9, 384, 0, 0, 155, 0);
            double new_angle;
            bool rotate_clockwise;
            pt.GetTargetAngle( "D18", 1, out new_angle, out rotate_clockwise);
            Assert.IsFalse( rotate_clockwise);
            // correct answer is to rotate theta 65 degrees CCW
            Assert.LessOrEqual( Math.Abs( new_angle + 65.0), 0.01);
        }

        [Test]
        /// well 2 is in Q3 relative to well 1
        /// tip 2 is above tip 1
        public void TestGetTargetAngleQuadrant3Above()
        {
            PTGenerator pt = new PTGenerator( "D16", 1, 9, 384, 0, 0, 155, 0);
            double new_angle;
            bool rotate_clockwise;
            pt.GetTargetAngle( "D18", 0, out new_angle, out rotate_clockwise);
            Assert.IsTrue( rotate_clockwise);
            // correct answer is to rotate theta 115 degrees CW
            Assert.LessOrEqual( Math.Abs( new_angle - 115.0), 0.01);            
        }

        [Test]
        /// well 2 is in Q4 relative to well 1
        /// tip 2 is below tip 1
        public void TestGetTargetAngleQuadrant4Below()
        {
            PTGenerator pt = new PTGenerator( "D16", 0, 9, 384, 0, 0, 25, 0);
            double new_angle;
            bool rotate_clockwise;
            pt.GetTargetAngle( "D18", 1, out new_angle, out rotate_clockwise);
            Assert.IsTrue( rotate_clockwise);
            // correct answer is to rotate theta 65 degrees CW
            Assert.LessOrEqual( Math.Abs( new_angle - 65.0), 0.01);
        }

        [Test]
        /// well 2 is in Q4 relative to well 1
        /// tip 2 is above tip 1
        public void TestGetTargetAngleQuadrant4Above()
        {
            PTGenerator pt = new PTGenerator( "D16", 1, 9, 384, 0, 0, 25, 0);
            double new_angle;
            bool rotate_clockwise;
            pt.GetTargetAngle( "D18", 0, out new_angle, out rotate_clockwise);
            Assert.IsFalse( rotate_clockwise);
            // correct answer is to rotate theta 115 degrees CCW
            Assert.LessOrEqual( Math.Abs( new_angle + 115.0), 0.01);            
        }

        [Test]
        public void TestPTTableGeneration()
        {
            PTGenerator pt = new PTGenerator( "D16", 0, 9, 384, 0, 0, -25, 0);
            Assert.IsTrue( pt.GeneratePTTable( "D18", 1));
            pt.Table.WriteToFile();
        }
    }
    */
}
