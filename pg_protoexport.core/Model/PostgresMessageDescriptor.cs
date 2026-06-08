namespace pg_protoexport;

/// <summary>
/// Message header descriptor, with code, name and direction
/// </summary>
/// <param name="Code">Messsage code</param>
/// <param name="Name">Message name according to Postgres doc</param>
/// <param name="IsFrontEnd">Direction of the message. When true, the message is emitted by the client, false when is it emitted by the server</param>
public record struct PostgresMessageDescriptor(char Code, string Name, bool IsFrontEnd);
