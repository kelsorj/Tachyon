using System.Diagnostics;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class ExceptionTraceListener : TraceListener
    {
        public override void WriteLine(string message)
        {            
        }
        public override void Write(string message)
        {
        }

        public override void Fail(string message)
        {
            // Debug Assertions sometimes don't throw up the assertion message box in WPF.  No one knows why.
            // This class forces the debugger to break whether or not it popped up a message box.

            // Unfortunately this hangs the app if you're running outside of the debugger!  Go MS!
            Debugger.Break();
        }

        public override void Fail(string message, string detailMessage)
        {
            Debugger.Break();
        }
    }
#endif
}
