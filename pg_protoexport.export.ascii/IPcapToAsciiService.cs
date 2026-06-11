namespace pg_protoexport;

public interface IPcapToAsciiService
{
    void PcapToAscii(IEnumerable<PostgresPacket> packets, string outputFile);

    void PcapToSequenceDiagram(IEnumerable<PostgresPacket> packets, string outputFile);

    void PcapToSequenceDiagram(IEnumerable<PostgresPacket> packets, TextWriter writer);
}
