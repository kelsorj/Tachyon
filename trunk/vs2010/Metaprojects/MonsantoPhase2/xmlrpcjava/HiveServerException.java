package BioNex;

public class HiveServerException extends Exception
{
    public HiveServerException(){ super();}
    public HiveServerException(String message){ super(message);}
    public HiveServerException(String message, Throwable cause){ super(message, cause);}
    public HiveServerException(Throwable cause){ super(cause);}
}