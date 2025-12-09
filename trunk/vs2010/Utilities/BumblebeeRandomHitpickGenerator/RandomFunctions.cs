using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.BumblebeeRandomHitpickGenerator
{
    public enum PlateType { Plate96, Plate384, Random }

    public static class RandomFunctions
    {
        static Random _rand = new Random( DateTime.Now.Second * DateTime.Now.Minute + DateTime.Now.Hour );

        public static PlateType GetRandomPlateType()
        {
            return (_rand.Next() % 2) == 0 ? PlateType.Plate96 : PlateType.Plate384;
        }

        public static List<PlateType> GetRandomPlateTypes( int number_of_plates)
        {
            List<PlateType> random_types = new List<PlateType>();
            for( int i=0; i<number_of_plates; i++)
                random_types.Add( GetRandomPlateType());
            return random_types;
        }

        public static List<string> GetRandomWells( PlateType plate_type, out string labware_name, int min96, int max96, int min384, int max384)
        {
            List<string> wells = new List<string>();
            // treat random plate type differently than 96 and 384
            int num_wells;
            if( plate_type == PlateType.Random) {
                // can't cast int to an enum???  I originally tried that and it always gave me 0.
                plate_type = (_rand.Next() % 2) == 0 ? PlateType.Plate96 : PlateType.Plate384;
            }
            // now that we know the type of plate (even if it's random), get the number of wells to create
            num_wells = plate_type == PlateType.Plate96 ? _rand.Next( min96, max96) : _rand.Next(min384, max384);
            // need to pick random wells.  loop over and over until we have num_wells UNIQUE indexes!
            List<int> random_wells = new List<int>();
            int rand_range = GetNumberOfWellsFromPlateType( plate_type);
            do {
                int possible_well = _rand.Next( 0, rand_range - 1);
                while( random_wells.Contains( possible_well)) {
                    if( ++possible_well > rand_range - 1)
                        possible_well = 0;
                }
                random_wells.Add( possible_well);
            } while( random_wells.Count < num_wells);
            for( int i=0; i<num_wells; i++)
                wells.Add( BioNex.Shared.Utils.Wells.IndexToWellName( random_wells[i], rand_range));
            labware_name = GetLabwareNameFromPlateType( plate_type);
            return wells;
        }

        public static int GetNumberOfWellsFromPlateType( PlateType type)
        {
            return type == PlateType.Plate96 ? 96 : 384;
        }

        public static string GetLabwareNameFromPlateType( PlateType type)
        {
            string labware96 = "NUNC 96 clear round well flat bottom";
            string labware384 = "NUNC 384 clear drafted square well";
            return type == PlateType.Plate96 ? labware96 : labware384;
        }

        public static void AddDestPlatesIfNecessary( List<PlateType> dest_plates, int total_source_transfers)
        {
            // figure out how many wells are covered by the current list of destination plates
            int available_dest_wells = 0;
            foreach( PlateType type in dest_plates)
                available_dest_wells += (type == PlateType.Plate96 ? 96 : 384);
            // if we have enough destination wells, then we're good to go
            if( available_dest_wells >= total_source_transfers)
                return;
            // otherwise, let's see how many are left
            int number_of_extra_wells_needed = total_source_transfers - available_dest_wells;
            // add more random destination plates until the requirement is met
            // here, we'll just keep looping and decrement the number of wells needed
            // until we have enough space in all dest plates
            while( number_of_extra_wells_needed > 0) {
                PlateType new_dest_plate_type = RandomFunctions.GetRandomPlateType();
                int num_wells_in_plate = RandomFunctions.GetNumberOfWellsFromPlateType( new_dest_plate_type);
                dest_plates.Add( new_dest_plate_type);
                number_of_extra_wells_needed -= num_wells_in_plate;
            }
        }

        /// <summary>
        /// Fisher-Yates shuffle algorithm implemented as an extension method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this IList<T> list)  
        {  
            Random rand = new Random();  
            int n = list.Count;  
            while( n > 1) {  
                n--;  
                int k = rand.Next( n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
        }
    }
}
