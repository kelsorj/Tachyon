using System;
using System.Collections.Generic;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.Shared.PlateWork
{
    public interface IPlateScheduler : IReportsStatus
    {
        void StartScheduler();
        void StopScheduler();
        void EnqueueWorklist( Worklist worklist);
    }

    public interface IRobotScheduler : IReportsStatus
    {
        event EventHandler EnteringMovePlate;
        event EventHandler ExitingMovePlate;

        void StartScheduler();
        void StopScheduler();

        void AddJob( ActivePlate active_plate);
        void AddJob( string src_device_name, string src_location_name, string dst_device_name, string dst_location_name, string labware_name);
    }

    public class Workset
    {
        public HashSet< Plate> Plates { get; protected set; }

        public Workset()
        {
            Plates = new HashSet< Plate>();
        }

        public void AddPlate( Plate plate)
        {
            Plates.Add( plate);
        }
    }

    public class Worklist
    {
        public string Name { get; private set; }
        public IList< Plate> SourcePlates { get; protected set; }
        public IList< Plate> DestinationPlates { get; protected set; }
        public TransferOverview TransferOverview { get; set; }

        public event EventHandler WorklistComplete;

        public Worklist( string worklist_name)
        {
            Name = worklist_name;
            SourcePlates = new List< Plate>();
            DestinationPlates = new List< Plate>();
        }

        public void AddSourcePlate( Plate plate)
        {
            SourcePlates.Add( plate);
        }

        public void AddDestinationPlate( Plate plate)
        {
            DestinationPlates.Add( plate);
        }

        public void OnWorklistComplete()
        {
            if( WorklistComplete != null){
                WorklistComplete( this, null);
            }
        }
    }

    public class WorksetSequencer
    {
        /*
        public static Worklist DetermineSequence( Workset workset)
        {
        }
        */

        public static Worklist DetermineSequence( string worklist_name, TransferOverview transfer_overview)
        {
            Worklist worklist = new Worklist( worklist_name);
            worklist.TransferOverview = transfer_overview;
            // IEnumerable< Plate> source_plates = transfer_overview.Transfers.Select( t => t.Source).Distinct();
            foreach( var source_plate in transfer_overview.SourcePlates){
                worklist.AddSourcePlate( source_plate.Value);
            }
            foreach( var dest_plate in transfer_overview.DestinationPlates){
                worklist.AddDestinationPlate( dest_plate.Value);
            }
            return worklist;
        }
    }
}
