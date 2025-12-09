using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using System.ComponentModel.Composition;

namespace BioNex.PlateStak
{
    [Export(typeof(DeviceInterface))]
    public class Stacker : SystemStartupCheckInterface, StackerInterface, DeviceInterface
    {
        #region StackerInterface Members

        public void Upstack(BioNex.Shared.PlateDefs.Plate plate)
        {
            
        }

        public void Downstack(BioNex.Shared.PlateDefs.Plate plate)
        {
            
        }

        #endregion

        public override bool IsReady(out string reason_not_ready)
        {
            reason_not_ready = "";
            return true;
        }

        public override System.Windows.Controls.UserControl GetSystemPanel()
        {
            return null;
        }

        #region DeviceInterface Members

        public void Connect()
        {
            
        }

        public bool Connected
        {
            get { return false; }
        }

        public void Home()
        {
            
        }

        public bool IsHomed
        {
            get { return false; }
        }

        public void Close()
        {
            
        }

        public bool ExecuteCommand(string command, Dictionary<string, object> parameters)
        {
            return true;
        }

        public IEnumerable<string> GetCommands()
        {
            return new List<string>();
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

        public void Reset()
        {
            
        }

        #endregion

        #region IPluginIdentity Members

        public string Name
        {
            get { return "PlateStak Instance Name"; }
        }

        public string ProductName
        {
            get { return "PlateStak"; }
        }

        public string Manufacturer
        {
            get { return "Perkin Elmer"; }
        }

        public string Description
        {
            get { return "Perkin Elmer PlateStak"; }
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            
        }

        #endregion

        #region IHasDiagnosticPanel Members

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            return null;
        }

        public void ShowDiagnostics()
        {
            
        }

        #endregion
    }
}
