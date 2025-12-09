using System;
using System.Collections.Generic;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.BumblebeePlugin.Hardware
{
    public enum TipShuttleState
    {
        Uninitialized,
        ServingTips,
        Washing,
        OutOfService,
    }

    public enum WasherPosition
    {
        Retracted,
        Wash,
        Dry,
    }

    public class TipWasherIOConfiguration
    {
        public int BathWaterBitIndex { get; set; }
        public int PlenumWaterBitIndex { get; set; }
        public int OverflowExhaustBitIndex { get; set; }
        public int VacuumBitIndex { get; set; }
        public int AirBitIndex { get; set; }

        public bool AllBitIndicesUnique()
        {
            HashSet< int> used_bit_indices = new HashSet< int>();
            foreach( int i in new int[]{ BathWaterBitIndex, PlenumWaterBitIndex, OverflowExhaustBitIndex, VacuumBitIndex, AirBitIndex}){
                if( used_bit_indices.Contains( i)){
                    return false;
                }
                used_bit_indices.Add( i);
            }
            return true;
        }
    }

    public class TipShuttle : Stage
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public IAxis AAxis { get; private set; }
        public IAxis BAxis { get; private set; }
        public IOInterface IOInterface { get; private set; }
        private TipWasherIOConfiguration IOConfiguration { get;  set; }

        public TipCarrier TipCarrier { get; private set; }
        private TipShuttleState State { get; set; }

        public const string PLENUM_PREFIX = "plenum";
        public const string BATH_PREFIX = "bath";

        private const double Y_POSITION_SAFE = 170.0;

        private double StagePositionWash { get { return _teachpoints.GetRobotTeachpoint( ID, 0)[ "y"]; }}
        private double BathPositionRetracted { get { return _teachpoints.GetWasherTeachpoint( ID)[ BATH_PREFIX + "-" + ToPositionString( WasherPosition.Retracted)]; }}
        private double BathPositionWash { get { return _teachpoints.GetWasherTeachpoint( ID)[ BATH_PREFIX + "-" + ToPositionString( WasherPosition.Wash)]; }}
        private double BathPositionDry { get { return _teachpoints.GetWasherTeachpoint( ID)[ BATH_PREFIX + "-" + ToPositionString( WasherPosition.Dry)]; }}
        private double PlenumPositionRetracted { get { return _teachpoints.GetWasherTeachpoint( ID)[ PLENUM_PREFIX + "-" + ToPositionString( WasherPosition.Retracted)]; }}
        private double PlenumPositionWash { get { return _teachpoints.GetWasherTeachpoint( ID)[ PLENUM_PREFIX + "-" + ToPositionString( WasherPosition.Wash)]; }}
        private double PlenumPositionDry { get { return _teachpoints.GetWasherTeachpoint( ID)[ PLENUM_PREFIX + "-" + ToPositionString( WasherPosition.Dry)]; }}

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipShuttle( byte id, byte y_axis_id, byte a_axis_id, byte b_axis_id, IOInterface io_interface, TipWasherIOConfiguration io_configuration)
            : this( id, new NonExistentAxis( y_axis_id), a_axis_id, b_axis_id, io_interface, io_configuration)
        {
        }
        // ----------------------------------------------------------------------
        public TipShuttle( byte id, IAxis y_axis, byte a_axis_id, byte b_axis_id, IOInterface io_interface, TipWasherIOConfiguration io_configuration)
            : this( id, y_axis, new NonExistentAxis( a_axis_id), new NonExistentAxis( b_axis_id), io_interface, io_configuration)
        {
        }
        // ----------------------------------------------------------------------
        public TipShuttle( byte id, IAxis y_axis, IAxis a_axis, IAxis b_axis, IOInterface io_interface, TipWasherIOConfiguration io_configuration)
            : base( id, y_axis, new NonExistentAxis(( byte)( y_axis.GetID() + 3)))
        {
            AddAxis( "A", AAxis = a_axis);
            AddAxis( "B", BAxis = b_axis);
            IOInterface = io_interface;
            IOConfiguration = io_configuration;

            TipCarrier = new TipCarrier( this, new TipCarrierFormat());
            State = TipShuttleState.Uninitialized;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public TipShuttleState GetState()
        {
            lock( this){
                return State;
            }
        }
        // ----------------------------------------------------------------------
        public void SetState( TipShuttleState state)
        {
            lock( this){
                State = state;
            }
        }
        // ----------------------------------------------------------------------
        public void HomeAB()
        {
            Home( new string[]{ "A", "B"});
        }
        // ----------------------------------------------------------------------
        // stage-motion methods.
        // ----------------------------------------------------------------------
        public bool IsYSafeToMove( bool throw_exception_if_unsafe = true)
        {
            if( AAxis.GetPositionMM() < PlenumPositionRetracted - 0.5){
                if( throw_exception_if_unsafe){
                    throw new Exception( "Unsafe to move stage because plenum is not fully retracted");
                }
                return false;
            }
            if( BAxis.GetPositionMM() > BathPositionRetracted + 0.5){
                if( throw_exception_if_unsafe){
                    throw new Exception( "Unsafe to move stage because bath is not fully retracted");
                }
                return false;
            }
            return true;
        }
        // ----------------------------------------------------------------------
        public override void ClearForStage()
        {
            MoveWasher( WasherPosition.Retracted);
            base.ClearForStage();
        }
        // ----------------------------------------------------------------------
        public override void MoveAbsolute( double y, double r)
        {
            // if any part of the stage move is not in the stage's safe zone, throw exception if unsafe to move stage.
            if(( y < Y_POSITION_SAFE) || ( YAxis.GetPositionMM() < Y_POSITION_SAFE)){
                IsYSafeToMove( throw_exception_if_unsafe: true);
            }
            YAxis.MoveAbsolute( y);
        }
        // ----------------------------------------------------------------------
        public void MoveToWasher()
        {
            MoveAbsolute( StagePositionWash, 0);
        }
        // ----------------------------------------------------------------------
        // washer-motion methods.
        // ----------------------------------------------------------------------
        private bool IsPlenumOrBathSafeToMove( bool throw_exception_if_unsafe = true)
        {
            double current_y_position = YAxis.GetPositionMM();
            if(( current_y_position > StagePositionWash - 0.5) && ( current_y_position < StagePositionWash + 0.5)){
                return true;
            }
            if( current_y_position > Y_POSITION_SAFE){
                return true;
            }
            if( throw_exception_if_unsafe){
                throw new Exception( "Unsafe to move plenum/bath because stage is neither at the washer position nor in the safe zone");
            }
            return false;
        }
        // ----------------------------------------------------------------------
        public override void JogAxis( string axis_name, double jog_increment)
        {
            switch( axis_name){
                case "Y":   MoveAbsolute( YAxis.GetPositionMM() + jog_increment, 0); break;
                case "A":   JogPlenum( jog_increment); break;
                case "B":   JogBath( jog_increment); break;
            }
        }
        // ----------------------------------------------------------------------
        private void JogPlenum( double jog_increment)
        {
            double plenum_position_src = AAxis.GetPositionMM();
            double plenum_position_dst = plenum_position_src + jog_increment;
            // if jogging down, throw exception if unsafe to move plenum.
            if(( jog_increment < 0.0) && ( plenum_position_dst < PlenumPositionRetracted - 0.5)){
                IsPlenumOrBathSafeToMove( throw_exception_if_unsafe: true);
            }
            AAxis.MoveAbsolute( plenum_position_dst);
        }
        // ----------------------------------------------------------------------
        private void JogBath( double jog_increment)
        {
            double bath_position_src = BAxis.GetPositionMM();
            double bath_position_dst = bath_position_src + jog_increment;
            // if jogging up, throw exception if unsafe to move bath.
            if(( jog_increment > 0.0) && ( bath_position_dst > BathPositionRetracted + 0.5)){
                IsPlenumOrBathSafeToMove( throw_exception_if_unsafe: true);
            }
            BAxis.MoveAbsolute( bath_position_dst);
        }
        // ----------------------------------------------------------------------
        public void MoveWasher( WasherPosition washer_position, bool move_plenum = true, bool move_bath = true, double speed_factor = 1.0)
        {
            // make sure speed factor is between 0.001 and 1.0.
            speed_factor = Math.Max( Math.Min( speed_factor, 1.0), 0.001);
            Teachpoint washer_teachpoint = _teachpoints.GetWasherTeachpoint( ID);
            string position_string = ToPositionString( washer_position);
            double plenum_position_src = AAxis.GetPositionMM();
            double plenum_position_dst = move_plenum ? washer_teachpoint[ PLENUM_PREFIX + "-" + position_string] : plenum_position_src;
            // if moving plenum down and moving down beyond (by 0.5mm) retracted position, throw exception if unsafe to move plenum.
            if(( plenum_position_dst - plenum_position_src < 0.0) && ( plenum_position_dst < PlenumPositionRetracted - 0.5)){
                IsPlenumOrBathSafeToMove( throw_exception_if_unsafe: true);
            }
            double bath_position_src = BAxis.GetPositionMM();
            double bath_position_dst = move_bath ? washer_teachpoint[ BATH_PREFIX + "-" + position_string] : bath_position_src;
            // if moving bath up and moving up beyond (by 0.5mm) retracted position, throw exception if unsafe to move bath.
            if(( bath_position_dst - bath_position_src > 0.0) && ( bath_position_dst > BathPositionRetracted + 0.5)){
                IsPlenumOrBathSafeToMove( throw_exception_if_unsafe: true);
            }
            if( move_plenum){
                AAxis.MoveAbsolute( plenum_position_dst, velocity: AAxis.Settings.Velocity * speed_factor, acceleration: AAxis.Settings.Acceleration * speed_factor * speed_factor, wait_for_move_complete: false);
            }
            if( move_bath){
                BAxis.MoveAbsolute(bath_position_dst, velocity: BAxis.Settings.Velocity * speed_factor, acceleration: BAxis.Settings.Acceleration * speed_factor * speed_factor, wait_for_move_complete: true);
            }
            if( move_plenum){
                AAxis.MoveAbsolute(plenum_position_dst, velocity: AAxis.Settings.Velocity * speed_factor, acceleration: AAxis.Settings.Acceleration * speed_factor * speed_factor, wait_for_move_complete: true);
            }
        }
        // ----------------------------------------------------------------------
        // i/o-manipulation methods.
        // ----------------------------------------------------------------------
        public void SetBathWaterSwitch( bool on)
        {
            IOInterface.SetOutputState( IOConfiguration.BathWaterBitIndex, on);
        }
        // ----------------------------------------------------------------------
        public void SetPlenumWaterSwitch( bool on)
        {
            IOInterface.SetOutputState( IOConfiguration.PlenumWaterBitIndex, on);
        }
        // ----------------------------------------------------------------------
        public void SetOverflowExhaustSwitch( bool on)
        {
            IOInterface.SetOutputState( IOConfiguration.OverflowExhaustBitIndex, on);
        }
        // ----------------------------------------------------------------------
        public void SetVacuumSwitch( bool on)
        {
            IOInterface.SetOutputState( IOConfiguration.VacuumBitIndex, on);
        }
        // ----------------------------------------------------------------------
        public void SetAirSwitch( bool on)
        {
            IOInterface.SetOutputState( IOConfiguration.AirBitIndex, on);
        }
        // ----------------------------------------------------------------------
        // other methods.
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return String.Format( "Tip Shuttle {0}", ID);
        }
        // ----------------------------------------------------------------------
        // class methods.
        // ----------------------------------------------------------------------
        public static string ToPositionString( WasherPosition washer_position)
        {
            return washer_position.ToString().ToLower();
        }
        // ----------------------------------------------------------------------
        public static WasherPosition ToWasherPosition( string position_string)
        {
            if( position_string == "retracted"){
                return WasherPosition.Retracted;
            } else if( position_string == "wash"){
                return WasherPosition.Wash;
            } else if( position_string == "dry"){
                return WasherPosition.Dry;
            }
            throw new ArgumentException( "invalid position_string");
        }
    }
}
