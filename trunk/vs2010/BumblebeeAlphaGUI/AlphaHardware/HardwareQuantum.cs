using System;
using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.TechnosoftLibrary;
using log4net;

namespace BioNex.BumblebeePlugin.Hardware
{
    public class HardwareQuantum
    {
        // ----------------------------------------------------------------------
        // inner classes.
        // ----------------------------------------------------------------------
        public class AxisStatus
        {
            public bool IsHomed { get; set; }
            public bool IsEnabled { get; set; }
            public double PositionMM { get; set; }
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public byte ID { get; protected set; }
        public IDictionary< string, IAxis> Axes { get; private set; }
        public IDictionary< string, AxisStatus> AxisStatuses { get; private set; }

        protected ILog Log { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public HardwareQuantum( byte id)
        {
            ID = id;
            Axes = new Dictionary< string, IAxis>();
            AxisStatuses = new Dictionary< string, AxisStatus>();

            Log = LogManager.GetLogger( this.GetType());
        }
        // ----------------------------------------------------------------------
        public void AddAxis( string axis_name, IAxis axis)
        {
            Axes[ axis_name] = axis;
            AxisStatuses[ axis_name] = new AxisStatus();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void Reset()
        {
            foreach( IAxis axis in Axes.Values){
                axis.ResetPause();
            }
        }
        // ----------------------------------------------------------------------
        public void UpdateCurrentStatus()
        {
            foreach( string axis_name in Axes.Keys){
                IAxis axis = Axes[ axis_name];
                AxisStatuses[ axis_name].IsHomed = axis.IsHomed;
                AxisStatuses[ axis_name].IsEnabled = axis.IsOn();
                AxisStatuses[ axis_name].PositionMM = axis.GetPositionMM();
            }
        }
        // ----------------------------------------------------------------------
        public bool IsHomed( bool use_cached_values = true)
        {
            if( !use_cached_values){
                UpdateCurrentStatus();
            }
            return AxisStatuses.Count( axis_status => !axis_status.Value.IsHomed) == 0;
        }
        // ----------------------------------------------------------------------
        public void Home( IEnumerable< string> axis_names, long timeout_s = 30)
        {
            IEnumerable< IAxis> axes = axis_names.Select( axis_name => Axes[ axis_name]);
            foreach( IAxis axis in axes){
                axis.SendResetAndHome();
            }
            DateTime expiration = DateTime.Now.AddSeconds( timeout_s);
            foreach( IAxis axis in axes){
                axis.WaitForHomeResult(( long)(( expiration - DateTime.Now).TotalMilliseconds), false);
            }
        }
        // ----------------------------------------------------------------------
        public void Enable( bool on)
        {
            foreach( IAxis axis in Axes.Values){
                axis.Enable( on, true);
            }
        }
        // ----------------------------------------------------------------------
        // axis methods.
        // ----------------------------------------------------------------------
        public void HomeAxis( string axis_name)
        {
            Axes[ axis_name].Home( true);
        }
        // ----------------------------------------------------------------------
        public void EnableAxis( string axis_name, bool on)
        {
            Axes[ axis_name].Enable( on, true);
        }
        // ----------------------------------------------------------------------
        public virtual void JogAxis( string axis_name, double jog_increment)
        {
            // WAxis.StartLogging();
            IAxis axis = Axes[ axis_name];
            jog_increment = ( axis.Settings.FlipAxisDirection ? -jog_increment : jog_increment);
            axis.MoveRelative( jog_increment);
            // WAxis.WaitForLoggingComplete( String.Format( @"c:\{0}_aspirate_pos.txt", WAxis.GetID()));
            // WAxis.WaitForLoggingComplete( String.Format( @"c:\{0}_dispense_pos.txt", WAxis.GetID()));
        }
    }
}
