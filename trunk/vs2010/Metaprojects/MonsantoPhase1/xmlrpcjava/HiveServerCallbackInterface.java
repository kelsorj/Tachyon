package BioNex;

public interface HiveServerCallbackInterface
{
    void ReinventoryComplete(String hive_name, String cart_name, Object[] barcodes);
    public void Ping();
}