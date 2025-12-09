package BioNex;

public class JemsImpl
{
    static HiveServerCallbackInterface _callbacks;

    public static void Init( HiveServerCallbackInterface callbacks)
    {
        _callbacks = callbacks;
    }

    //[XmlRpcMethod("BioNex.GemsRpc.IGemsData.DestinationPlateComplete", Description = "DestinationPlateComplete method is called by Synapsis after a destination plate has been filled and place in a cart.  Parameters are hive_name, destination_barcode, an array of transfer information which maps destination wells to source wells")]
    public String DestinationPlateComplete( String hive_name, String destination_barcode, TransferMap[] mapping)
    {
        if( _callbacks != null)
            _callbacks.ReinventoryComplete(hive_name, destination_barcode, mapping);
        else
            System.out.println("null callback in BioNex.JemsImpl.DestinationPlateComplete");
        return "";
    }

    //[XmlRpcMethod("BioNex.GemsRpc.IGemsData.ReinventoryComplete", Description="ReinventoryComplete method is called by Synapsis after an inventory is completed.  Parameters are hive_name, cart_name, and an array of barcodes")]
    public String ReinventoryComplete( String hive_name, String cart_name, Object[] barcodes)
    {
        if( _callbacks != null)
            _callbacks.ReinventoryComplete(hive_name, cart_name, barcodes);
        else
            System.out.println("null callback in BioNex.JemsImpl.ReinventoryComplete");
        return "";
    }

    //[XmlRpcMethod("BioNex.GemsRpc.IGemsData.Ping", Description="Ping method is used to verify connection without performing any action")]
    public String Ping()
    {
        if( _callbacks != null)
            _callbacks.Ping();
        else
            System.out.println("null callback in BioNex.JemsImpl.Ping");
        return "";
    }
}
