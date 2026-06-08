
namespace pg_protoexport;

public interface IPcapToLatexService
{
    GenerationState PcapToLaTeX(IEnumerable<PostgresPacket> pgSqlPackets, string latexOutputFile, bool standalone = true);

    GenerationState PcapToLaTeX(IEnumerable<PostgresPacket> pgSqlPackets, string latexOutputFile, bool standalone, LatexRenderOptions render);

    GenerationState PcapToLaTeX(IEnumerable<PostgresPacket> pgSqlPackets, Stream outputStream, bool standalone, LatexRenderOptions render);

    GenerationState PcapToLaTeX_MultipleFiles(IEnumerable<PostgresPacket> pgSqlPackets, string latexOutputDirectory);

    GenerationState PcapToLaTeX_MultipleFiles(IEnumerable<PostgresPacket> pgSqlPackets, string latexOutputDirectory, LatexRenderOptions render);
}