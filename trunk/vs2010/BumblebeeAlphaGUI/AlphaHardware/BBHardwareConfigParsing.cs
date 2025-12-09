using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.BumblebeePlugin.Hardware
{
    public partial class BBHardware
    {
        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        /// <summary>
        /// loads a hardware configuration and also validates against available axes
        /// </summary>
        /// <param name="configuration_path"></param>
        /// <param name="ts"></param>
        public void LoadConfiguration( string configuration_path, TechnosoftConnection ts, IOInterface io_interface)
        {
            TechnosoftConnection = ts;
            Dictionary<byte,IAxis> axes = ts.GetAxes();
            Channels = new List< Channel>();
            Stages = new List< Stage>();
            XmlDocument doc = new XmlDocument();
            doc.Load( configuration_path);
            Debug.Assert( doc.DocumentElement.Name == "HardwareConfiguration");
            foreach( XmlNode node in doc.DocumentElement.ChildNodes) {
                if( node.Name == "channels") {
                    foreach( XmlNode n in node) {
                        if( n.Name == "channel") {
                            ParseChannel( n, Channels, axes);
                        }
                    }
                } else if( node.Name == "stages") {
                    foreach( XmlNode n in node) {
                        if( n.Name == "stage") {
                            ParseStage( n, Stages, axes);
                        }
                        if( n.Name == "tip_shuttle"){
                            ParseTipShuttle( n, Stages, axes, io_interface);
                        }
                    }
                }
            }

            // set up the events and stuff for homing and moving.
            AvailableHardwareQuanta.ForEach( q => q.UpdateCurrentStatus());

            // DKM 2012-01-16 debugging only -- want to see what the initial home states are for each of the axes.
            // LogBumblebeeHomeStates();
        }
        // ----------------------------------------------------------------------
        private static void ParseChannel( XmlNode node, ICollection< Channel> channels, IDictionary< byte, IAxis> axes)
        {
            byte id = 0;
            byte x_id = 0;
            byte z_id = 0;
            byte w_id = 0;
            string conversion_formula = String.Empty;
            // TipShuckSettings tip_shuck_settings = null;
            double w_shuck_offset_mm = 0.0;

            bool available = true;
            if( node.Name == "channel")
                id = (byte)(channels.Count + 1);
            else
                return;
            foreach( XmlNode n in node) {
                if( n.Name == "x")
                    x_id = byte.Parse( n.InnerText);
                else if( n.Name == "z")
                    z_id = byte.Parse( n.InnerText);
                else if( n.Name == "w")
                    w_id = byte.Parse( n.InnerText);
                else if( n.Name == "conversion_formula")
                    conversion_formula = n.InnerText;
                else if( n.Name == "available")
                    available = bool.Parse( n.InnerText);
                else if( n.Name == "w_shuck_offset_mm")
                    w_shuck_offset_mm = double.Parse( n.InnerText);
                // else if( n.Name == "tip_shuck_settings")
                // tip_shuck_settings = ParseTipShuckSettings( n);
            }
            if( x_id == 0 && z_id == 0 && w_id == 0){
                channels.Add( new Channel( id, ( byte)( id * 10 + 1), ( byte)( id * 10 + 3), ( byte)( id * 10 + 4), w_shuck_offset_mm, available));
                if( conversion_formula != String.Empty)
                    axes[w_id].SetConversionFormula( conversion_formula);
                return;
            }
            // technically, we shouldn't need this part if the XML gets validated against an XSD.
            if( id == 0 || x_id == 0 || z_id == 0 || w_id == 0)
                throw new ApplicationException( "Cannot parse channel XML because one or more elements are missing");
            // now we have to make sure that all of the specified axes actually exist
            if( !axes.ContainsKey( x_id) || !axes.ContainsKey( z_id) || !axes.ContainsKey( w_id))
                throw new ApplicationException( String.Format( "Cannot create channel '{0}' because one of its axes was not defined in the motor configuration file", id));
            channels.Add( new Channel( id, axes[ x_id], axes[ z_id], axes[ w_id], w_shuck_offset_mm, available));
            if( conversion_formula != String.Empty)
                axes[w_id].SetConversionFormula( conversion_formula);
        }
        // ----------------------------------------------------------------------
        private static void ParseStage( XmlNode node, ICollection< Stage> stages, IDictionary< byte, IAxis> axes)
        {
            byte id = 0;
            byte y_id = 0;
            byte r_id = 0;
            if( node.Name == "stage") {
                id = (byte)(stages.Count + 1);
            } else
                return;
            foreach( XmlNode n in node) {
                if( n.Name == "y")
                    y_id = byte.Parse( n.InnerText);
                else if( n.Name == "r")
                    r_id = byte.Parse( n.InnerText);
            }

            if( id == 0 || y_id == 0 || r_id == 0)
                throw new ApplicationException( "Cannot parse stage XML because one or more elements are missing");
            // ensure that the required axes exist
            if( !axes.ContainsKey( y_id) || !axes.ContainsKey( r_id))
                throw new ApplicationException( String.Format( "Cannot create stage '{0}' because one of its axes was not defined in the motor configuration file", id));
            stages.Add( new Stage( id, axes[y_id], axes[r_id]));
        }
        // ----------------------------------------------------------------------
        private static void ParseTipShuttle( XmlNode node, ICollection< Stage> tip_shuttles, IDictionary< byte, IAxis> axes, IOInterface tip_washer_io_interface)
        {
            byte id = 0;
            byte y_id = 0;
            byte a_id = 0;
            byte b_id = 0;
            TipWasherIOConfiguration tip_washer_io_configuration = null;
            if( node.Name == "tip_shuttle"){
                id = ( byte)( tip_shuttles.Count + 1);
            } else{
                return;
            }
            foreach( XmlNode n in node){
                if( n.Name == "y")
                    y_id = byte.Parse( n.InnerText);
                else if( n.Name == "a")
                    a_id = byte.Parse( n.InnerText);
                else if( n.Name == "b")
                    b_id = byte.Parse( n.InnerText);
                else if( n.Name == "io_configuration")
                    tip_washer_io_configuration = ParseTipWasherIOConfiguration( n);
            }
            // tip shuttle id cannot be zero.
            if( id == 0){
                throw new ApplicationException( "Cannot parse tip-shuttle XML because one or more elements are missing");
            }
            // if y_id nonzero, then there must be a matching axis among axes.
            // if a_id nonzero, then there must be a matching axis among axes.
            // if b_id nonzero, then there must be a matching axis among axes.
            if(( y_id != 0 && !axes.ContainsKey( y_id)) || ( a_id != 0 && !axes.ContainsKey( a_id)) || ( b_id != 0 && !axes.ContainsKey( b_id))){
                throw new ApplicationException( String.Format( "Cannot create tip-shuttle '{0}' because one of its axes was not defined in the motor configuration file", id));
            }
            if( tip_washer_io_configuration == null || !tip_washer_io_configuration.AllBitIndicesUnique()){
                throw new ApplicationException( String.Format( "Cannot create tip-shuttle '{0}' with incomplete/invalid I/O configuration", id));
            }
            if( y_id == 0){
                // if y_id zero, then create fully virtual tip shuttle.
                tip_shuttles.Add( new TipShuttle( id, new SimAxis(( byte)( id * 10 + 2), axes[( byte)( id * 10 + 2)].Settings, true), new SimAxis(( byte)( id * 10 + 6), axes[( byte)( id * 10 + 6)].Settings, true), new SimAxis(( byte)( id * 10 + 7), axes[( byte)( id * 10 + 7)].Settings, true), tip_washer_io_interface, tip_washer_io_configuration));
            } else{
                // if y_id nonzero, then create tip shuttle with real y.
                if(( a_id != 0) && ( b_id != 0)){
                    // if a_id and b_id nonzero, then create tip shuttle with real y, a, and b.
                    tip_shuttles.Add( new TipShuttle( id, axes[ y_id], axes[ a_id], axes[ b_id], tip_washer_io_interface, tip_washer_io_configuration));
                } else{
                    // else create tip shuttle with real y but virtual washer (a, b).
                    tip_shuttles.Add( new TipShuttle( id, axes[ y_id], new SimAxis(( byte)( y_id + 4), axes[( byte)( y_id + 4)].Settings, true), new SimAxis(( byte)( y_id + 5), axes[( byte)( y_id + 5)].Settings, true), tip_washer_io_interface, tip_washer_io_configuration));
                }
            }
        }
        // ----------------------------------------------------------------------
        private static TipWasherIOConfiguration ParseTipWasherIOConfiguration( XmlNode node)
        {
            TipWasherIOConfiguration config = new TipWasherIOConfiguration();

            foreach( XmlNode n in node) {
                if( n.Name == "bath_water_bit_index")
                    config.BathWaterBitIndex = int.Parse( n.InnerText);
                if( n.Name == "plenum_water_bit_index")
                    config.PlenumWaterBitIndex = int.Parse( n.InnerText);
                if( n.Name == "overflow_exhaust_bit_index")
                    config.OverflowExhaustBitIndex = int.Parse( n.InnerText);
                if( n.Name == "vacuum_bit_index")
                    config.VacuumBitIndex = int.Parse( n.InnerText);
                if( n.Name == "air_bit_index")
                    config.AirBitIndex = int.Parse( n.InnerText);
            }

            return config;
        }
        // ----------------------------------------------------------------------
        /*
        private void LogBumblebeeHomeStates()
        {
            lock( AxisStatusLock){
                List< string> status = ( from x in AxisStatus.Values select x.HomeStatus.ToString()).ToList();
                string status_string = status.ToCommaSeparatedString();
                _log.DebugFormat( "Bumblebee home states: {0}", status_string);
            }
        }
        */
    }
}
