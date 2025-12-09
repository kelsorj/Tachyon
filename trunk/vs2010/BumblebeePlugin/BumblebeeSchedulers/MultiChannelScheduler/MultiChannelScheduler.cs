using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.BumblebeeAlphaGUI;
using BioNex.Shared.PlateDefs;

namespace BioNex.BumblebeePlugin.Scheduler.MultiChannelScheduler
{
    [ Export( typeof( IScheduler))]
    public class MultiChannelScheduler : IScheduler
    {
#region IScheduler Members
        void IScheduler.SetDeviceInterface( AccessibleDeviceInterface device_interface)
        {
            throw new NotImplementedException();
        }

        void IScheduler.SetHardware( AlphaHardware hw)
        {
            throw new NotImplementedException();
        }

        void IScheduler.SetTeachpoints( Teachpoints tps)
        {
            throw new NotImplementedException();
        }

        void IScheduler.SetTipHandlingMethod( string method_name)
        {
            throw new NotImplementedException();
        }

        void IScheduler.StartProcess( TransferOverview to)
        {
            throw new NotImplementedException();
        }

        void IScheduler.Reset()
        {
            throw new NotImplementedException();
        }

        string IScheduler.GetSchedulerName()
        {
            throw new NotImplementedException();
        }

        void IScheduler.Pause()
        {
            throw new NotImplementedException();
        }

        void IScheduler.Resume()
        {
            throw new NotImplementedException();
        }

        void IScheduler.Abort()
        {
            throw new NotImplementedException();
        }

        void IScheduler.SetConfiguration( BumblebeeConfiguration config)
        {
            throw new NotImplementedException();
        }
#endregion
    }
}
