using System;
using System.Collections.Generic;
using System.Threading;
using BioNex.Shared.CanDongleWrapper;
using log4net;
using UcanDotNET;

namespace Systec_IO
{
    public class IOX1 : InternalIOInterface, ICanDongleDevice
    {
        public enum bit_state { clear = 0, set = 1 };
        public event IOXInputChangedEvent IOXInputChanged;

        // ICanDongleDevice
        public byte NodeId { get; private set; }
        public byte DeviceId { get; private set; } // device number of CAN adapter (#0 - DC2, #1 - BB2, #2 - IO2)
        public byte ChannelId
        {
            get { return 0; } // channel number on CAN adapter (always 0 for us since we use single channel USB-CANmodul adapters)
        }
        public bool Initialized { get; private set; }

        public int NumberOfInputs { get { return _input_bytes * 8; } }
        public int NumberOfOutputs { get { return _output_bytes * 8; } }

        public IOX1()
        {
        } // IOX1 constructor


        public void Close()
        {
            if (!Initialized)
                return;

            _kill_threads = true;
            _input_polling_thread.Join();
            BioNex.Shared.CanDongleWrapper.Dongle.Close(this);
            Initialized = false;
        }

        public int Initialize(int IOX1_base_addr, int device, int channel, int inputBytes = 2, int outputBytes = 1)
        {
            string err_msg = "";
            if (outputBytes > 8 || outputBytes < 1)
                err_msg = "IOX1::Initialize - output bytes must be 1 <= n <= 8";
            if (inputBytes > 8 || inputBytes < 1)
                err_msg = "IOX1::Initialize - input bytes must be 1 <= n <= 8";
            if (IOX1_base_addr < 1 || IOX1_base_addr > 127)
                err_msg = "IOX1::Initialize - CAN Node ID must be 1 <= n <= 127";

            if (!string.IsNullOrWhiteSpace(err_msg))
            {
                LogError(err_msg);
                throw new ArgumentException(err_msg);
            }

            NodeId = (byte)IOX1_base_addr;
            DeviceId = (byte)device;
            _input_bytes = (byte)inputBytes;
            _output_bytes = (byte)outputBytes;

            _inputState = new byte[_input_bytes];
            _outputState = new byte[_output_bytes];

            _kill_threads = false;

            try
            {
                _USBcanServer_object = BioNex.Shared.CanDongleWrapper.Dongle.Initialize(this);
            }
            catch (Exception ex)
            {
                return LogError(ex.Message);
            }

            CreateOutgoingCANMsgs();

            // Unused int dll_ver = m_USBcanServer_object.GetUserDllVersion();
            //_log.DebugFormat( "User Dll: Ver={0}, Rev={1}, Release={2}", dll_ver & 0xff, (dll_ver & 0xff00) >> 8, (dll_ver & 0xff0000) >> 16);

            byte ret;

            int dwCount_send = 0;
            lock (_IOX1_lock)
            {
                // Send out NMT Start command to device
                ret = _USBcanServer_object.WriteCanMsg(ChannelId, ref NMT_STARTUP, ref dwCount_send);
            }
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                BioNex.Shared.CanDongleWrapper.Dongle.Close(this);
                return LogError(String.Format("IOX1::Initialize - Sending NMT Start returned {0}", ret));
            }

            //set up Polling thread and launch it
            _input_polling_thread = new Thread(InputPollingThread);
            _input_polling_thread.Name = "I/O input polling thread";
            _input_polling_thread.Start();

            // DKM 2010-09-17 cannot block here, because if the power is off, Synapsis will not load
            //                add timeout, and I need to add a "reinitialize all devices" menu item
            if (WaitHandle.WaitAny(new WaitHandle[] { InitializedWaitForInputsRefresh }, 3000) == WaitHandle.WaitTimeout)
                return LogError("IOX1::Initialize - Timed out waiting for input status during initialization -- check power");
            if (WaitHandle.WaitAny(new WaitHandle[] { InitializedWaitForOutputsRefresh }, 3000) == WaitHandle.WaitTimeout)
                return LogError("IOX1::Initialize - Timed out waiting for output status during initialization -- check power");

            Initialized = true;
            return 0;
        } // Initialize(...)

        public byte[] ReadInputs()
        {
            return _inputState;
        }

