package BioNex;

public class HiveClientException extends Exception
{
    public HiveClientException(){ super();}
    public HiveClientException(String message){ super(message);}
    public HiveClientException(String message, Throwable cause){ super(message, cause);}
    public HiveClientException(Throwable cause){ super(cause);}
}