using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.HiveIntegration
{
    /// <summary>
    /// Provides information about the Hive device's current state of operation
    /// </summary>
    public class HiveStatus
    {
        // DO NOT EVER CHANGE THIS ORDER OF BITS
        public enum StatusBits { Full, Empty, Busy, MovingPlate, LoadingPlate, UnloadingPlate, ScanningInventory };

        /// <summary>
        /// Hive static storage is full, and all carts are either full or do not belong to the plate group requested by the MovePlate method
        /// </summary>
        public bool Full { get; set; }
        /// <summary>
        /// Hive static storage has empty locations available
        /// </summary>
        public bool Empty { get; set; }
        /// <summary>
        /// Hive is currently busy processing another command
        /// </summary>
        public bool Busy { get; set; }
        /// <summary>
        /// Hive is currently moving a plate between static storage, carts, and/or the trash dropoff location
        /// </summary>
        public bool MovingPlate { get; set; }
        /// <summary>
        /// Hive is currently handling a LoadPlate command
        /// </summary>
        public bool LoadingPlate { get; set; }
        /// <summary>
        /// Hive is currently handling an UnloadPlate command
        /// </summary>
        public bool UnloadingPlate { get; set; }
        /// <summary>
        /// Hive is currently scanning its plate locations with the barcode reader
        /// </summary>
        public bool ScanningInventory { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine( "Full = " + Full.ToString());
            sb.AppendLine( "Empty = " + Empty.ToString());
            sb.AppendLine( "Busy = " + Busy.ToString());
            sb.AppendLine( "MovingPlate = " + MovingPlate.ToString());
            sb.AppendLine( "UnloadingPlate = " + UnloadingPlate.ToString());
            sb.AppendLine( "LoadingPlate = " + LoadingPlate.ToString());
            sb.AppendLine( "ScanningInventory = " + ScanningInventory.ToString());
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            HiveStatus other = (HiveStatus)obj;
            return Full == other.Full && Empty == other.Empty && Busy == other.Busy && MovingPlate == other.MovingPlate &&
                   LoadingPlate == other.LoadingPlate && UnloadingPlate == other.UnloadingPlate && ScanningInventory == other.ScanningInventory;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public HiveStatus()
        {
        }

        public HiveStatus( bool full, bool empty, bool busy, bool moving_plate, bool loading_plate, bool unloading_plate, bool scanning_inventory)
            : this()
        {
            Full = full;
            Empty = empty;
            Busy = busy;
            MovingPlate = moving_plate;
            LoadingPlate = loading_plate;
            UnloadingPlate = unloading_plate;
            ScanningInventory = scanning_inventory;
        }

        public static HiveStatus FromInt( int status)
        {
            bool full = (status & (1 << (int)StatusBits.Full)) != 0 ? true : false;
            bool empty = (status & (1 << (int)StatusBits.Empty)) != 0 ? true : false;
            bool busy = (status & (1 << (int)StatusBits.Busy)) != 0 ? true : false;
            bool moving_plate = (status & (1 << (int)StatusBits.MovingPlate)) != 0 ? true : false;
            bool loading_plate = (status & (1 << (int)StatusBits.LoadingPlate)) != 0 ? true : false;
            bool unloading_plate = (status & (1 << (int)StatusBits.UnloadingPlate)) != 0 ? true : false;
            bool scanning_inventory = (status & (1 << (int)StatusBits.ScanningInventory)) != 0 ? true : false;
            return new HiveStatus( full, empty, busy, moving_plate, loading_plate, unloading_plate, scanning_inventory);
        }

        public int ToInt()
        {
            int status = 0;
            if( Full) status |= (1 << (int)StatusBits.Full);
            if( Empty) status |= (1 << (int)StatusBits.Empty);
            if( Busy) status |= (1 << (int)StatusBits.Busy);
            if( MovingPlate) status |= (1 << (int)StatusBits.MovingPlate);
            if( LoadingPlate) status |= (1 << (int)StatusBits.LoadingPlate);
            if( UnloadingPlate) status |= (1 << (int)StatusBits.UnloadingPlate);
            if( ScanningInventory) status |= (1 << (int)StatusBits.ScanningInventory);
            return status;
        }
    }
}
