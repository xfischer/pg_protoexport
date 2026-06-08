using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace pg_protoexport;

public class PcapToMermaidService(ILogger<PcapToMermaidService> logger) : IPcapToMermaidService, IPcapExporter
{
    public const string ModeSequenceDiagram = "sequenceDiagram";
    public const string ModePacket = "packet";

    public static IPcapToMermaidService Create(ILoggerFactory? loggerFactory = null)
    {
        var log = loggerFactory == null
            ? NullLogger<PcapToMermaidService>.Instance
            : loggerFactory.CreateLogger<PcapToMermaidService>();

        return new PcapToMermaidService(log);
    }

    public string Name => "mermaid";
    public string DefaultExtension => ".md";

    public IExportResult Export(IEnumerable<PostgresPacket> packets, string outputPath, string? mode, IExportOptions? options)
    {
        switch (mode)
        {
            case ModeSequenceDiagram:
                PcapToSequenceDiagram(packets, outputPath);
                break;
            case ModePacket:
                PcapToPacketDiagram(packets, outputPath);
                break;
            default:
                throw new ArgumentException(
                    $"Mermaid exporter requires mode '{ModeSequenceDiagram}' or '{ModePacket}', got '{mode ?? "<null>"}'.",
                    nameof(mode));
        }
        return new EmptyExportResult();
    }

    public void PcapToSequenceDiagram(IEnumerable<PostgresPacket> packets, string outputFile)
    {
        using var writer = new StreamWriter(outputFile, false);
        int diagramCount = WriteSequenceDiagram(packets, writer);
        logger.LogInformation("Wrote {DiagramCount} sequence diagram(s) to {OutputFile}", diagramCount, outputFile);
    }

    public void PcapToSequenceDiagram(IEnumerable<PostgresPacket> packets, TextWriter writer)
    {
        int diagramCount = WriteSequenceDiagram(packets, writer);
        logger.LogInformation("Wrote {DiagramCount} sequence diagram(s) to TextWriter", diagramCount);
    }

    private static int WriteSequenceDiagram(IEnumerable<PostgresPacket> packets, TextWriter writer)
    {
        SessionEndpoints? endpoints = null;
        bool diagramOpen = false;
        int diagramIndex = 0;

        foreach (var packet in packets)
        {
            endpoints ??= SessionEndpoints.FromFirstPacket(packet);

            foreach (var (frontEnd, lineLabel, closesDiagram) in PostgresPacketSequence.BuildSequenceLines(packet))
            {
                if (!diagramOpen)
                {
                    diagramIndex++;
                    writer.WriteLine();
                    writer.WriteLine($"```mermaid");
                    writer.WriteLine($"sequenceDiagram");
                    writer.WriteLine($"    participant C as Client ({endpoints.Client}:{endpoints.ClientPort})");
                    writer.WriteLine($"    participant S@{{ \"type\" : \"database\" }} as Server ({endpoints.Server}:{endpoints.ServerPort})");
                    diagramOpen = true;
                }

                string arrow = frontEnd ? "C->>S" : "S->>C";
                writer.WriteLine($"    {arrow}: {lineLabel}");

                if (closesDiagram)
                {
                    writer.WriteLine($"```");
                    diagramOpen = false;
                }
            }
        }

        if (diagramOpen)
        {
            writer.WriteLine($"```");
        }

        return diagramIndex;
    }

    public void PcapToPacketDiagram(IEnumerable<PostgresPacket> packets, string outputFile)
    {
        using var writer = new StreamWriter(outputFile, false);

        int packetIndex = 0;
        foreach (var packet in packets)
        {
            packetIndex++;
            string direction = packet.IsFrontEnd
                ? "FrontEnd --> BackEnd"
                : "FrontEnd <-- BackEnd";

            writer.WriteLine();
            writer.WriteLine($"# Packet {packetIndex} ({packet.Messages.Count} messages, {direction})");
            writer.WriteLine();
            WritePacketDiagram(writer, packet);
        }

        logger.LogInformation("Wrote {PacketCount} packet diagram(s) to {OutputFile}", packetIndex, outputFile);
    }

