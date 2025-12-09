using System;
using System.Collections.Generic;
using System.Text;

namespace BioNex.Utilities.DetectSysTecDriver
{
    class Program
    {
        /// <summary>
        /// Returns 0 if SysTec driver found, otherwise returns non-zero
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {
            bool found_systec = false;
            System.Diagnostics.ProcessStartInfo start_info = new System.Diagnostics.ProcessStartInfo();
            System.Diagnostics.Process p = new System.Diagnostics.Process();

            start_info.CreateNoWindow = true;
            start_info.RedirectStandardInput = true;
            start_info.RedirectStandardOutput = true;
            start_info.UseShellExecute = false;
            start_info.Arguments = "";
            start_info.FileName = "driverquery";

            p.StartInfo = start_info;
            p.OutputDataReceived += (sender, arguments) => {
                if( arguments.Data != null && arguments.Data.Contains( "UCANNET"))
                    found_systec = true;
            };
            
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
            
            // you can check the error code returned from console by using "echo %errorlevel%"
            return found_systec ? 0 : 1;
        }
    }
}
