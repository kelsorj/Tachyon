namespace BioNex.Shared.DeviceInterfaces
{
    public interface SystemStatusInterface
    {
        void Running( bool is_running);
        void Error( bool has_error);
        void Paused( bool is_paused);
        void ProtocolComplete( bool is_complete);
    }
}
