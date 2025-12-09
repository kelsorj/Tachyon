using System;
using System.Diagnostics;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class ElevatedProcessLauncher
    {
        // filename must include ".exe" for elevation to work
        public ElevatedProcessLauncher(string filename, string arguments)
        {
            Process process = null;
            ProcessStartInfo processStartInfo = new ProcessStartInfo(filename);

            if (Environment.OSVersion.Version.Major >= 6)  // Windows Vista or higher
                processStartInfo.Verb = "runas";

            processStartInfo.Arguments = arguments;
            processStartInfo.WindowStyle = ProcessWindowStyle.Normal;
            processStartInfo.UseShellExecute = true; // required for auto-elevation

            try
            {
                Console.Write(string.Format("running '{0} {1}' with elevated privileges... ", filename, arguments));
                process = Process.Start(processStartInfo);
                process.WaitForExit();
                Console.WriteLine("success");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (process != null)
                    process.Dispose();
            }
        }
    }
#endif
}
