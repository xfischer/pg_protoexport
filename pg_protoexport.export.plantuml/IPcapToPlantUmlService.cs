namespace pg_protoexport;

public interface IPcapToPlantUmlService
{
    void PcapToSequenceDiagram(IEnumerable<PostgresPacket> packets, string outputFile);
    void PcapToPacketDiagram(IEnumerable<PostgresPacket> packets, string outputFile);
}
