using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using BioNex.Shared.BioNexGuiControls;

namespace BioNex.BPS140Plugin
{
    [XmlRoot("Configuration")]
    public class BPS140Configuration
    {
        public class RackConfig
        {
            public int RackNumber { get; set; }
            public int SideNumber { get; set; }
            public RackView.PlateTypeT DefaultPlateType { get; set; }
            public List<Slot> Slots { get; set; }

            public RackConfig()
            {
                Slots = new List<Slot>();
            }

            public override string ToString()
            {
                return String.Format( "Rack {0}, {1} slots have plate type overriden, default plate type: {2}", RackNumber, Slots.Count(), DefaultPlateType);
            }
        }

        public class Slot
        {
            public int SlotNumber { get; set; }
            public RackView.PlateTypeT PlateType { get; set; }

            public override string ToString()
            {
                return String.Format( "Slot {0}, plate type: {1}", SlotNumber, PlateType);
            }
        }
        
        /// <summary>
        /// Allows us to set up each rack individually.
        /// 1 = TipBox
        /// 2 = Source
        /// 4 = Destination
        /// RackConfigurationMask should be either Tipbox or Source | Destination, but not ever Tipbox | Source | Destination
        /// because that would be an ambiguous barcode reading condition.
        /// </summary>
        /// <remarks>
        /// Super ultra lame -- I forgot that XmlSerializer won't work unless the properties are public.  So that means
        /// the app can just change stuff willy-nilly.  I don't like this, but don't have much of a choice for now.
        /// Please don't abuse this class.
        /// </remarks>
        public List<RackConfig> RackConfigurations { get; set; }
        /// <summary>
        /// If the number of barcode misreads is at or above this value, rescan the entire rack at a slower
        /// speed.  Otherwise, just scan individual plates.
        /// </summary>
        /// <remarks>
        /// If this value is zero, then the entire rack will always get rescanned at a slower speed.
        /// </remarks>
        public int BarcodeMisreadThreshold { get; set; }

        public BPS140Configuration()
        {
            RackConfigurations = new List<RackConfig>();
            BarcodeMisreadThreshold = 4;
        }

        public void SetRackPlateType( int side_number, int rack_number, RackView.PlateTypeT type_mask)
        {
            var rack = RackConfigurations.FirstOrDefault( x => (x.RackNumber == rack_number && x.SideNumber == side_number));
            if (rack == null) {
                RackConfigurations.Add( new RackConfig { SideNumber = side_number, RackNumber = rack_number, DefaultPlateType = type_mask } );
                return;
            }                
            rack.DefaultPlateType = type_mask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="side_number"></param>
        /// <param name="rack_number"></param>
        /// <returns>Dictionary&lt;int,PlateTypeT&gt;, where the key is the shelf NUMBER </returns>
        public Dictionary<int, RackView.PlateTypeT> GetOverriddenPlateTypesInRack( int side_number, int rack_number)
        {
            // should have a max of one rack defined per rack number
            var rack = (from x in RackConfigurations where (x.SideNumber == side_number && x.RackNumber == rack_number) select x).FirstOrDefault();
            Dictionary<int,RackView.PlateTypeT> plate_types = new Dictionary<int,RackView.PlateTypeT>();
            if( rack == null)
                return plate_types;
            foreach( Slot x in rack.Slots) {
                plate_types.Add( x.SlotNumber, x.PlateType);
            }
            return plate_types;

        }

        public RackView.PlateTypeT GetDefaultPlateTypeForRack( int side_number, int rack_number)
        {
            // should have a max of one rack defined per rack number
            var rack = (from x in RackConfigurations where (x.SideNumber == side_number && x.RackNumber == rack_number) select x).FirstOrDefault();
            if( rack == null)
                return RackView.PlateTypeT.Barcode;
            return rack.DefaultPlateType;
        }
    }
}
