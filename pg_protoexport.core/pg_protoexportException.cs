namespace pg_protoexport;

public class pg_protoexportException : Exception
{
    public pg_protoexportException() { }
    public pg_protoexportException(string message) : base(message) { }
    public pg_protoexportException(string message, Exception inner) : base(message, inner) { }
}
