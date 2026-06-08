namespace pg_protoexport;

public interface IPcapToPqTraceService
{
    void PcapToPqTrace(IEnumerable<PostgresPacket> packets, string outputFile);
}
