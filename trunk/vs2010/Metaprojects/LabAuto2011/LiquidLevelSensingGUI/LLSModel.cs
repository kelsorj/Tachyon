using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using log4net;
using System.Threading;
using BioNex.BumblebeePlugin;
using BioNex.Shared.Utils;
using BioNex.LiquidLevelDevice;

namespace LiquidLevelSensingGUI
{
    class LLSModel
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(LLSModel));
        public event EventHandler ProtocolComplete;

        public bool Simulate { get; private set; }
        private uint _run_counter = 0;

        LLSensorPlugin _sensor;

        public bool IsCalibrated { get { throw new NotImplementedException(); } }// return _sensors[0].IsCalibrated; } }

        private BioNex.BumblebeePlugin.Bumblebee _bee;

        public LLSModel(ExternalDataRequesterInterface data_request_interface, bool simulate=false) 
        {
            Simulate = simulate;
            var devices = data_request_interface.GetDeviceInterfaces();
            _bee = (from x in devices where (x as Bumblebee) != null select x as Bumblebee).FirstOrDefault();
            if( _bee != null) _bee.HomeOnProtocolComplete = false;
            else
                System.Windows.MessageBox.Show("You forgot to add a BumbleBee to the device database");

            _sensor = (from x in devices where (x as LLSensorPlugin) != null select x as LLSensorPlugin).FirstOrDefault();
            if( _sensor != null) _sensor.BuddyDevice = _bee;
            else
                System.Windows.MessageBox.Show("You forgot to add a LLSensorPlugin to the device database");
        }

        public void Close()
        {
            if( _sensor != null)
                _sensor.Close();
        }

        public void Start(LLSCaptureStateMachine.CaptureProgressCallback callback) 
        {
            if (_bee == null && !Simulate)
            {
                _log.Error("No BumbleBee found in device database -- add one and try starting again");
                if (ProtocolComplete != null) ProtocolComplete(this, null);
                return;
            }
            new Thread(() => {
                //_sensor.Calibrate();
                _sensor.Capture(callback, ++_run_counter);
                if (ProtocolComplete != null) ProtocolComplete(this, null);              

            }).Start();
        }

        public void CalibrateSensors()
        {
            if (_bee == null && !Simulate)
            {
                _log.Error("No BumbleBee found in device database -- add one and try starting again");
                if (ProtocolComplete != null) ProtocolComplete(this, null);
                return;
            }

            new Thread(() => {
                _sensor.Calibrate();
            }).Start();
        }
        }
        }
