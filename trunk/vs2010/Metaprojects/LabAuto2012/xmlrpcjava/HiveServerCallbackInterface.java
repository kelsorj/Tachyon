package BioNex;

public interface HiveServerCallbackInterface
{
    void DestinationPlateComplete(String hive_name, String destination_barcode, TransferMap[] mapping);
    void ReinventoryComplete(String hive_name, String cart_name, Object[] barcodes);
    void Ping();
}
