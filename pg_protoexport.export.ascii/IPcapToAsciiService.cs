namespace pg_protoexport;

public interface IPcapToAsciiService
{
    void PcapToAscii(IEnumerable<PostgresPacket> packets, string outputFile);
}
