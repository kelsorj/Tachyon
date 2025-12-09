using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using BioNex.BumblebeeGUI;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.TechnosoftLibrary;
using log4net;

[assembly: InternalsVisibleTo( "BumblebeePlugin")]

namespace BioNex.BumblebeePlugin.Hardware
{
    public partial class BBHardware
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private TechnosoftConnection TechnosoftConnection = null;

        public List< Channel> Channels { get; private set; }
        public List< Channel> AvailableChannels { get { return Channels.Where( c => c.Available).ToList(); }}
        public List< Stage> Stages { get; private set; }
        public List< TipShuttle> TipShuttles { get { return Stages.Where( s => s is TipShuttle).Select( s => s as TipShuttle).ToList(); }}
        public List< HardwareQuantum> AvailableHardwareQuanta { get { return AvailableChannels.Select( c => c as HardwareQuantum).Union( Stages.Select( s => s as HardwareQuantum)).ToList(); }}
        
        private static readonly ILog Log = LogManager.GetLogger(typeof( BBHardware));

        public bool Homed {
            get {
                // DKM 2012-01-16 debugging only -- want to see what the initial home states are for each of the axes.
                // LogBumblebeeHomeStates();
                // prevent composition crash if there is no hardware and you aren't simulating.
                if( Channels == null){
                    return false;
                }
                return AvailableHardwareQuanta.TrueForAll( q => q.IsHomed());
            }
        }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public BBHardware()
        {
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void Reset()
        {
            AvailableHardwareQuanta.ForEach( q => q.Reset());
        }
        // ----------------------------------------------------------------------
        public HardwareQuantum GetHardwareQuantum( byte channel_id, byte stage_id, string axis_name)
        {
            switch( axis_name){
                case "X":
                case "Z":
                case "W":
                    return GetChannel( channel_id);
                case "Y":
                case "R":
                    return GetStage( stage_id);
                case "A":
                case "B":
                    return GetTipShuttle( stage_id);
            }
            return null;
        }
        // ----------------------------------------------------------------------
        public Channel GetChannel( byte id)
        {
            return Channels.FirstOrDefault( c => c.ID == id);
        }
        // ----------------------------------------------------------------------
        public Stage GetStage( byte id)
        {
            return Stages.FirstOrDefault( s => s.ID == id);
        }
        // ----------------------------------------------------------------------
        public TipShuttle GetTipShuttle( byte id)
        {
            return GetStage( id) as TipShuttle;
        }
        // ----------------------------------------------------------------------
        public Stage GetStage( Plate plate_on_stage)
        {
            if( plate_on_stage == null){
                return null;
            }
            return Stages.FirstOrDefault( s => s.Plate == plate_on_stage);
        }
        // ----------------------------------------------------------------------
        public double GetChannelSpacing( byte channel1_id, byte channel2_id, byte stage_id)
        {
            StageTeachpoint tp1 = GetStage( stage_id).GetChannelTeachpoint( channel1_id);
            StageTeachpoint tp2 = GetStage( stage_id).GetChannelTeachpoint( channel2_id);

            double channel_spacing_ul = Math.Abs( tp1.UpperLeft[ "y"] - tp2.UpperLeft[ "y"]);
            double channel_spacing_lr = Math.Abs( tp1.LowerRight[ "y"] - tp2.LowerRight[ "y"]);

            string possible_error = String.Format( "The teachpoints for each channel should be very close to each other, but the ones for channels {0} and {1} are off by {2}", channel1_id, channel2_id, channel_spacing_lr - channel_spacing_ul);
            if( Math.Abs( channel_spacing_ul - channel_spacing_lr) > 1.0){
                Debug.Assert( false, possible_error);
            }

            return ( channel_spacing_ul + channel_spacing_lr) / 2;
        }
    }
}
