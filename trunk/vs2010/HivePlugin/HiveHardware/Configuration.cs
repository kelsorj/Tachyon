using System;
using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.BioNexGuiControls;

namespace BioNex.Hive.Hardware
{
    public class Configuration
    {
        public class RackConfig
        {
            public int RackNumber { get; set; }
            public RackView.PlateTypeT DefaultPlateType { get; set; }
            public List< Slot> Slots { get; set; }

            public RackConfig()
            {
                Slots = new List< Slot>();
            }

            public override string ToString()
            {
                return String.Format( "Rack {0}, {1} slots, default plate type: {2}", RackNumber, Slots.Count(), DefaultPlateType);
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

        public double ThetaSafe { get; set; }
        public double ThetaKungFu { get; set; }
        public double GripOpenDelta { get; set; }
        // Arm geometry constants
        public double ArmLength { get; set; }
        public double MaxY { get; set; }
        public double FingerOffsetZ { get; set; }
        public double PlateOffsetZ { get; set; }
        public double ParkPositionX { get; set; }
        public double ParkPositionZ { get; set; }

        /// <summary>
        /// Rehoming the Hive every time we start Synapsis is a real drag, so make it optional.
        /// </summary>
        public bool ForceRehome { get; set; }

        /// <summary>
        /// Any PVT move at a Z lower than this value will require a PVT-out move higher than the
        /// gripper offset, and then a Z down to the gripper offset for picking the plate.
        /// </summary>
        public double MinimumAllowableToolZHeightForPVT { get; set; }
        
        // barcode scanning behavior
        public int BarcodeMisreadThreshold { get; set; }

        /// <summary>
        /// Allows us to set up each slot individually.
        /// 1 = Tipbox
        /// 2 = Barcoded plate
        /// RackConfigurationMask should be either Tipbox or Barcode.
        /// </summary>
        /// <remarks>
        /// Super ultra lame -- I forgot that XmlSerializer won't work unless the properties are public.  So that means
        /// the app can just change stuff willy-nilly.  I don't like this, but don't have much of a choice for now.
        /// Please don't abuse this class.
        /// </remarks>
        public List< RackConfig> RackConfigurations { get; set; }

        public int TeachpointServicePort { get; set; }

        public Configuration()
        {
            RackConfigurations = new List<RackConfig>();
            TeachpointServicePort = 2345; // default
            BarcodeMisreadThreshold = 4;
            ForceRehome = false;
        }

        public void SetRackPlateType( int rack_number, RackView.PlateTypeT plate_type)
        {
            var rack = RackConfigurations.FirstOrDefault( x => x.RackNumber == rack_number);
            if (rack == null) {
                RackConfigurations.Add( new RackConfig { RackNumber = rack_number,  DefaultPlateType = plate_type } );
                return;
            }                
            rack.DefaultPlateType = plate_type;
        }

        /// <summary>
        /// returns each slot's plate type
        /// </summary>
        /// <param name="rack_number"></param>
        /// <returns>Dictionary&lt;int,PlateTypeT&gt;, where the key is the shelf NUMBER </returns>
        public Dictionary<int,RackView.PlateTypeT> GetOverriddenPlateTypesInRack( int rack_number)
        {
            // should have a max of one rack defined per rack number
            var rack = (from x in RackConfigurations where x.RackNumber == rack_number select x).FirstOrDefault();
            Dictionary<int,RackView.PlateTypeT> plate_types = new Dictionary<int,RackView.PlateTypeT>();
            if( rack == null)
                return plate_types;
            foreach( Slot x in rack.Slots) {
                plate_types.Add( x.SlotNumber, x.PlateType);
            }
            return plate_types;
        }

        public RackView.PlateTypeT GetDefaultPlateTypeForRack( int rack_number)
        {
            // should have a max of one rack defined per rack number
            var rack = (from x in RackConfigurations where x.RackNumber == rack_number select x).FirstOrDefault();
            if( rack == null)
                return RackView.PlateTypeT.Barcode;
            return rack.DefaultPlateType;
        }
    }
}
