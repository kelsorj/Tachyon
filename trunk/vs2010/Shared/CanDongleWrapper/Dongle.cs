using System;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using UcanDotNET;

namespace BioNex.Shared.CanDongleWrapper
{
    public static class Dongle
    {
        class CANMessageRouter
        {
            public USBcanServer Server {get; private set;}

            readonly int _CAN_systec_id;
            // incoming CAN message buffers
            const int _can_msgs_max = 32;

            /// lookup table for getting the callback for a particular node ID
            readonly Dictionary<int, ICanDongleDevice> _client_lookup = new Dictionary<int, ICanDongleDevice>(); // key is node_id

            USBcanServer.tCanMsgStruct[] _can_msgs;

            
            public CANMessageRouter(int id) 
            {
                _CAN_systec_id = id;
                Server = new USBcanServer();
                Server.CanMsgReceivedEvent += new USBcanServer.CanMsgReceivedEventEventHandler(CANMessageReceivedEventHandler);
                Server.StatusEvent += new USBcanServer.StatusEventEventHandler(CANStatusEventHandler);

                // create incoming message buffers
                _can_msgs = new USBcanServer.tCanMsgStruct[_can_msgs_max];
                for (int n = 0; n < _can_msgs_max; ++n)
                    _can_msgs[n] = USBcanServer.tCanMsgStruct.CreateInstance(0, 0);
            }
            void CANStatusEventHandler(byte channel)
            {
                lock (this)
                {
                    USBcanServer.tStatusStruct status = new USBcanServer.tStatusStruct();
                    byte ret = Server.GetStatus(channel, ref status);

                    _log.DebugFormat( "CanDongleWrapper::StatusEvent - Status event happened on Systec Dev#{0}: CAN_Status={1}, USB_Status={2}, ret={3}", _CAN_systec_id, status.m_wCanStatus, status.m_wUsbStatus, ret);
                    Server.ResetCan(channel, USBcanServer.USBCAN_RESET_ONLY_STATUS);
                }
            }
            void CANMessageReceivedEventHandler(byte channel)
            {
                lock (this)
                {
                    int dwCount = _can_msgs_max;
                    byte ret = Server.ReadCanMsg(ref channel, ref _can_msgs, ref dwCount);
                    if (USBcanServer.USBCAN_SUCCESSFUL != ret)
                    {
                        _log.ErrorFormat( "CanDongleWrapper::CANReceive Error - ReadCanMsg failed on Dev#{0}: error code {1}", _CAN_systec_id, ret);
                        return;
                    }

                    // route the messages to the correct device instance based on node ID
                    for (int n = 0; n < dwCount; ++n)
                    {
#if false
                        _log.DebugFormat( "IOX1::CanMsgReceivedEventHdlr - CanMsg: ID={0:x03} DLC={1} {2:x02} {3:x02} {4:x02} {5:x02} {6:x02} {7:x02} {8:x02} {9:x02}",
                        can_msgs[n].m_dwID, can_msgs[n].m_bDLC, can_msgs[n].m_bData[0], can_msgs[n].m_bData[1], can_msgs[n].m_bData[2],
                        can_msgs[n].m_bData[3], can_msgs[n].m_bData[4], can_msgs[n].m_bData[5], can_msgs[n].m_bData[6],
                        can_msgs[n].m_bData[7]);
#endif

                        // call approriate MessageRouting(_can_msgs[n]) method
                        int node_id = _can_msgs[n].m_dwID & 0x7F;      // bottom 7 bits is node

                        if (_client_lookup.ContainsKey(node_id) && _client_lookup[node_id] != null)
                            _client_lookup[node_id].CANMessageRouting(_can_msgs[n]);
                    }
                }
            }
            public void AddClient(ICanDongleDevice device)
            {
                lock (this)
                {
                    _client_lookup[device.NodeId] = device;
                }
            }
            public void RemoveClient(byte node_id)
            {
                lock (this)
                {
                    _client_lookup.Remove(node_id);
                }
            }
        }

        /// <summary>
        /// keeps track of number of references to a particular USBcanServer instance
        /// </summary>
        private static readonly Dictionary<int,int> _refcounts = new Dictionary<int,int>(); // key is systec_id
        /// <summary>
        /// lookup table for getting a USBcanServer instance, based on the connection properties
        /// </summary>
        private static readonly Dictionary<int,CANMessageRouter> _refs = new Dictionary<int,CANMessageRouter>(); // key is systec_id

        private static readonly object _static_lock = new object();

        private static readonly ILog _log = log4net.LogManager.GetLogger(typeof(Dongle));

        public static USBcanServer Initialize( ICanDongleDevice device)
        {
            CANMessageRouter router = null;
            lock (_static_lock)
            {
                if (_refs.ContainsKey(device.DeviceId))
                {
                    // already instantiated a USBcanServer object with these connection properties, so
                    // just increment the reference count
                    Debug.Assert(_refcounts.ContainsKey(device.DeviceId));
                    _refcounts[device.DeviceId]++;
                }
                else
                {
                    _refs[device.DeviceId] = new CANMessageRouter(device.DeviceId);
                    _refcounts[device.DeviceId] = 1;
                }
                router = _refs[device.DeviceId];
            }

            lock (router)
            {
                USBcanServer adapter = router.Server;
                router.AddClient(device);

                // initialize the adapter
                byte ret = adapter.InitHardware(device.DeviceId);
                if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                {
                    Close(device);
                    throw new Exception(String.Format("CanDongleWrapper::Initialize Error - InitHardware returned {0} for adapter with device ID {1}", ret, device.DeviceId));
                }

                // device channel is always 0 since we only use single port usb can adapters --> if we start using multi-channel devices, we may need to revisit this stuff (especially the dictionaries)
                ret = adapter.InitCan(device.ChannelId, USBcanServer.USBCAN_BAUD_500kBit, USBcanServer.USBCAN_BAUDEX_USE_BTR01,
                        USBcanServer.USBCAN_AMR_ALL, USBcanServer.USBCAN_ACR_ALL, (byte)USBcanServer.tUcanMode.kUcanModeNormal,
                        USBcanServer.USBCAN_OCR_DEFAULT);
                if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                {
                    Close(device);
                    throw new Exception(String.Format("CanDongleWrapper::Initialize Error - InitCan returned {0} for adapter with device ID {1}", ret, device.DeviceId));
                }

                return adapter;
            }
        }

        public static void Close( ICanDongleDevice device)
        {
            lock (_static_lock)
            {
                // decrement refcount
                _refcounts[device.DeviceId]--;
                CANMessageRouter router = _refs[device.DeviceId];
                lock (router)
                {
                    USBcanServer adapter = router.Server;
                    router.RemoveClient(device.NodeId);

                    // destroy USBcanServer if it's no longer used
                    if (_refcounts[device.DeviceId] == 0)
                    {
                        adapter.Shutdown();
                        adapter.Dispose();
                        _refs.Remove(device.DeviceId);
                    }
                }
            }
        }
    }
}
