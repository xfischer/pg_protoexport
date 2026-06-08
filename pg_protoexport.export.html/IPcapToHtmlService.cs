namespace pg_protoexport;

public interface IPcapToHtmlService
{
    void PcapToHtml(IEnumerable<PostgresPacket> packets, string outputFile);
}