        public bit_state ReadInput(int channel_0based)
        {
            int byte_index = (int)(channel_0based / 8);
            int bit_index = channel_0based % 8;
            if (channel_0based < 0 || byte_index > _input_bytes)
            {
                string msg = string.Format("IOX1::ReadInput - channel must be 0 <= n <= {0}, but was '{1}'", (_input_bytes * 8) - 1, channel_0based);
                LogError(msg);
                throw new System.ArgumentException(msg);
            }

            byte[] inputs = ReadInputs();
            return 0 != (inputs[byte_index] & (1 << bit_index)) ? bit_state.set : bit_state.clear;
        }

        public bit_state ReadOutput(int channel_0based)
        {
            int byte_index = (int)(channel_0based / 8);
            int bit_index = channel_0based % 8;
            if (channel_0based < 0 || byte_index > _output_bytes)
            {
                string msg = string.Format("IOX1::ReadOutput - channel must be 0 <= n <= {0}, but was '{1}'", (_output_bytes * 8) - 1, channel_0based);
                LogError(msg);
                throw new System.ArgumentException(msg);
            }

            byte[] outputs = GetOutputState();
            return 0 != (outputs[byte_index] & (1 << bit_index)) ? bit_state.set : bit_state.clear;
        }

        public void WriteOutputs(Byte[] outputs)
        {
            if (outputs.Length != _output_bytes)
            {
                string msg = string.Format("IOX1::WriteOutputs - array length '{0}' does not match output size '{1}'", outputs.Length, _output_bytes);
                LogError(msg);
                throw new System.ArgumentException(msg);
            }

            byte ret;
            int dwCount_send = 1;
            lock (_IOX1_lock)
            {
                // Send out RPDO1 to set outputs
                for (int i = 0; i < _output_bytes; ++i)
                    RPDO1_SET_OUTPUTS[0].m_bData[i] = outputs[i];
                ret = _USBcanServer_object.WriteCanMsg(ChannelId, ref RPDO1_SET_OUTPUTS, ref dwCount_send);
                if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                {
                    LogError(String.Format("IOX1::WriteOutputs - Sending RPDO1 returned {0}", ret));
                    return;
                }
                for (int i = 0; i < _output_bytes; ++i)
                    _outputState[i] = outputs[i];
            }
        }

        public byte[] GetOutputState() { return _outputState; }

        public void WriteOutput(int channel_0based, bit_state state)
        {
            int byte_index = (int)(channel_0based / 8);
            int bit_index = channel_0based % 8;
            if (channel_0based < 0 || byte_index > _output_bytes)
            {
                string msg = string.Format("IOX1::WriteOutput - channel must be 0 <= n <= {0}, but was '{1}'", (_output_bytes * 8) - 1, channel_0based);
                LogError(msg);
                throw new System.ArgumentException(msg);
            }

            lock (_IOX1_lock)
            {
                byte[] outputs = GetOutputState();
                outputs[byte_index] = (byte)(state == bit_state.set ? (outputs[byte_index] | (1 << bit_index)) : (outputs[byte_index] & ~(1 << bit_index)));
                WriteOutputs(outputs);
            }
        }

        public void ToggleOutput(int channel_0based)
        {
            int byte_index = (int)(channel_0based / 8);
            int bit_index = channel_0based % 8;
            if (channel_0based < 0 || byte_index > _output_bytes)
            {
                string msg = string.Format("IOX1::ToggleOutput - channel must be 0 <= n <= {0}, but was '{1}'", (_output_bytes * 8) - 1, channel_0based);
                LogError(msg);
                throw new System.ArgumentException(msg);
            }

            lock (_IOX1_lock)
            {
                byte[] outputs = GetOutputState();
                outputs[byte_index] = (byte)(0 == (outputs[byte_index] & (1 << bit_index)) ? (outputs[byte_index] | (1 << bit_index)) : (outputs[byte_index] & ~(1 << bit_index)));
                WriteOutputs(outputs);
            }
        }

        // Logical OR operation with outputs and outputs_bitmask
        public void SetOutputs(byte[] outputs_bitmask)
        {
            if (outputs_bitmask.Length != _output_bytes)
            {
                string msg = string.Format("IOX1::SetOutputs - array length '{0}' does not match output size '{1}'", outputs_bitmask.Length, _output_bytes);
                LogError(msg);
                throw new System.ArgumentException(msg);
            }
            lock (_IOX1_lock)
            {
                byte[] outputs = GetOutputState();
                for (int i = 0; i < _output_bytes; ++i)
                    outputs[i] |= outputs_bitmask[i];
                WriteOutputs(outputs);
            }
        }

