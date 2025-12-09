using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UcanDotNET;

namespace BioNex.Shared.CanDongleWrapper
{
    public interface ICanDongleDevice
    {
        byte NodeId { get; }
        byte DeviceId { get; }
        byte ChannelId { get; }
        void CANMessageRouting(USBcanServer.tCanMsgStruct can_msg);
    }
}