    // ── Packet diagram rendering ────────────────────────────────────────

    static void WritePacketDiagram(StreamWriter writer, PostgresPacket packet)
    {
        foreach (var (message, count) in PostgresPacketSequence.GroupMessages(packet))
        {
            string title = count == 1 ? message.Name : $"{message.Name} (x{count})";

            var sb = new StringBuilder();
            sb.AppendLine("```mermaid");
            sb.AppendLine("---");
            sb.AppendLine($"title: \"{title}\"");
            sb.AppendLine("config:");
            sb.AppendLine("  packet:");
            sb.AppendLine("    bitsPerRow: 32");
            sb.AppendLine("---");
            sb.AppendLine("packet");
            AppendMessageFields(sb, message);
            sb.AppendLine("```");

            writer.Write(sb);
            writer.WriteLine();
        }
    }

    static void AppendMessageFields(StringBuilder sb, PostgresMessageBase message)
    {
        Field(sb, 1, $"{message.Code}");
        Field(sb, 4, $"Length: {message.Length}");

        switch (message)
        {
            case ParseMessage parse:
                NullStr(sb, parse.Statement, "Stmt");
                NullStr(sb, parse.Query, "Query");
                Field(sb, 2, $"Params: {parse.ParameterCount}");
                foreach (var oid in parse.ParameterOids)
                    Field(sb, 4, $"OID: {oid}");
                break;

            case BindMessage bind:
                NullStr(sb, bind.PortalName, "Portal");
                NullStr(sb, bind.StatementName, "Statement");
                Field(sb, 2, $"Fmt count: {bind.ParameterFormatsCount}");
                foreach (var fmt in bind.ParameterFormats)
                    Field(sb, 2, fmt == 0 ? "Text" : "Binary");
                Field(sb, 2, $"Val count: {bind.ParameterValuesCount}");
                foreach (var (len, _) in bind.ParameterValues)
                {
                    Field(sb, 4, $"Len: {len}");
                    if (len > 0) Field(sb, len, "data");
                }
                Field(sb, 2, $"Res fmt count: {bind.ResultsFormatCount}");
                foreach (var fmt in bind.ResultsFormat)
                    Field(sb, 2, fmt == 0 ? "Text" : "Binary");
                break;

            case DescribeMessage describe:
                Field(sb, 1, $"{describe.PortalOrStatement}");
                NullStr(sb, describe.PortalOrStatementName,
                    describe.PortalOrStatement == 'P' ? "Portal" : "Statement");
                break;

            case ExecuteMessage execute:
                NullStr(sb, execute.PortalName, "Portal");
                Field(sb, 4, $"MaxRows: {execute.MaxRows}");
                break;

            case QueryMessage query:
                NullStr(sb, query.Query, "Query");
                break;

            case RowDescriptionMessage rowDesc:
                Field(sb, 2, $"Fields: {rowDesc.FieldCount}");
                foreach (var field in rowDesc.FieldDescriptions)
                {
                    NullStr(sb, field.ColumnName, "Name");
                    Field(sb, 4, $"TableOid: {field.TableOid}");
                    Field(sb, 2, $"ColIdx: {field.ColumnIndex}");
                    Field(sb, 4, $"TypeOid: {field.TypeOid}");
                    Field(sb, 2, $"ColLen: {field.ColumnLength}");
                    Field(sb, 4, $"TypeMod: {field.TypeModifier}");
                    Field(sb, 2, field.Format == 0 ? "Text" : "Binary");
                }
                break;

            case DataRowMessage dataRow:
                Field(sb, 2, $"Fields: {dataRow.FieldCount}");
                foreach (var col in dataRow.ColumnValues)
                {
                    Field(sb, 4, $"Len: {col.Length}");
                    if (col.Length > 0)
                    {
                        string val = Truncate(col.TextRepresentation ?? "(null)", 40);
                        string label = col.Name != null ? $"{Escape(col.Name)}: {Escape(val)}" : Escape(val);
                        Field(sb, col.Length, label);
                    }
                }
                break;

            case CommandCompleteMessage cmdComplete:
                NullStr(sb, cmdComplete.Message, "Tag");
                break;

            case ReadyForQueryMessage rfq:
                Field(sb, 1, $"{rfq.Status}");
                break;

            case ParameterStatusMessage paramStatus:
                NullStr(sb, paramStatus.ParameterName, "Name");
                NullStr(sb, paramStatus.Value, "Value");
                break;

            case BackendKeyDataMessage bkd:
                Field(sb, 4, $"PID: {bkd.ProcessId}");
                Field(sb, 4, $"Key: {bkd.SecretKey}");
                break;

            case StartupMessageMessage startup:
                Field(sb, 4, $"Protocol: {startup.ProtocolMajorVersion}.{startup.ProtocolMinorVersion}");
                foreach (var param in startup.Parameters)
                {
                    NullStr(sb, param.Key);
                    NullStr(sb, param.Value);
                }
                break;

            case FieldListResponseMessage fieldList:
                foreach (var (fieldType, fieldMessage) in fieldList.Fields)
                {
                    Field(sb, 1, $"{fieldType}");
                    NullStr(sb, fieldMessage);
                }
                break;

            case CopyResponseBase copyResp:
                Field(sb, 1, copyResp.OverallFormat == 1 ? "Binary" : "Text");
                Field(sb, 2, $"Cols: {copyResp.ColumnFormats.Count}");
                foreach (var fmt in copyResp.ColumnFormats)
                    Field(sb, 2, fmt == 1 ? "Binary" : "Text");
                break;

            case CopyDataMessage copyData when copyData.IsHeader && copyData.BinaryHeader is { } hdr:
                Field(sb, 11, $"sig: {(hdr.SignatureValid ? "PGCOPY OK" : "MISMATCH")}");
                Field(sb, 4, $"flags: 0x{hdr.Flags:X8}");
                Field(sb, 4, $"ext-len: {hdr.HeaderExtensionLength}");
                if (hdr.HeaderExtensionPreview is { Length: > 0 } extBytes)
                    Field(sb, extBytes.Length, $"ext: {Convert.ToHexString(extBytes)}");
                int afterHeader = copyData.DataLength - 19 - (hdr.HeaderExtensionPreview?.Length ?? 0);
                if (afterHeader > 0)
                    Field(sb, afterHeader, $"tuple data ({afterHeader} bytes)");
                break;

            case CopyDataMessage copyData when copyData.IsTrailer:
                Field(sb, 2, "trailer: FF FF (end of stream)");
                break;

            case CopyDataMessage copyData:
                string suffix = copyData.IsBinaryFormat == true ? ", binary" : "";
                Field(sb, 4, $"Data: {copyData.DataLength} bytes{(copyData.DataLength > CopyDataMessage.PreviewMaxBytes ? " (preview)" : "")}{suffix}");
                if (copyData.PreviewBytes.Length > 0)
                    Field(sb, copyData.PreviewBytes.Length, Convert.ToHexString(copyData.PreviewBytes));
                break;

            case CopyFailMessage copyFail:
                NullStr(sb, copyFail.ErrorMessage, "Error");
                break;

            default:
                // Simple messages (Sync, Terminate, ParseComplete, BindComplete, etc.)
                // have no payload beyond Code + Length — nothing more to emit.
                break;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Emit a field using Mermaid +N syntax where N is the byte count.</summary>
    static void Field(StringBuilder sb, int bytes, string label)
    {
        if (bytes <= 0) return;
        sb.AppendLine($"    +{bytes}: \"{Escape(label)}\"");
    }

    /// <summary>Emit a null-terminated string field with its actual UTF-8 byte size.</summary>
    static void NullStr(StringBuilder sb, string? text, string? prefix = null)
    {
        int bytes = NullTermBytes(text);
        string display = prefix != null
            ? $"{prefix}: {Escape(Truncate(text, 50))}"
            : Escape(Truncate(text, 50));
        Field(sb, bytes, display);
    }

    /// <summary>Byte count of a null-terminated UTF-8 string on the wire.</summary>
    static int NullTermBytes(string? text)
        => text is null or "" ? 1 : Encoding.UTF8.GetByteCount(text) + 1;

    static string Escape(string text) => text.Replace("\"", "'");

    static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
