using System;
using BioNex.BumblebeeGUI;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.BumblebeePlugin.Hardware
{
    public class Stage : HardwareQuantum
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public IAxis YAxis { get; protected set; }
        public IAxis RAxis { get; protected set; }

        protected Teachpoints _teachpoints;

        public Plate Plate { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public Stage( byte id, IAxis y_axis, IAxis r_axis)
            : base( id)
        {
            AddAxis( "Y", YAxis = y_axis);
            AddAxis( "R", RAxis = r_axis);

            Plate = null;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void HomeYR()
        {
            Home( new string[]{ "Y", "R"});
        }
        // ----------------------------------------------------------------------
        // need to pass in teachpoints because we need it for calculating well positions -- is there a better way?
        public void SetSystemTeachpoints( Teachpoints tps)
        {
            _teachpoints = tps;
        }
        // ----------------------------------------------------------------------
        public StageTeachpoint GetChannelTeachpoint( byte channel_id)
        {
            return _teachpoints.GetStageTeachpoint( channel_id, ID);
        }
        // ----------------------------------------------------------------------
        public virtual void ClearForStage()
        {
        }
        // ----------------------------------------------------------------------
        public virtual void MoveAbsolute( double y, double r)
        {
            DateTime start = DateTime.Now;
            YAxis.MoveAbsolute( y, wait_for_move_complete: false);
            RAxis.MoveAbsolute( r, wait_for_move_complete: false);
            YAxis.MoveAbsolute( y);
            RAxis.MoveAbsolute( r);
            TimeSpan task_time = DateTime.Now - start;
            Log.DebugFormat( "(gantt) moving {0} to ({1}, {2}) took {3}s", this, y, r, task_time.TotalSeconds);
        }
        // ----------------------------------------------------------------------
        public void MoveToRobotTeachpoint( int orientation)
        {
            Teachpoint rtp = _teachpoints.GetRobotTeachpoint( ID, orientation);
            MoveAbsolute( rtp[ "y"], rtp[ "r"]);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns the center of the plate stage in world coordinates, based off of the
        /// UL and LR teachpoints
        /// </summary>
        /// <param name="channel_id"></param>
        /// <param name="center_x"></param>
        /// <param name="center_y"></param>
        public Tuple< double, double> GetCenterPosition( byte channel_id)
        {
            // look up the teachpoint for this stage and channel_id
            StageTeachpoint stp = _teachpoints.GetStageTeachpoint( channel_id, ID);

            // figure out midpoint -- this is the center position
            return Tuple.Create(( stp.LowerRight["x"] - stp.UpperLeft["x"]) / 2 + stp.UpperLeft["x"],
                                ( stp.LowerRight["y"] - stp.UpperLeft["y"]) / 2 + stp.UpperLeft["y"]);
        }
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return String.Format( "Stage {0}", ID);
        }
    }
}
