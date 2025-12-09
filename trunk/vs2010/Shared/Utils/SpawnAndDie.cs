using System.Diagnostics;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class SpawnAndDie
    {
        public SpawnAndDie(string path, bool terminate_parent = true)
        {
            var proc = Process.Start(path);
            if (terminate_parent)
                Process.GetCurrentProcess().Kill();
        }
    }
#endif
}