        // Logical AND operation with outputs and ~outputs_bitmask 
        public void ClearOutputs(byte[] outputs_bitmask)
        {
            if (outputs_bitmask.Length != _output_bytes)
            {
                string msg = string.Format("IOX1::ClearOutputs - array length '{0}' does not match output size '{1}'", outputs_bitmask.Length, _output_bytes);
                LogError(msg);
                throw new System.ArgumentException(msg);
            }
            lock (_IOX1_lock)
            {
                byte[] outputs = GetOutputState();
                for (int i = 0; i < _output_bytes; ++i)
                    outputs[i] &= (byte)~outputs_bitmask[i];
                WriteOutputs(outputs);
            }
        }

        public void SetInputName(int channel_0based, string name) { }
        public void SetOutputName(int channel_0based, string name) { }

        // ICanDongleDevice
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

        private UcanDotNET.USBcanServer _USBcanServer_object = null;
        private volatile bool _kill_threads = false;
        private volatile bool _pause_polling = false;
        private const double _polling_frequency_Hz = 10.0; // polls per second default to 10Hz. Should be fast enough to refresh state since the I/O automatically sends TPDO1 messages on Input change
        private readonly object _IOX1_lock = new object(); // mutal exclusion lock to prevent race conditions with toggling/setting/clearing individual Outputs


        // I/O Setup --
        //
        //   Currently this driver reads Inputs and Writes outpus using 1 PDO
        //   A single PDO has a maximum of 8 bytes of data, so theoretically we can support 64 ins and 64 outs
        //   Reading outputs via SDO would requires one transfer per byte
        //
        //

        private byte _input_bytes;  // number of bytes to expect the device to provide when reading inputs
        private byte _output_bytes; // number of bytes to expect the device to support when writing outputs

        private volatile byte[] _inputState;
        private volatile byte[] _outputState;       // desired output state on device

        private DateTime _lastInputStateUpdateTime = DateTime.Now;
        private DateTime _lastOutputStateUpdateTime = DateTime.Now;

        // setup outgoing CAN messages
        private USBcanServer.tCanMsgStruct[] TPDO1_RTR_GET_INPUTS;
        private USBcanServer.tCanMsgStruct[] RSDO_GET_OUTPUTS;
        private USBcanServer.tCanMsgStruct[] RPDO1_SET_OUTPUTS;
        private USBcanServer.tCanMsgStruct[] NMT_STARTUP;

        private static ILog _log;
        private void LogDebug(string msg)
        {
            if (_log == null)
                _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            System.Console.WriteLine(msg);
            if (_log.IsDebugEnabled) _log.Debug(msg);
        }
        private int LogError(string msg)
        {
            if (_log == null)
                _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            System.Console.WriteLine(msg);
            _log.Error(msg);
            return -1; // to simplify use from logged returns
        }

        private readonly EventWaitHandle InitializedWaitForInputsRefresh = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly EventWaitHandle InitializedWaitForOutputsRefresh = new EventWaitHandle(false, EventResetMode.AutoReset);


        ~IOX1() // not IDisposable, so this is deferred until GC ... 
        {
            Close();
        }

        private Thread _input_polling_thread;
        private void InputPollingThread()
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "I/O input polling";

            // this loop appears to be intended to make polls occur at a clock interval that comes close as possible to matching our desired frequency
            // if it misses a poll, it skips the poll rather than introduce another one, so the worst case is that you get 1/2 the frequency that you asked for (...assuming you achieve that...), 
            // rather than a non-deterministic frequency.

            int poll_period_ms = (int)(1000.0 / Math.Min(Math.Max(_polling_frequency_Hz, 0.2), 100.0));
            DateTime NextRunTime = DateTime.Now; // set it up to have the timer expire immediately

