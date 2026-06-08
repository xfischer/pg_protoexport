using System.Net;

namespace pg_protoexport;

public class PostgresPacket
{
    public List<PostgresMessageBase> Messages { get; set; } = [];
    public bool IsFrontEnd { get; internal init; }
    public IPAddress SourceAddress { get; internal init; } = IPAddress.Loopback;
    public IPAddress DestinationAddress { get; internal init; } = IPAddress.Loopback;
    public ushort SourcePort { get; internal init; }
    public ushort DestinationPort { get; internal init; }
    public int PacketIndex { get; internal set; }
    public DateTime Timestamp { get; internal set; }
    public byte[] PayloadData { get; internal set; } = [];
}
