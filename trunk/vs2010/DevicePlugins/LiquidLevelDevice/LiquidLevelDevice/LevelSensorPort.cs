using System;
using System.IO.Ports;
using BioNex.Shared.CanDongleWrapper;
using log4net;
using UcanDotNET;
using System.Diagnostics;
using System.Threading;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interface to abstract RS232 vs. CANOpen details
    /// </summary>
    internal interface ILevelSensorPort
    {
        string Name { get; }
        bool IsOpen { get; }
        int ReadTimeout { get; set; }

        void Open(bool reset_canbus = false);
        void Close();
        void DiscardInBuffer();
        void Write(string output);
        string ReadTo(string value);
        string ReadFromTo(string from, string to);
    }

    internal class LevelSensorPortSelector
    {
        public static ILevelSensorPort SelectPort(int can_device_id, int id)
        {
            return can_device_id == -1 ? (ILevelSensorPort)new LevelSensorRS232(id) : (ILevelSensorPort)new LevelSensorCANOpen(can_device_id, id);
        }
    }

    internal class SimulatedLevelSensorPort : ILevelSensorPort
    {
        private int _id;
        private bool _open;

        #region ILevelSensorPort Members
        public string Name { get { return String.Format("Sensor {0}", _id); } }
        public bool IsOpen { get { return _open; } }
        public int ReadTimeout { get; set; }
        public void Open(bool reset_canbus = false) { _open = true; }
        public void Close() { _open = false; }
        public void DiscardInBuffer() { }
        public void Write(string output) { }
        public string ReadTo(string value) { return ""; }
        public string ReadFromTo(string from, string to) { return ""; }
        #endregion

        public SimulatedLevelSensorPort(int id)
        {
            _id = id;
            ReadTimeout = 1000;
        }
    }

    internal class LevelSensorRS232 : ILevelSensorPort
    {
        SerialPort _port;
        public LevelSensorRS232(int id)
        {
            _port = new SerialPort(string.Format("com{0}", id), 115200, Parity.None, 8, StopBits.One);
        }

        public string Name { get { return _port.PortName; } }
        public bool IsOpen { get { return _port.IsOpen; } }
        public int ReadTimeout { get { return _port.ReadTimeout; } set { _port.ReadTimeout = value; } }
        public void Open(bool reset_canbus = false) { _port.Open(); }
        public void Close() { _port.Close(); }
        public void DiscardInBuffer() { _port.DiscardInBuffer(); }
        public void Write(string output) { _port.Write(output); }
        public string ReadTo(string value) { return _port.ReadTo(value); }
        public string ReadFromTo(string from, string to) { _port.ReadTo(from); return _port.ReadTo(to); }
    }

    internal class LevelSensorCANOpen : ILevelSensorPort, ICanDongleDevice
    {
        USBcanServer _usb_can_server;
        private static readonly ILog _log = LogManager.GetLogger(typeof(LevelSensorCANOpen));

        public LevelSensorCANOpen(int can_device_id, int id)
        {
            _CAN_systec_id = (byte)can_device_id;
            _CAN_node_id = (byte)id;
        }

        public string Name { get { return string.Format("CAN_{0}_{1}", _CAN_systec_id, _CAN_node_id); } }
        public bool IsOpen { get { return _is_open; } }
        public int ReadTimeout { get { return _read_timeout; } set { _read_timeout = value; } }
        public void Open(bool reset_canbus = false)
        {
            OpenCAN(reset_canbus);
        }
        public void Close()
        {
            CloseCAN();
        }
        public void DiscardInBuffer()
        {
            _ring_buffer.Discard();
        }
        public void Write(string output)
        {
            WriteCAN(output);
        }
        public string ReadTo(string value)
        {
            return ReadToCAN(value);
        }
        public string ReadFromTo(string from, string to)
        {
            ReadToCAN(from);
            return ReadToCAN(to);
        }

        // --------------------------------------------------------------- 
        // --------------------- implementation details ------------------ 
        // --------------------------------------------------------------- 

        public byte DeviceId { get { return _CAN_systec_id; } }
        public byte ChannelId { get { return 0; } }
        public byte NodeId { get { return _CAN_node_id; } }

        byte _CAN_systec_id;
        byte _CAN_node_id;
        int _read_timeout;
        bool _is_open;
        bool _got_wakeup;
        const int _channel = 0; // channel is always Zero for now, but could be 0 or 1 in the future if we bought a different Systec device

        // outgoing CAN messages
        USBcanServer.tCanMsgStruct[] _TPDO1_RTR_GET_INPUTS;
        USBcanServer.tCanMsgStruct[] _RSDO_GET_OUTPUTS;
        USBcanServer.tCanMsgStruct[] _RPDO1_SET_OUTPUTS;
        USBcanServer.tCanMsgStruct[] _NMT_STARTUP;
        USBcanServer.tCanMsgStruct[] _NMT_RESET;

        const int _ring_buffer_size = 16384;
        RingBuffer _ring_buffer;

        void OpenCAN(bool reset_bus)
        {
            Close();
            _ring_buffer = new RingBuffer(_ring_buffer_size);

            _usb_can_server = Dongle.Initialize(this);

            // create outgoing message buffers
            CreateOutgoingCANMessages();

            int bytes_sent = 1;
            byte ret;

            // Send out NMT Reset command to device
            _got_wakeup = !reset_bus; // ignore the wakeup message if we're resetting the bus
            if (reset_bus)
            {
                ret = _usb_can_server.WriteCanMsg(_channel, ref _NMT_RESET, ref bytes_sent);
                if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                {
                    _usb_can_server = null;
                    Dongle.Close(this);
                    throw new Exception(String.Format("LevelSensorCANOpen::OpenCAN Error - Node #{0} Sending NMT Reset returned {1}", _CAN_node_id, ret));
                }

                // Give the board a period to reset
                System.Threading.Thread.Sleep(10);

                // Send out NMT Start command to device
                ret = _usb_can_server.WriteCanMsg(_channel, ref _NMT_STARTUP, ref bytes_sent);
                if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                {
                    _usb_can_server = null;
                    Dongle.Close(this);
                    throw new Exception(String.Format("LevelSensorCANOpen::OpenCAN Error - Node #{0} Sending NMT Start returned {1}", _CAN_node_id, ret));
                }
            }

            _is_open = true;
        }

        void CloseCAN()
        {
            _is_open = false;
            if (_usb_can_server == null)
                return;

            _usb_can_server = null;
            Dongle.Close(this);
        }

        void CreateOutgoingCANMessages()
        {
            // TPDO1 gets the input state (RTR bit identifies this as a request to receive)
            _TPDO1_RTR_GET_INPUTS = new USBcanServer.tCanMsgStruct[1];
            _TPDO1_RTR_GET_INPUTS[0] = USBcanServer.tCanMsgStruct.CreateInstance(0x180 + _CAN_node_id, USBcanServer.USBCAN_MSG_FF_RTR);
            _TPDO1_RTR_GET_INPUTS[0].m_bDLC = 0;

            // SDO read current state of digital outputs at OD 0x6200, index 1
            _RSDO_GET_OUTPUTS = new USBcanServer.tCanMsgStruct[1];
            _RSDO_GET_OUTPUTS[0] = USBcanServer.tCanMsgStruct.CreateInstance(0x600 + _CAN_node_id, USBcanServer.USBCAN_MSG_FF_STD);
            _RSDO_GET_OUTPUTS[0].m_bDLC = 8;
            _RSDO_GET_OUTPUTS[0].m_bData[0] = 0x40; // client command specifier =1 (initiate download)
            _RSDO_GET_OUTPUTS[0].m_bData[1] = 0x00; // index low
            _RSDO_GET_OUTPUTS[0].m_bData[2] = 0x62; // index high
            _RSDO_GET_OUTPUTS[0].m_bData[3] = 0x01; // subindex
            _RSDO_GET_OUTPUTS[0].m_bData[4] = 0x00; // data0
            _RSDO_GET_OUTPUTS[0].m_bData[5] = 0x00; // data1
            _RSDO_GET_OUTPUTS[0].m_bData[6] = 0x00; // data2
            _RSDO_GET_OUTPUTS[0].m_bData[7] = 0x00; // data3

            // RPDO1 sets the output state 
            _RPDO1_SET_OUTPUTS = new USBcanServer.tCanMsgStruct[1];
            _RPDO1_SET_OUTPUTS[0] = USBcanServer.tCanMsgStruct.CreateInstance(0x200 + _CAN_node_id, USBcanServer.USBCAN_MSG_FF_STD);
            _RPDO1_SET_OUTPUTS[0].m_bDLC = 1;        // we will overwrite this on send
            _RPDO1_SET_OUTPUTS[0].m_bData[0] = 0x00; // we will overwrite this on send
            _RPDO1_SET_OUTPUTS[0].m_bData[1] = 0x00; // we will overwrite this on send
            _RPDO1_SET_OUTPUTS[0].m_bData[2] = 0x00; // we will overwrite this on send
            _RPDO1_SET_OUTPUTS[0].m_bData[3] = 0x00; // we will overwrite this on send
            _RPDO1_SET_OUTPUTS[0].m_bData[4] = 0x00; // we will overwrite this on send
            _RPDO1_SET_OUTPUTS[0].m_bData[5] = 0x00; // we will overwrite this on send
            _RPDO1_SET_OUTPUTS[0].m_bData[6] = 0x00; // we will overwrite this on send
            _RPDO1_SET_OUTPUTS[0].m_bData[7] = 0x00; // we will overwrite this on send

            // NMT command message for start_remote_node command
            _NMT_STARTUP = new USBcanServer.tCanMsgStruct[1];
            _NMT_STARTUP[0] = USBcanServer.tCanMsgStruct.CreateInstance(0x000, USBcanServer.USBCAN_MSG_FF_STD);
            _NMT_STARTUP[0].m_bDLC = 2;
            _NMT_STARTUP[0].m_bData[0] = 0x01;
            _NMT_STARTUP[0].m_bData[1] = 0x00; // _CAN_node_id; // ZERO for broadcast, or specific node address         --> broadcast to sync the blinky lights

            // NMT command message for reset_node command
            _NMT_RESET = new USBcanServer.tCanMsgStruct[1];
            _NMT_RESET[0] = USBcanServer.tCanMsgStruct.CreateInstance(0x000, USBcanServer.USBCAN_MSG_FF_STD);
            _NMT_RESET[0].m_bDLC = 2;
            _NMT_RESET[0].m_bData[0] = 0x82;
            _NMT_RESET[0].m_bData[1] = 0x00; // ZERO for broadcast, or specific node address         --> broadcast to sync the blinky lights
        }

        public void CANMessageRouting(USBcanServer.tCanMsgStruct can_msg)
        {
            int function_id = can_msg.m_dwID & 0x780; // top 4 bits is function 
            switch (function_id)
            {
                case 0x180:
                    HandleTPDO1(can_msg);
                    break;
                case 0x80:
                    HandleEmergency(can_msg);
                    break;
                case 0x700:
                    HandleNMTWakeup(can_msg);
                    break;
                case 0x580:
                    HandleTSDO(can_msg);
                    break;
            }
        }

        void HandleTPDO1(USBcanServer.tCanMsgStruct can_msg)
        {
            if (USBcanServer.USBCAN_MSG_FF_RTR == can_msg.m_bFF) // ignore RTR messages (probably don't see these ?)
                return;

            // put received bytes into our circular buffer so the READ routine can pull from buffer
            for (int i = 0; i < can_msg.m_bDLC; ++i)
                if (!_ring_buffer.Enqueue(can_msg.m_bData[i]))
                    _log.Error(String.Format("LevelSensorCANOpen::TPDO1Receive - buffer overflow while receiving data from node #{0} - either the buffer needs to be bigger, or reads need to happen sooner", _CAN_node_id));
        }


        void HandleEmergency(USBcanServer.tCanMsgStruct can_msg)
        {
            _log.Info(String.Format("LevelSensorCANOpen::EMCYReceive - Node #{0} ErrCode={1:x02}{2:x02} ErrReg={3:x02} ManufCode={4:x02}{5:x02}{6:x02}{7:x02}{8:x02}",
                _CAN_node_id,
                can_msg.m_bData[1], can_msg.m_bData[0], // err code
                can_msg.m_bData[2],                     // err register
                can_msg.m_bData[3], can_msg.m_bData[4], can_msg.m_bData[5], can_msg.m_bData[6], can_msg.m_bData[7]
                ));

            // Send out NMT Start command to our node (be sure we are really started, even in the case of an error condition)
            int bytes_sent = 1;
            byte ret = _usb_can_server.WriteCanMsg(_channel, ref _NMT_STARTUP, ref bytes_sent);
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                _log.Error(String.Format("LevelSensorCANOpen::EMCYReceive - Node #{0} Sending NMT Start after receiving EMCY returned {1}", _CAN_node_id, ret));
        }

        void HandleNMTWakeup(USBcanServer.tCanMsgStruct can_msg)
        {
            if (can_msg.m_bDLC < 1)
            {
                _log.Error(string.Format("LevelSensorCANOpen::NMTReceive - Received empty NMT message from our device at node #{0}.", _CAN_node_id));
                return;
            }

            bool wakeup = can_msg.m_bData[0] == 0x00;
            bool pre_operational = (can_msg.m_bData[0] & 0x7F) == 0x7F;
            bool operational = (can_msg.m_bData[0] & 0x05) == 0x05;
            if (wakeup && _got_wakeup)
                _log.Debug(string.Format("LevelSensorCANOpen::NMTReceive - Received NMT Wakeup message from our device at node #{0}. Entering operational state now.", _CAN_node_id));
            else if (pre_operational)
                _log.Debug(string.Format("LevelSensorCANOpen::NMTReceive - Received NMT Heartbeat message from our pre-operational device at node #{0}. Entering operational state now.", _CAN_node_id));
            else if (operational)
                _log.Debug(string.Format("LevelSensorCANOpen::NMTReceive - Received NMT Heartbeat message from our operational device at node #{0}.", _CAN_node_id));

            if ((wakeup && _got_wakeup) || pre_operational)
            {
                // Send out NMT Start command to broadcast
                int dwCount_send = 1;
                byte ret = _usb_can_server.WriteCanMsg(_channel, ref _NMT_STARTUP, ref dwCount_send);
                if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                {
                    _log.Error(String.Format("LevelSensorCANOpen::NMTReceive - Sending NMT Start to node #{0} returned {1}", _CAN_node_id, ret));
                    return;
                }
            }
            if (wakeup)
                _got_wakeup = true;
        }

        void HandleTSDO(USBcanServer.tCanMsgStruct can_msg)
        {
            return;
            // we may handle SDO responses in the future, but for now we're all about asynch
            /*
            if (0x4f != can_msg.m_bData[0]) // successful sdo upload
                return;

            if (0x00 != can_msg.m_bData[1]
             || 0x62 != can_msg.m_bData[2]
             || 0x01 != can_msg.m_bData[3]) // OD 0x6200:1 - digital outputs, ignore any other address reported
                return;
             */
        }

        void WriteCAN(string output)
        {
            // send an RPDO1 up to 8 bytes at a time 
            //    for (int i = 0; i < output.Length; ++i)

            int i = 0;
            while (i < output.Length)
            {
                byte size = (byte)Math.Min(output.Length - i, 8);
                int bytes_sent = size;

                for (int j = 0; j < size; ++j)
                    _RPDO1_SET_OUTPUTS[0].m_bData[j] = (byte)output[i + j];
                _RPDO1_SET_OUTPUTS[0].m_bDLC = size;
                // send the message
                byte ret = _usb_can_server.WriteCanMsg(_channel, ref _RPDO1_SET_OUTPUTS, ref bytes_sent);
                if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                {
                    _log.Error(String.Format("LevelSensorCANOpen::WriteCAN - Node #{0} sending RPDO1 returned {1} for index {2} of string '{3}'", _CAN_node_id, ret, i, output));
                    return;
                }
                i += size;
            }
        }

        string ReadToCAN(string value)
        {
            string received = "";
            bool timed_out = false;
            bool value_found = false;

            while (!(timed_out = !_ring_buffer.WaitForDataReady(_read_timeout)))
            {
                char next_byte = (char)_ring_buffer.Dequeue();
                received += next_byte;
                if (received.EndsWith(value))
                {
                    // emulate SerialPort ReadTo which discards "value"
                    received = received.Substring(0, received.Length - value.Length);
                    value_found = true;
                    break;
                }
            }

            if (timed_out && !value_found)
            {
                string message = string.Format("LevelSensorCANOpen::ReadToCAN CAN Node {0} Timed out while waiting for '{1}'.  Received {2} bytes : '{3}' so far.  Timeout period was {4} ms ",
                _CAN_node_id, value, received.Length, received, _read_timeout);
                _log.Debug(message);
                throw new TimeoutException(message);
            }

            return received;
        }
    }

    class RingBuffer
    {
        int _size;
        byte[] _buffer;
        int _start;
        int _end;
        ManualResetEvent _data_ready;

        public RingBuffer(int size)
        {
            _size = size + 1; // use one extra slot to detect full condition
            _buffer = new byte[_size];
            _start = 0;
            _end = 0;
            _data_ready = new ManualResetEvent(false);
        }

        public bool IsFull { get { lock (this) { return (_end + 1) % _size == _start; } } }
        public bool IsEmpty { get { lock (this) { return _start == _end; } } }

        public bool WaitForDataReady(int timeout_ms) { return _data_ready.WaitOne(timeout_ms); }

        public bool Enqueue(byte data)
        {
            lock (this)
            {
                if (IsFull)
                    _start = (++_start) % _size; // maintain indices at least 1 byte apart if we're full
                _buffer[_end] = data;
                _end = (++_end) % _size;

                _data_ready.Set();
                return !IsFull;
            }
        }

        public byte Dequeue()
        {
            lock (this)
            {
                if (IsEmpty) // you should check empty before reading
                {
                    _data_ready.Reset();
                    return 0;
                }
                byte data = _buffer[_start];
                _start = (++_start) % _size;
                if (IsEmpty)
                    _data_ready.Reset();
                return data;
            }
        }

        public void Discard()
        {
            lock (this)
            {
                _data_ready.Reset();
                _start = _end = 0;
            }
        }
    }
}