            while (!_kill_threads)
            {
                DateTime now = DateTime.Now;

                if (NextRunTime <= now)
                {
                    if (!_pause_polling)
                    {
                        byte ret = (byte)0xff;
                        int dwCount_send = 1;

                        lock (_IOX1_lock)
                        {
                            ret = _USBcanServer_object.WriteCanMsg(ChannelId, ref TPDO1_RTR_GET_INPUTS, ref dwCount_send);
                        }
                        if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                        {
                            LogDebug(String.Format("IOX1::InputPollingThread Error - Sending RTR for TPDO1 returned {0}", ret));
                        }

                        for (int i = 0; i < _output_bytes; ++i)
                        {
                            lock (_IOX1_lock)
                            {
                                // Send SDO Upload to read current state of digital outputs at OD 0x6200, index 1                            
                                RSDO_GET_OUTPUTS[0].m_bData[3] = (byte)(i + 1); // subindex
                                ret = _USBcanServer_object.WriteCanMsg(ChannelId, ref RSDO_GET_OUTPUTS, ref dwCount_send);
                            }
                            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                                LogDebug(String.Format("IOX1::InputPollingThread Error - Sending SDO for OD 0x6200:{0} returned {1}", i, ret));
                        }
                    } // if (!m_pause_polling)
                    NextRunTime = NextRunTime.AddMilliseconds(poll_period_ms);
                } // if (NextRunTime <= Now)

                int sleep_time = (int)Math.Min(20, (NextRunTime - now).TotalMilliseconds); // sleep for a max of 100 milliseconds, less if we are expected to poll soon
                if (sleep_time < 0)
                {
                    //LogDebug("Missed at least one deadline for polling loop. Oh well. Life moves on.."); // skip ahead to the next polling loop
                    NextRunTime = DateTime.Now.AddMilliseconds(poll_period_ms);
                    sleep_time = Math.Min(20, poll_period_ms);
                }

                Thread.Sleep(sleep_time);
            }
        }

        private void HandleTPDO1(USBcanServer.tCanMsgStruct can_msg)
        {
            if (USBcanServer.USBCAN_MSG_FF_RTR == can_msg.m_bFF) // ignore RTR messages (probably don't see these ?)
                return;

            if (can_msg.m_bDLC != _input_bytes) // check message length, expect 2 bytes
            {
                LogDebug(String.Format("IOX1::TPDO1Receive - Expecting DLC to be {0}, but it is {1}. Ignoring this TPDO1.", _input_bytes, can_msg.m_bDLC));
                return;
            }

            FireEventOnInputStateChange(can_msg.m_bData);

            for (int i = 0; i < _input_bytes; ++i)
                _inputState[i] = can_msg.m_bData[i];

            _lastInputStateUpdateTime = DateTime.Now;
            InitializedWaitForInputsRefresh.Set();
        }

        private void FireEventOnInputStateChange(byte[] state)
        {
            // DKM 2011-04-21 just a "simple" event to notify client that the input states have changed.
            // I already have stuff in place to read the input bit of interest so didn't want to
            // create specialized event args.
            bool state_changed = false;
            for (int i = 0; i < _input_bytes; ++i)
                if (_inputState[i] != state[i])
                {
                    state_changed = true;
                    break;
                }

            if (state_changed && IOXInputChanged != null)
            {
                var indices = new List<int>();
                var values = new List<bool>();
                for (int i = 0; i < _input_bytes; ++i)
                    for (int j = 0; j < 8; ++j)
                    {
                        if ((state[i] & (1 << j)) != (_inputState[i] & (1 << j)))
                        {
                            indices.Add(j);
                            values.Add((state[i] & (1 << j)) != 0);
                        }
                    }

                IOXInputChanged(this, new IOXEventArgs(indices, values));
            }
        }

        private void HandleEmergency(USBcanServer.tCanMsgStruct can_msg)
        {
            LogDebug(String.Format("IOX1::CanMsgReceivedEventHdlr - EMCY Message: ErrCode={0:x02}{1:x02} ErrReg={2:x02} ManufCode={3:x02}{4:x02}{5:x02}{6:x02}{7:x02}",
                can_msg.m_bData[1], can_msg.m_bData[0], // err code
                can_msg.m_bData[2],                     // err register
                can_msg.m_bData[3], can_msg.m_bData[4], can_msg.m_bData[5], can_msg.m_bData[6], can_msg.m_bData[7]
                ));

            int dwCount_send = 1;
            byte ret;
            lock (_IOX1_lock)
            {
                // Send out NMT Start command to broadcast (be sure we are really started, even in the case of an error condition)
                ret = _USBcanServer_object.WriteCanMsg(ChannelId, ref NMT_STARTUP, ref dwCount_send);
            }
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
                LogDebug(String.Format("IOX1::CanMsgReceivedEventHdlr - Sending NMT Start after receiving EMCY returned {0}", ret));
        }

        private void HandleNMTWakeup(USBcanServer.tCanMsgStruct can_msg)
        {
            LogDebug("IOX1::CanMsgReceivedEventHdlr - Received NMT Wakeup message from our device. Putting him into Operational state now.");

            int dwCount_send = 1;
            byte ret;
            lock (_IOX1_lock)
            {
                // Send out NMT Start command to broadcast
                ret = _USBcanServer_object.WriteCanMsg(ChannelId, ref NMT_STARTUP, ref dwCount_send);
            }
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                LogDebug(String.Format("IOX1::CanMsgReceivedEventHdlr - Sending NMT Start after receiving NMT Start returned {0}", ret));
                return;
            }
            _pause_polling = false; // be sure we are polling him
            LogDebug("IOX1::CanMsgReceivedEventHdlr - Sending last cached value of outputs to IOX1 device after sending NMT START following the Received NMT Wakeup message.");
            WriteOutputs(_outputState); // this should set the last known outputs again
        } // NMT wakeup

        private void HandleTSDO(USBcanServer.tCanMsgStruct can_msg)
        {
            if (0x4f != can_msg.m_bData[0]) // successful sdo upload
                return;

            if (0x00 != can_msg.m_bData[1]
             || 0x62 != can_msg.m_bData[2])  // OD 0x6200: - digital outputs, ignore any other address reported
                return;

            int byte_index = can_msg.m_bData[3] - 1;
            if (byte_index < 0 || byte_index >= _output_bytes)
                return;


            // -- POLLING SDO needs to increment address for every IO byte -- we only get 1 byte of IO per SDO poll
            lock (_IOX1_lock)
            {
                _outputState[byte_index] = can_msg.m_bData[4]; // one byte per SDO
                _lastOutputStateUpdateTime = DateTime.Now;
            }
            InitializedWaitForOutputsRefresh.Set();
        }

        private void CreateOutgoingCANMsgs()
        {
            // TPDO1 gets the input state (RTR bit identifies this as a request to receive)
            TPDO1_RTR_GET_INPUTS = new USBcanServer.tCanMsgStruct[1];
            TPDO1_RTR_GET_INPUTS[0] = USBcanServer.tCanMsgStruct.CreateInstance(0x180 + NodeId, USBcanServer.USBCAN_MSG_FF_RTR);
            TPDO1_RTR_GET_INPUTS[0].m_bDLC = 0;
            for (int i = 0; i < 8; ++i)
                TPDO1_RTR_GET_INPUTS[0].m_bData[i] = 0x00; // we will overwrite this on receive

            // SDO read current state of digital outputs at OD 0x6200, index 1
            RSDO_GET_OUTPUTS = new USBcanServer.tCanMsgStruct[1];
            RSDO_GET_OUTPUTS[0] = USBcanServer.tCanMsgStruct.CreateInstance(0x600 + NodeId, USBcanServer.USBCAN_MSG_FF_STD);
            RSDO_GET_OUTPUTS[0].m_bDLC = 8;
            RSDO_GET_OUTPUTS[0].m_bData[0] = 0x40; // client command specifier =1 (initiate download)
            RSDO_GET_OUTPUTS[0].m_bData[1] = 0x00; // index low
            RSDO_GET_OUTPUTS[0].m_bData[2] = 0x62; // index high
            RSDO_GET_OUTPUTS[0].m_bData[3] = 0x01; // subindex -- we will overwrite this with output_byte index - 1
            RSDO_GET_OUTPUTS[0].m_bData[4] = 0x00; // data0 -- output data
            RSDO_GET_OUTPUTS[0].m_bData[5] = 0x00; // data1 -- unused
            RSDO_GET_OUTPUTS[0].m_bData[6] = 0x00; // data2 -- unused
            RSDO_GET_OUTPUTS[0].m_bData[7] = 0x00; // data3 -- unused

            // RPDO1 sets the output state 
            RPDO1_SET_OUTPUTS = new USBcanServer.tCanMsgStruct[1];
            RPDO1_SET_OUTPUTS[0] = USBcanServer.tCanMsgStruct.CreateInstance(0x200 + NodeId, USBcanServer.USBCAN_MSG_FF_STD);
            RPDO1_SET_OUTPUTS[0].m_bDLC = _output_bytes;
            for (int i = 0; i < 8; ++i)
                RPDO1_SET_OUTPUTS[0].m_bData[i] = 0x00; // we will overwrite this on send

            // NMT command message for start_remote_node command
            NMT_STARTUP = new USBcanServer.tCanMsgStruct[1];
            NMT_STARTUP[0] = USBcanServer.tCanMsgStruct.CreateInstance(0x000, USBcanServer.USBCAN_MSG_FF_STD);
            NMT_STARTUP[0].m_bDLC = 2;
            NMT_STARTUP[0].m_bData[0] = 0x01;
            NMT_STARTUP[0].m_bData[1] = NodeId; // ZERO for broadcast, or specific node address        
        }
    } // IO
} // namespace Systec_IO
