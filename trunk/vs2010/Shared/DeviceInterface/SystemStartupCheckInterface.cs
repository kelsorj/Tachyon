using System.Windows.Controls;

namespace BioNex.Shared.DeviceInterfaces
{
    public interface IHasSystemPanel
    {
        UserControl GetSystemPanel();
    }

    public abstract class SystemStartupCheckInterface : IHasSystemPanel
    {
        public delegate bool SafeToMoveDelegate( DeviceInterface requester);
        public abstract bool IsReady( out string reason_not_ready);
        public abstract UserControl GetSystemPanel();
    }
}
