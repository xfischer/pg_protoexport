namespace pg_protoexport;

public interface IPcapToMermaidService
{
    void PcapToSequenceDiagram(IEnumerable<PostgresPacket> packets, string outputFile);
    void PcapToSequenceDiagram(IEnumerable<PostgresPacket> packets, TextWriter writer);
    void PcapToPacketDiagram(IEnumerable<PostgresPacket> packets, string outputFile);
}
