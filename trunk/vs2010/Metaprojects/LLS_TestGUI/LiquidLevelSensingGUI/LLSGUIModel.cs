using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using log4net;
using System.Threading;
using BioNex.Shared.Utils;
using BioNex.LiquidLevelDevice;
using BioNex.Shared.LibraryInterfaces;
using System.Windows;

namespace LiquidLevelSensingGUI
{
    class LLSGUIModel
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(LLSGUIModel));
        public event EventHandler ProtocolComplete;

        public bool Simulate { get; private set; }
        public bool ShowResultsSummary { get; set; }

        DeviceInterface _device;
        public ILLSensorPlugin Sensor { get { return (ILLSensorPlugin)_device; } }
        public DeviceInterface Device { get { return _device; } }

        public LLSGUIModel(ExternalDataRequesterInterface data_request_interface, bool simulate=false) 
        {
            Simulate = simulate;
            ShowResultsSummary = true;
            var devices = data_request_interface.GetDeviceInterfaces();

            _device = (from x in devices where (x as ILLSensorPlugin) != null select x as DeviceInterface).FirstOrDefault();
            if( _device == null)
                System.Windows.MessageBox.Show("You forgot to add a LLSensorPlugin to the device database");
        }

        public void Close()
        {
            if( _device != null)
                _device.Close();
        }

        string _last_labware;
        List<Averages> _last_results;
        public void Start(string labware) 
        {
            var dispatcher = Application.Current.MainWindow.Dispatcher;
            new Thread(() => {
                try
                {
                    var results = Sensor.Capture(labware);
                    FireProtocolComplete();

                    _last_labware = labware;
                    _last_results = results;
                    if (_summary != null)
                    {
                        _summary.Closed -= SummaryClosed;
                        _summary = null;
                    }
                    
                    if (ShowResultsSummary)
                        dispatcher.Invoke(new Action(() => ShowLastResults()));
                }
                catch (LevelSensorException)
                {
                }
            }).Start();
        }

        ResultsSummary _summary;
        public void ShowLastResults()
        {
            if (Application.Current == null)
                return;

            if(_last_results == null)
                return;

            if (_summary == null)
            {
                int rows, columns;
                double dummy;
                Sensor.Model.GetLabwareData(_last_labware, out columns, out rows, out dummy, out dummy, out dummy, out dummy);
                var volumes = Sensor.Model.GetVolumesFromAverages(_last_labware, _last_results);

                _summary = new ResultsSummary(Sensor.Model.RunCounter, rows, columns, _last_results, volumes);
                if (Application.Current.MainWindow == _summary)
                    return;
                _summary.Owner = Application.Current.MainWindow;
                _summary.Closed += SummaryClosed;

            }
            _summary.Show();           
        }

        private void SummaryClosed(object sender, EventArgs args)
        {
            _summary = null;
        }

        public void CalibrateSensors()
        {
            try
            {
                Sensor.Calibrate();
            }
            catch (LevelSensorException e)
            {
                _log.Error(string.Format("LiquidLevelDevice: {0}", e.Message));
            }
        }


        bool _paused = false;
        bool _needs_complete;

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
            if (_needs_complete)
                FireProtocolComplete();
        }

        public void Abort()
        {
            _paused = false;
            if (_needs_complete)
                FireProtocolComplete();
        }

        void FireProtocolComplete()
        {
            _needs_complete = true;
            if (_paused)
                return;
            _needs_complete = false;
            if (ProtocolComplete != null)
                ProtocolComplete(this, new BioNex.SynapsisPrototype.ViewModel.SynapsisViewModel.ProtocolCompleteEventArgs() { ShowMessageBox = !ShowResultsSummary });
        }
    }
}
