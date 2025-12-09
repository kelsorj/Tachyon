using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;
using GalaSoft.MvvmLight.Command;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.BioNexGuiControls;
using System.Windows.Media.Media3D;

namespace LiquidLevelSensingGUI
{
    [Export(typeof(ICustomerGUI))]
    public partial class LiquidLevelGUI : UserControl, ICustomerGUI
    {
        public RelayCommand HomeAllDevicesCommand { get; set; }
        public RelayCommand CalibrateSensorsCommand {get; set; }

        [Import]
        private Lazy<ICustomSynapsisQuery> SynapsisQuery { get; set; }
        private LLSModel _model;

        [ImportingConstructor]
        public LiquidLevelGUI([Import] ExternalDataRequesterInterface dri)
        {
            InitializeComponent();
            DataContext = this;
            HomeAllDevicesCommand = new RelayCommand(HomeAllDevices, CanExecuteHomeAllDevicesCommand);
            CalibrateSensorsCommand = new RelayCommand(CalibrateSensors, CanExecuteCalibrateSensors);
            _model = new LLSModel(dri);

            Graph3D.RotateAroundAxis(new Vector3D(0, 0, 1), 190); // rotate around the Z
            var x_axis = new Vector3D(1,0,0) * Graph3D.Rotation.Value; // get the rotated x_axis
            Graph3D.RotateAroundAxis(x_axis, 70); // rotate around x
            Graph3D.Scale = 10.0;
        }

        private void HomeAllDevices()
        {
            SynapsisQuery.Value.HomeAllDevices();
        }

        private bool CanExecuteHomeAllDevicesCommand()
        {
            if( SynapsisQuery == null)
                return false;
            string reason;
            bool ok = SynapsisQuery.Value.ClearToHome(out reason);
            return ok;
        }

        private void CalibrateSensors()
        {
            _model.CalibrateSensors();
        }

        private bool CanExecuteCalibrateSensors()
        {
            return true;
        }

        #region ICustomerGUI
        public event EventHandler ProtocolComplete { add { _model.ProtocolComplete += value; } remove { _model.ProtocolComplete -= value; } }
        public event EventHandler AbortableTaskStarted { add { ;} remove { ;} }
        public event EventHandler AbortableTaskComplete { add { ;} remove { ;} }
        string ICustomerGUI.GUIName { get { return "Liquid Level Sensing"; } }
        bool ICustomerGUI.Busy { get { return false; } }
        string ICustomerGUI.BusyReason { get { return "not busy"; } }
        bool ICustomerGUI.CanExecuteStart(out IEnumerable<string> failure_reasons) { failure_reasons = null;  return true; }
        bool ICustomerGUI.ShowProtocolExecuteButtons() { return true; }
        bool ICustomerGUI.CanClose() { return true; }
        bool ICustomerGUI.CanPause() { return false; }
        void ICustomerGUI.Close() { _model.Close(); }
        bool ICustomerGUI.AllowDiagnostics() { return true; }
        bool ICustomerGUI.ExecuteStart()
        {
            Graph3D.Clear();
            _model.Start(GraphCallback);
            return true;
        }
        void ICustomerGUI.CompositionComplete() { }
        #endregion

        void GraphCallback(int row, int col, double x, double y, double z)
        {
            if (_model.Simulate)
            {
                z /= (4096.0/10.0);
            }
            x /= 5.0;
            y /= 5.0;
            z /= 5.0;
            Graph3D.AddPoint(x, y, z);
            Graph3D.Recenter();
        }
    }
}
