package BioNex;

public interface HiveClientInterface
{
    void Init(String host, int port)                throws HiveClientException;
    void AddWorkSet(String workset_xml)             throws HiveClientException;
    void SetPlateFate(String barcode, String fate)  throws HiveClientException;
    void EndBatch(String fate)                      throws HiveClientException;
    void Ping()                                     throws HiveClientException;
}

