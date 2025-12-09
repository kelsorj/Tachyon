using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.PlateWork;
using System.ComponentModel.Composition;

namespace BioNex.DummyPlateScheduler
{
    [Export(typeof(IPlateScheduler))]
    [Export(typeof(IRobotScheduler))]
    public class DummyScheduler : IPlateScheduler, IRobotScheduler
    {
        #region IPlateScheduler Members

        public void StartScheduler()
        {
        }

        public void StopScheduler()
        {
        }

        public void EnqueueWorklist(Worklist worklist)
        {
        }

        #endregion

        #region IReportsStatus

        public string GetStatus() { return "none"; }        

        #endregion

        #region IRobotScheduler Members

        public event EventHandler EnteringMovePlate;

        public event EventHandler ExitingMovePlate;

        public void AddJob(ActivePlate active_plate)
        {
            return;
        }

        public void AddJob(string src_device_name, string src_location_name, string dst_device_name, string dst_location_name, string labware_name)
        {
            return;
        }

        #endregion
    }
}
