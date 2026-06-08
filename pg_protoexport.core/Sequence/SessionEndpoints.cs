using System.Net;

namespace pg_protoexport;

/// <summary>
/// Client/server endpoints for a PostgreSQL session, resolved from the first observed packet.
/// </summary>
public sealed record SessionEndpoints(
    IPAddress Client, ushort ClientPort,
    IPAddress Server, ushort ServerPort)
{
    /// <summary>
    /// Resolves the client/server endpoints from a packet. A front-end packet's source
    /// is the client; a back-end packet's source is the server.
    /// </summary>
    public static SessionEndpoints FromFirstPacket(PostgresPacket packet) =>
        packet.IsFrontEnd
            ? new(packet.SourceAddress, packet.SourcePort, packet.DestinationAddress, packet.DestinationPort)
            : new(packet.DestinationAddress, packet.DestinationPort, packet.SourceAddress, packet.SourcePort);
}
