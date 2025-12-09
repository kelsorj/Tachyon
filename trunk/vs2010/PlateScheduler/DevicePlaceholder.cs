// DEVICE PLACEHOLDER HAS BEEN COMPLETELY ELIMINATED.
/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using log4net;

namespace BioNex.PlateScheduler
{
    /*
    public abstract class IDevicePlaceholder
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public string Name { get; protected set; }

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        protected static readonly ILog Log = LogManager.GetLogger( typeof( IDevicePlaceholder));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public IDevicePlaceholder( string name)
        {
            Name = name;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public abstract PlateLocation GetAvailableLocation( ActivePlate active_plate);
        public abstract void MakeLocationAvailable();
        public abstract void AddJob( ActivePlate active_plate);
    }

    /*
    public class DevicePlaceholder : IDevicePlaceholder
    {
        // protected ManualResetEvent LocationIsFree = new ManualResetEvent( true);
        protected PlateLocation DeviceLocation { get; set; }

        public DevicePlaceholder( string name)
            : base( name)
        {
            DeviceLocation = new PlateLocation( name + " Location");
        }

        public override PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            if( DeviceLocation.GetState() == PlateLocation.State.FREE){
                return DeviceLocation;
            } else{
                return null;
            }
        }

        public override void MakeLocationAvailable()
        {
            throw new NotImplementedException();
        }

        public override void AddJob( ActivePlate active_plate)
        {
            Execute( active_plate);
        }

        public virtual void Execute( ActivePlate active_plate)
        {
            Thread execute_thread = new Thread( () => ExecuteThreadRunner( active_plate, active_plate.GetCurrentToDo().Command)){ Name = GetType().ToString(), IsBackground = true};
            execute_thread.Start();
        }

        protected void ExecuteThreadRunner( ActivePlate active_plate, string command)
        {
            // TO DO. wait for robot to deliver plate.
            active_plate.PlateIsReadyForTask.WaitOne();
            // simulate actual running.
            Random random = new Random();
            Thread.Sleep( random.Next( 300, 500));
            // log completion.
            Log.InfoFormat( "Executed {0} on plate {1}", command, active_plate);
            // clean up activities...
            active_plate.MarkJobCompleted();
        }
    }

    /*
    public class NullDevicePlaceholder : IDevicePlaceholder
    {
        public NullDevicePlaceholder( string name)
            : base( name)
        {
        }

        public override PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            throw new NotImplementedException();
        }

        public override void MakeLocationAvailable()
        {
            throw new NotImplementedException();
        }

        public override void AddJob( ActivePlate active_plate)
        {
            throw new NotImplementedException();
        }

        protected void ExecuteThreadRunner( ActivePlate active_plate, string command)
        {
            Log.InfoFormat( "Executed {0} on plate {1}", command, active_plate);
            active_plate.MarkJobCompleted();
        }
    }

    /*
    public class BumblebeePlaceholder : IDevicePlaceholder
    {
        private PlateLocation[] DeviceLocations { get; set; }
        private Dictionary< ActivePlate, PlateLocation> LocationAssignment = new Dictionary< ActivePlate, PlateLocation>();

        public BumblebeePlaceholder()
            : base( "bb")
        {
            int places = 3;
            DeviceLocations = new PlateLocation[ places];
            for( int loop = 0; loop < places; loop++){
                DeviceLocations[ loop] = new PlateLocation( String.Format( "{0} Location {1}", Name, loop));
            }
        }

        public override PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            foreach( PlateLocation DeviceLocation in DeviceLocations){
                if( DeviceLocation.GetState() == PlateLocation.State.FREE){
                    return DeviceLocation;
                }
            }
            return null;
        }

        public override void MakeLocationAvailable()
        {
            throw new NotImplementedException();
        }

        public override void AddJob( ActivePlate active_plate)
        {
            Thread execute_thread = new Thread( () => ExecuteThreadRunner( active_plate, active_plate.GetCurrentToDo().Command)){ Name = GetType().ToString(), IsBackground = true};
            execute_thread.Start();
        }

        protected void ExecuteThreadRunner( ActivePlate active_plate, string command)
        {
            Random random = new Random();
            Thread.Sleep( random.Next( 1500, 2000));
            Log.InfoFormat( "Executed {0} on plate {1}", command, active_plate);
            active_plate.MarkJobCompleted();
        }
    }
    */

    /*
    public class DeviceManagerPlaceholder
    {
        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        public IDictionary< string, IDevicePlaceholder> Devices = new Dictionary< string, IDevicePlaceholder>();

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public DeviceManagerPlaceholder()
        {
            // Devices[ "robot"] = new RobotPlaceholder();
            // Devices[ "bb"] = new BumblebeePlaceholder();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public IDevicePlaceholder GetDevice( string device_name)
        {
            if( !Devices.ContainsKey( device_name)){
                // create required device out of thin air.
                IDevicePlaceholder device = new DevicePlaceholder( device_name);
                Devices[ device_name] = device;
            }
            return Devices[ device_name];
        }
        // ----------------------------------------------------------------------
        public void SetDevice( IDevicePlaceholder device, string device_name)
        {
            Devices[ device_name] = device;
        }
    }
    */
// }
