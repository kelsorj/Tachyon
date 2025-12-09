namespace BioNex.Shared.DeviceInterfaces
{
    public interface ProtocolHooksInterface
    {
        void ProtocolStarting();
        void ProtocolStarted();
        void ProtocolComplete();
        void ProtocolAborted();
    }
}
