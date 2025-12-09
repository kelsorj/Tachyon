using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using System.ComponentModel.Composition;

namespace MockDevice
{
    [Export(typeof(DeviceInterface))]
    public class Mocky : DeviceInterface
    {
        #region DeviceInterface Members

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            return null;
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            
        }

        public void ShowDiagnostics()
        {
            
        }

        public void Initialize()
        {
            
        }

        public void Close()
        {
            
        }

        public IEnumerable<string> GetCommands()
        {
            return new List<string>();
        }


        public void Connect()
        {
            
        }

        public bool Connected
        {
            get { return true; }
        }

        public void Home()
        {
            
        }

        public void Abort()
        {
            
        }

        public void Pause()
        {

        }

        public void Resume()
        {
            
        }


        bool DeviceInterface.IsHomed
        {
            get { return true; }
        }

        public void Reset()
        {
            
        }

        public string Name
        {
            get { return "Mocky Alpha"; }
        }

        public string ProductName
        {
            get { return "Mocky"; }
        }

        public string Manufacturer
        {
            get { return "Mock Industries"; }
        }

        public string Description
        {
            get { return "You mock me, I mock you!"; }
        }

        #endregion

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            return true;
        }
    }
}
