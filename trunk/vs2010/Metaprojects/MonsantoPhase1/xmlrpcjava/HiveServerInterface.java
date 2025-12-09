package BioNex;

public interface HiveServerInterface
{
    void Init(int port, HiveServerCallbackInterface callbacks);
    void Start() throws HiveServerException;
    void Stop();
}