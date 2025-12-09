using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace LabwareCloudService
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern int AllocConsole();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].ToLower() == "/console")
            {
                AllocConsole();
                Console.WriteLine("LabwareCloudApp Console started. Type 'exit' to stop the application: ");
 
                var app = new LabwareCloudApp();
                app.Start(false);

                // Wait for the user to exit the application
                string input = "";
                while (input.ToLower() != "exit") 
                    input = Console.ReadLine();
 
                // Stop the application.
                app.Stop();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
			    { 
				    new LabwareCloudService() 
			    };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
