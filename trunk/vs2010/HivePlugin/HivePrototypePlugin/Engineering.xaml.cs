using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using UcanDotNET;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for Engineering.xaml
    /// </summary>
    public partial class Engineering : UserControl
    {
        private HivePlugin _controller;
        public HivePlugin Controller
        {
            get { return _controller; }
            set
            {
                _controller = value;
                DataContext = _controller;
            }
        }

        public Engineering()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TextEngTCT_num_msgs.Text = _controller.EngTCT_msg_num_total.ToString();
            TextEngTCT_axis_id.Text = _controller.EngTCT_axis_id.ToString();
            LabelEngTD_num_devices.Content = _controller.EngTD_num_devices.ToString();
        }

        private void ButtonTestCommsThroughput(object sender, RoutedEventArgs e)
        {
            DateTime start = DateTime.Now;

            _controller.EngTCT_axis_id = byte.Parse(TextEngTCT_axis_id.Text);
            _controller.EngTCT_msg_num_total = int.Parse(TextEngTCT_num_msgs.Text);

            for (_controller.EngTCT_msg_num = 1; _controller.EngTCT_msg_num <= _controller.EngTCT_msg_num_total; ++_controller.EngTCT_msg_num)
            {
                long pos = _controller.Hardware.GetAxisPositionCounts( _controller.EngTCT_axis_id);
            }

            TimeSpan elapsed_time = DateTime.Now - start;
            _controller.EngTCT_msg_num--;

            LabelEngTCT_elapsed_time_ms.Content = String.Format("{0:0} ms", elapsed_time.TotalMilliseconds);
            LabelEngTCT_msg_num.Content = _controller.EngTCT_msg_num.ToString();
            LabelEngTCT_msg_per_sec.Content = String.Format("{0:0}", _controller.EngTCT_msg_num / elapsed_time.TotalSeconds);

            HivePlugin._log.DebugFormat( "Called GetPositionCounts() {0} times on axis {1} in {2} milliseconds --> {3:0.0} msgs/sec",
                _controller.EngTCT_msg_num, _controller.EngTCT_axis_id, elapsed_time.TotalMilliseconds, _controller.EngTCT_msg_num / elapsed_time.TotalSeconds);
        }

        private void ButtonTestDiscovery(object sender, RoutedEventArgs e)
        {
            /*
            int num_devices_discovered = 0;
            byte ret;
            int dwCount_send = 0;
            byte channel = USBcanServer.USBCAN_CHANNEL_CH0;

            Dictionary<int, string> ts_cntl = new Dictionary<int,string>();

            const int can_msgs_max = 32;
            USBcanServer.tCanMsgStruct [] can_msgs;
            can_msgs = new USBcanServer.tCanMsgStruct[can_msgs_max];
            for (int n = 0; n < can_msgs_max; ++n)
            {
                can_msgs[n] = USBcanServer.tCanMsgStruct.CreateInstance(0, 0);
            }
            USBcanServer.tCanMsgStruct[] can_msgs_sending;
            can_msgs_sending = new USBcanServer.tCanMsgStruct[1];
            can_msgs_sending[0] = USBcanServer.tCanMsgStruct.CreateInstance(0, 0);

            USBcanServer USBCan = new USBcanServer();

            int dll_ver = USBCan.GetUserDllVersion();
            _controller._log.DebugFormat( "User Dll: Ver={0}, Rev={1}, Release={2}", dll_ver & 0xff, (dll_ver & 0xff00) >> 8, (dll_ver & 0xff0000) >> 16);
        
            byte device = byte.Parse(_controller._comm_selection_dialog.GetPort());
            ret = USBCan.InitHardware(device);
            _controller._log.DebugFormat( "InitHardware (device = {0}) returned {1}", device, ret);
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                _controller._log.DebugFormat( "Error: InitHardware returned {0}", ret);
            }

            ret = USBCan.InitCan(channel, USBcanServer.USBCAN_BAUD_500kBit, USBcanServer.USBCAN_BAUDEX_USE_BTR01,
                    USBcanServer.USBCAN_AMR_ALL, USBcanServer.USBCAN_ACR_ALL, (byte)USBcanServer.tUcanMode.kUcanModeNormal,
                    USBcanServer.USBCAN_OCR_DEFAULT);
            _controller._log.DebugFormat( "InitCan (channel={0}) returned {1}", channel, ret);
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                _controller._log.DebugFormat( "Error: InitCan returned {0}", ret);
            }

            int fw_ver = USBCan.GetFwVersion();
            _controller._log.DebugFormat( "Firmware: Ver={0}, Rev={1}, Release={2}", fw_ver & 0xff, (fw_ver & 0xff00) >> 8, (fw_ver & 0xff0000) >> 16);

            USBcanServer.tUcanHardwareInfoEx HwInfo = new USBcanServer.tUcanHardwareInfoEx();
            USBcanServer.tUcanChannelInfo CanInfoCh0 = new USBcanServer.tUcanChannelInfo();
            USBcanServer.tUcanChannelInfo CanInfoCh1 = new USBcanServer.tUcanChannelInfo();

            ret = USBCan.GetHardwareInfo(ref HwInfo, ref CanInfoCh0, ref CanInfoCh1);
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                _controller._log.DebugFormat( "Error: GetHardwareInfo returned {0}", ret);
            }
            else
            {
                _controller._log.DebugFormat( "S/N: {0}, Device# {1}, F/W: {2}, ProdCode: {3}, Size={4}", HwInfo.m_dwSerialNr, HwInfo.m_bDeviceNr, HwInfo.m_dwFwVersionEx, HwInfo.m_dwProductCode, HwInfo.m_dwSize);
                _controller._log.DebugFormat( "Ch0: Init={0}, CANstatus={1}", CanInfoCh0.m_fCanIsInit, CanInfoCh0.m_wCanStatus);
                _controller._log.DebugFormat( "Ch1: Init={0}, CANstatus={1}", CanInfoCh1.m_fCanIsInit, CanInfoCh1.m_wCanStatus);
            }

            // var_i2 = 0xffe5;       // from 254 to broadcast ==> 04200167 E5 FF 
            // var_i1=(var_i2+),spi;  // from 254 to broadcast ==> 12200108 67 03 66 03
            // ?var_i1;               // from 254 to broadcast ==> 16200004 E0 0F 66 03
            // replies to ?var_i1 should be something like  169FC004 ID 00 66 03 xx yy (from axis ID)
            // ??var_i1;              // from 254 to broadcast ==> 16600004 E0 0F 66 03
            // replies to ??var_i1 should be something like  1A9FC0nn 66 03 xx yy (from axis ID nn)

            // var_i2 = 0xffe5; (from 255 to broadcast)
            can_msgs_sending[0].m_dwID = 0x04200167;
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 2;
            can_msgs_sending[0].m_bData[0] = 0xE5;
            can_msgs_sending[0].m_bData[1] = 0xFF;
            dwCount_send = 1;
            TestSendCanMsgs(ref dwCount_send, ref can_msgs_sending, USBCan);

            ////////////////////////
            // First set of chars //
            ////////////////////////


            // var_i1=(var_i2+),spi;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x12200108;
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0x67;
            can_msgs_sending[0].m_bData[1] = 0x03;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            dwCount_send = 1;
            TestSendCanMsgs(ref dwCount_send, ref can_msgs_sending, USBCan);

            // ??var_i1;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x16600004; // GiveMeData2
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0xE0;
            can_msgs_sending[0].m_bData[1] = 0x0F;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            dwCount_send = 1;
            TestSendCanMsgs(ref dwCount_send, ref can_msgs_sending, USBCan);

            num_devices_discovered = TestCANbuildAxisDict(ref channel, ref ts_cntl, can_msgs_max, ref can_msgs, USBCan);

            /////////////////////////
            // Second set of chars //
            /////////////////////////

            // var_i1=(var_i2+),spi;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x12200108;
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0x67;
            can_msgs_sending[0].m_bData[1] = 0x03;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            dwCount_send = 1;
            TestSendCanMsgs(ref dwCount_send, ref can_msgs_sending, USBCan);

            // ??var_i1;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x16600004; // GiveMeData2
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0xE0;
            can_msgs_sending[0].m_bData[1] = 0x0F;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            dwCount_send = 1;
            TestSendCanMsgs(ref dwCount_send, ref can_msgs_sending, USBCan);

            int num_devices_queried = TestCANaddDict(num_devices_discovered, ref channel, ref ts_cntl, can_msgs_max, ref can_msgs, USBCan);

            ///////////////////////// 
            // Third set of chars  //
            /////////////////////////

            // var_i1=(var_i2+),spi;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x12200108;
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0x67;
            can_msgs_sending[0].m_bData[1] = 0x03;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            dwCount_send = 1;
            TestSendCanMsgs(ref dwCount_send, ref can_msgs_sending, USBCan);

            // ??var_i1;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x16600004; // GiveMeData2
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0xE0;
            can_msgs_sending[0].m_bData[1] = 0x0F;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            dwCount_send = 1;
            TestSendCanMsgs(ref dwCount_send, ref can_msgs_sending, USBCan);

            num_devices_queried = TestCANaddDict(num_devices_discovered, ref channel, ref ts_cntl, can_msgs_max, ref can_msgs, USBCan);

            ret = USBCan.Shutdown();
            //_controller._mainDebugFile.WriteLine(@"TestDiscovery", String.Format("Shutdown returned {0}", ret));
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                _controller._log.DebugFormat( "Error: Shutdown returned {0}", ret);
            }

            _controller._log.DebugFormat( "Found {0} Technosoft controllers on the CANbus", num_devices_discovered);
            LabelEngTD_num_devices.Content = String.Format("{0:0}", num_devices_discovered);

            foreach (KeyValuePair<int, string> kvp in ts_cntl)
            {
                _controller._log.DebugFormat( "Axis ID={0} has S/N: {1}", kvp.Key, kvp.Value);
            }

            _controller.EngTD_num_devices = num_devices_discovered;
             */

            MessageBox.Show( "This function has been disabled until it is compliant with new CanDongleWrapper assembly");
        }

        private int TestCANaddDict(int num_devices_discovered, ref byte channel, ref Dictionary<int, string> ts_cntl, int can_msgs_max, ref USBcanServer.tCanMsgStruct[] can_msgs, USBcanServer USBCan)
        {
            byte ret;
            int num_devices_queried = 0;
            DateTime time_start = DateTime.Now;
            const int timeout_ms = 100; // milliseconds

            while ((DateTime.Now - time_start).TotalMilliseconds < timeout_ms && num_devices_queried < num_devices_discovered) // max number of channel ID's on a bus is 255
            {
                int dwCount = can_msgs_max;
                ret = USBCan.ReadCanMsg(ref channel, ref can_msgs, ref dwCount);
                HivePlugin._log.DebugFormat( "ReadCanMsg returned {0}, dwCount={1}", ret, dwCount);
                if (ret == USBcanServer.USBCAN_WARN_NODATA)
                {
                    HivePlugin._log.Debug( "Sleeping for 1ms");
                    Thread.Sleep(1);
                    continue;
                }
                if (ret != USBcanServer.USBCAN_SUCCESSFUL || dwCount < 1)
                {
                    HivePlugin._log.DebugFormat( "Error: ReadCanMsg returned {0}, dwCount={1} (expected dwCount=={2})", ret, dwCount, 1);
                    Thread.Sleep(1);
                    continue;
                }
                for (int n = 0; n < dwCount; ++n)
                {
                    HivePlugin._log.DebugFormat( "CanMsg: ID={0:x08} DLC={1} {2:x02} {3:x02} {4:x02} {5:x02} {6:x02} {7:x02} {8:x02} {9:x02}",
                    can_msgs[n].m_dwID, can_msgs[n].m_bDLC, can_msgs[n].m_bData.ElementAt(0), can_msgs[n].m_bData.ElementAt(1), can_msgs[n].m_bData.ElementAt(2),
                    can_msgs[n].m_bData.ElementAt(3), can_msgs[n].m_bData.ElementAt(4), can_msgs[n].m_bData.ElementAt(5), can_msgs[n].m_bData.ElementAt(6),
                    can_msgs[n].m_bData.ElementAt(7));
                    //axis_id = ((can_msgs[n].m_bData.ElementAt(1) & 0x0f) << 4) | ((can_msgs[n].m_bData.ElementAt(0) & 0xf0) >> 4);
                    if ((can_msgs[n].m_dwID & 0xFFFFFF00) == 0x1A9FC000) // TakeData2 message from controller to axis 254 (computer)
                    {
                        int axis_id = can_msgs[n].m_dwID & 0xFF;
                        HivePlugin._log.DebugFormat( "Found Axis ID={0}", axis_id);
                        if (!ts_cntl.ContainsKey(axis_id))
                        {
                            HivePlugin._log.DebugFormat( "Error: Not expecting to hear from AxisID={0}. Ignoring...", axis_id);
                            continue;
                        }
                        num_devices_queried++;
                        ts_cntl[axis_id] += String.Format("{0}{1}",
                            char.ConvertFromUtf32(can_msgs[n].m_bData.ElementAt(2)),
                            char.ConvertFromUtf32(can_msgs[n].m_bData.ElementAt(3)));
                    }
                    else
                    {
                        HivePlugin._log.DebugFormat( "Disregarding unknown CAN message ID={0:x08}", can_msgs[n].m_dwID);
                    }
                }
            }
            return num_devices_queried;
        }

        private int TestCANbuildAxisDict(ref byte channel, ref Dictionary<int, string> ts_cntl, int can_msgs_max, 
                                         ref USBcanServer.tCanMsgStruct[] can_msgs, USBcanServer USBCan)
        {
            int ret;
            int num_devices_discovered = 0;
            const int timeout_ms = 100; // milliseconds

            DateTime time_start = DateTime.Now;

            while ((DateTime.Now - time_start).TotalMilliseconds < timeout_ms && num_devices_discovered < 255) // max number of channel ID's on a bus is 255
            {
                int dwCount = can_msgs_max;
                ret = USBCan.ReadCanMsg(ref channel, ref can_msgs, ref dwCount);
                HivePlugin._log.DebugFormat( "ReadCanMsg returned {0}, dwCount={1}", ret, dwCount);
                if (ret == USBcanServer.USBCAN_WARN_NODATA)
                {
                    Thread.Sleep(1); // yield the processor if someone else is ready to run;
                    continue;
                }
                if (ret != USBcanServer.USBCAN_SUCCESSFUL || dwCount < 1)
                {
                    Thread.Sleep(1); // yield the processor if someone else is ready to run;
                    HivePlugin._log.DebugFormat( "Error: ReadCanMsg returned {0}, dwCount={1} (expected dwCount=={2})", ret, dwCount, 1);
                    continue;
                }
                for (int n = 0; n < dwCount; ++n)
                {
                    HivePlugin._log.DebugFormat( "CanMsg: ID={0:x08} DLC={1} {2:x02} {3:x02} {4:x02} {5:x02} {6:x02} {7:x02} {8:x02} {9:x02}",
                        can_msgs[n].m_dwID, can_msgs[n].m_bDLC, can_msgs[n].m_bData.ElementAt(0), can_msgs[n].m_bData.ElementAt(1), can_msgs[n].m_bData.ElementAt(2),
                        can_msgs[n].m_bData.ElementAt(3), can_msgs[n].m_bData.ElementAt(4), can_msgs[n].m_bData.ElementAt(5), can_msgs[n].m_bData.ElementAt(6),
                        can_msgs[n].m_bData.ElementAt(7));
                    //axis_id = ((can_msgs[0].m_bData.ElementAt(1) & 0x0f) << 4) | ((can_msgs[0].m_bData.ElementAt(0) & 0xf0) >> 4);
                    if ((can_msgs[n].m_dwID & 0xFFFFFF00) == 0x1A9FC000) // TakeData2 message from controller to axis 254 (computer)
                    {
                        num_devices_discovered++;
                        int axis_id = can_msgs[n].m_dwID & 0xFF;
                        HivePlugin._log.DebugFormat( "Found Axis ID={0}", axis_id);
                        ts_cntl.Add(axis_id, String.Format("{0}{1}",
                            char.ConvertFromUtf32(can_msgs[n].m_bData.ElementAt(2)),
                            char.ConvertFromUtf32(can_msgs[n].m_bData.ElementAt(3))));
                    }
                    else
                    {
                        HivePlugin._log.DebugFormat( "Disregarding unknown CAN message ID={0:x08}", can_msgs[n].m_dwID);
                    }
                }
            }
            return num_devices_discovered;
        }

        private byte TestSendCanMsgs(ref int dwCount_send, ref USBcanServer.tCanMsgStruct[] can_msgs_sending, USBcanServer USBCan)
        {
            byte ret = USBCan.WriteCanMsg(USBcanServer.USBCAN_CHANNEL_CH0, ref can_msgs_sending, ref dwCount_send);
            HivePlugin._log.DebugFormat( "WriteCanMsg returned {0}, dwCount_send={1}", ret, dwCount_send);
            if (ret != USBcanServer.USBCAN_SUCCESSFUL || dwCount_send < 1)
            {
                HivePlugin._log.DebugFormat( "Error: WriteCanMsg returned {0}, dwCount_send={1}", ret, dwCount_send);
            }
            return ret;
        }
    }
}
