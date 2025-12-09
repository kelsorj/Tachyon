using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BioNex.Shared.Location;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.TaskListXMLParser;
using log4net;

namespace BioNex.Shared.PlateWork
{
    public abstract class ActivePlate
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public static IList< ActivePlate> ActivePlates { get; set; }
        protected static int PlateSerialNumberAutoIncrement { get; set; }

        public int PlateSerialNumber { get; protected set; }

        protected List< PlateTask> ToDoList { get; set; }
        protected IEnumerator< PlateTask> CurrentToDo { get; set; }
        protected bool StillHaveToDos { get; set; }

        public ManualResetEvent PlateIsFree { get; set; }
        public bool Busy{
            get{
                return !PlateIsFree.WaitOne( 0);
            }
        }
        public PlateLocation CurrentLocation { get; set; }
        public PlateLocation DestinationLocation { get; set; }
        public Plate Plate { get; set; }
        public string Barcode{
            get{
                return Plate.Barcode;
            }
        }
        public string LabwareName{
            get{
                return Plate.Labware.Name;
            }
        }
        public int InstanceIndex { get; protected set; }

        public string GetStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine( String.Format( "Info for {0} plate S/N: {1}", this is ActiveSourcePlate ? "source" : "destination", PlateSerialNumber));
            sb.AppendLine( String.Format( "\tInstanceIndex: {0}", InstanceIndex));
            sb.AppendLine( String.Format( "\tBarcode: {0}", Barcode));
            sb.AppendLine( String.Format( "\tLabware: {0}", LabwareName));
            sb.AppendLine( String.Format( "\tBusy: {0}", Busy.ToString()));
            sb.AppendLine( String.Format( "\tCurrent location: {0}{1}{2}{3}", CurrentLocation.Name, CurrentLocation.Available ? ", available" : ", not available", CurrentLocation.Occupied.WaitOne(0) ? ", occupied" : ", not occupied", CurrentLocation.Reserved.WaitOne(0) ? ", reserved" : ", not reserved"));
            sb.AppendLine( String.Format( "\tDestination location: {0}{1}{2}{3}", DestinationLocation.Name, DestinationLocation.Available ? ", available" : ", not available", DestinationLocation.Occupied.WaitOne(0) ? ", occupied" : ", not occupied", CurrentLocation.Reserved.WaitOne(0) ? ", reserved" : ", not reserved"));
            sb.AppendLine( String.Format( "\tPlate is free: {0}", PlateIsFree.WaitOne(0) ? "yes" : "no"));
            sb.AppendLine( "\tToDoList:");
            for( int i=0; i<ToDoList.Count(); i++) {
                PlateTask task = ToDoList[i];
                sb.AppendLine( String.Format( "\t\tTask #{0}", i + 1));
                sb.AppendLine( String.Format( "\t\tDeviceType: {0}", task.DeviceType));
                sb.AppendLine( String.Format( "\t\tCommand: {0}", task.Command));
                sb.AppendLine( String.Format( "\t\tCompleted: {0}", task.Completed));
            }
            return sb.ToString();
        }

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        protected static readonly ILog Log = LogManager.GetLogger( typeof( ActivePlate));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        static ActivePlate()
        {
            ActivePlates = new List< ActivePlate>();
            PlateSerialNumberAutoIncrement = 0;
        }
        // ----------------------------------------------------------------------
        public ActivePlate( Worklist worklist, int instance_index)
        {
            InstanceIndex = instance_index;
            PlateIsFree = new ManualResetEvent( true);
            PlateSerialNumberAutoIncrement++;
            PlateSerialNumber = PlateSerialNumberAutoIncrement;
            Log.InfoFormat( "Created {0}", this);
            // CurrentLocation = new PlateLocation( ToString() + " Home");
            // CurrentLocation.Occupied.Set();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public PlateTask GetCurrentToDo()
        {
            return StillHaveToDos ? CurrentToDo.Current : null;
        }
        // ----------------------------------------------------------------------
        public void AdvanceCurrentToDo()
        {
            StillHaveToDos = CurrentToDo.MoveNext();
        }
        // ----------------------------------------------------------------------
        public virtual bool IsFinished()
        {
            return !Busy && !StillHaveToDos;
        }
        // ----------------------------------------------------------------------
        public void WaitForPlate()
        {
            DestinationLocation.Occupied.WaitOne();
        }
        // ----------------------------------------------------------------------
        public void MarkJobCompleted()
        {
            Log.DebugFormat( "Marking job completed for plate {0}, current location was {1}, destination location was {2}", this, CurrentLocation, DestinationLocation);
            AdvanceCurrentToDo();
            DestinationLocation.Reserved.Reset();
            CurrentLocation = DestinationLocation;
            PlateIsFree.Set();
        }
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return String.Format( "{0}{1}", GetType().Name, InstanceIndex);
        }
    }

    public class ActiveSourcePlate : ActivePlate
    {
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ActiveSourcePlate( Worklist worklist, int instance_index)
            : base( worklist, instance_index)
        {
            IList< PlateTask> bb_task_list = new List< PlateTask>{ new PlateTask( "Bumblebee", "source_hitpick")};
            ToDoList = new List< PlateTask>( worklist.TransferOverview.Tasks.source_prehitpick_tasks.Concat( bb_task_list).Concat( worklist.TransferOverview.Tasks.source_posthitpick_tasks));
            CurrentToDo = ToDoList.GetEnumerator();
            StillHaveToDos = CurrentToDo.MoveNext();
            Plate = worklist.SourcePlates[ instance_index];
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
    }

    public class ActiveDestinationPlate : ActivePlate
    {
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ActiveDestinationPlate( Worklist worklist, int instance_index)
            : base( worklist, instance_index)
        {
            IList< PlateTask> bb_task_list = new List< PlateTask>{ new PlateTask( "Bumblebee", "dest_hitpick")};
            ToDoList = new List< PlateTask>( worklist.TransferOverview.Tasks.dest_prehitpick_tasks.Concat( bb_task_list).Concat( worklist.TransferOverview.Tasks.dest_posthitpick_tasks));
            PlateTask load_task = ToDoList.FirstOrDefault( task => task.DeviceType == "Dock" && task.Command == "Load");
            if( load_task != null){
                string destination_device = worklist.TransferOverview.PlateStorageInterfaceName;
                load_task.ParametersAndVariables.Add( new PlateTask.Parameter( "device_instance", destination_device, ""));
            }
            CurrentToDo = ToDoList.GetEnumerator();
            StillHaveToDos = CurrentToDo.MoveNext();
            Plate = worklist.DestinationPlates[ instance_index];
            // DestinationPlate dst_plate = new DestinationPlate( worklist.TransferOverview.DestinationLabware, instance_index.ToString(), "A1:H12");
            // Plate = dst_plate;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
    }

    /*
    public class ActiveTipbox : ActivePlate
    {
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ActiveTipbox( Worklist worklist, int instance_index)
            : base( worklist, instance_index)
        {
            throw new NotImplementedException();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
    }
    */
}
