using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace pg_protoexport;

public class PcapToPlantUmlService(ILogger<PcapToPlantUmlService> logger) : IPcapToPlantUmlService, IPcapExporter
{
    public const string ModeSequenceDiagram = "sequenceDiagram";
    public const string ModePacket = "packet";

    public static IPcapToPlantUmlService Create(ILoggerFactory? loggerFactory = null)
    {
        var log = loggerFactory == null
            ? NullLogger<PcapToPlantUmlService>.Instance
            : loggerFactory.CreateLogger<PcapToPlantUmlService>();

        return new PcapToPlantUmlService(log);
    }

    public string Name => "plantuml";
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
                    $"PlantUML exporter requires mode '{ModeSequenceDiagram}' or '{ModePacket}', got '{mode ?? "<null>"}'.",
                    nameof(mode));
        }
        return new EmptyExportResult();
    }

    public void PcapToSequenceDiagram(IEnumerable<PostgresPacket> packets, string outputFile)
    {
        using var writer = new StreamWriter(outputFile, false);

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
                    writer.WriteLine("```plantuml");
                    writer.WriteLine("@startuml");
                    writer.WriteLine($"participant \"Client ({endpoints.Client}:{endpoints.ClientPort})\" as C");
                    writer.WriteLine($"participant \"Server ({endpoints.Server}:{endpoints.ServerPort})\" as S");
                    diagramOpen = true;
                }

                string arrow = frontEnd ? "C -> S" : "S -> C";
                writer.WriteLine($"{arrow} : {lineLabel}");

                if (closesDiagram)
                {
                    writer.WriteLine("@enduml");
                    writer.WriteLine("```");
                    diagramOpen = false;
                }
            }
        }

        if (diagramOpen)
        {
            writer.WriteLine("@enduml");
            writer.WriteLine("```");
        }

        logger.LogInformation("Wrote {DiagramCount} sequence diagram(s) to {OutputFile}", diagramIndex, outputFile);
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

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static void WritePacketDiagram(StreamWriter writer, PostgresPacket packet)
    {
        foreach (var (message, count) in PostgresPacketSequence.GroupMessages(packet))
        {
            string title = count == 1 ? message.Name : $"{message.Name} (x{count})";

            var root = new Dictionary<string, object?> { [title] = BuildMessageJson(message) };
            string json = JsonSerializer.Serialize(root, JsonOptions);

            writer.WriteLine("```plantuml");
            writer.WriteLine("@startjson");
            writer.WriteLine(json);
            writer.WriteLine("@endjson");
            writer.WriteLine("```");
            writer.WriteLine();
        }
    }

    static Dictionary<string, object?> BuildMessageJson(PostgresMessageBase message)
    {
        var fields = new Dictionary<string, object?>
        {
            ["Code"] = $"{message.Code} (1 byte)",
            ["Length"] = $"{message.Length} (4 bytes)",
        };

        switch (message)
        {
            case ParseMessage parse:
                fields["Stmt"] = NullStrValue(parse.Statement);
                fields["Query"] = NullStrValue(parse.Query);
                fields["Params"] = $"{parse.ParameterCount} (2 bytes)";
                if (parse.ParameterOids.Count > 0)
                    fields["OIDs"] = parse.ParameterOids
                        .Select(o => (object)$"{o} (4 bytes)")
                        .ToList();
                break;

            case BindMessage bind:
                fields["Portal"] = NullStrValue(bind.PortalName);
                fields["Statement"] = NullStrValue(bind.StatementName);
                fields["FmtCount"] = $"{bind.ParameterFormatsCount} (2 bytes)";
                if (bind.ParameterFormats.Count > 0)
                    fields["ParameterFormats"] = bind.ParameterFormats
                        .Select(f => (object)$"{(f == 0 ? "Text" : "Binary")} (2 bytes)")
                        .ToList();
                fields["ValCount"] = $"{bind.ParameterValuesCount} (2 bytes)";
                if (bind.ParameterValues.Count > 0)
                    fields["ParameterValues"] = bind.ParameterValues
                        .Select(pv =>
                        {
                            var entry = new Dictionary<string, object?>
                            {
                                ["Len"] = $"{pv.Length} (4 bytes)"
                            };
                            if (pv.Length > 0)
                                entry["Data"] = $"({pv.Length} bytes)";
                            return (object)entry;
                        })
                        .ToList();
                fields["ResFmtCount"] = $"{bind.ResultsFormatCount} (2 bytes)";
                if (bind.ResultsFormat.Count > 0)
                    fields["ResultFormats"] = bind.ResultsFormat
                        .Select(f => (object)$"{(f == 0 ? "Text" : "Binary")} (2 bytes)")
                        .ToList();
                break;

            case DescribeMessage describe:
                fields["PortalOrStatement"] = $"{describe.PortalOrStatement} (1 byte)";
                string key = describe.PortalOrStatement == 'P' ? "Portal" : "Statement";
                fields[key] = NullStrValue(describe.PortalOrStatementName);
                break;

            case ExecuteMessage execute:
                fields["Portal"] = NullStrValue(execute.PortalName);
                fields["MaxRows"] = $"{execute.MaxRows} (4 bytes)";
                break;

            case QueryMessage query:
                fields["Query"] = NullStrValue(query.Query);
                break;

            case RowDescriptionMessage rowDesc:
                fields["Fields"] = $"{rowDesc.FieldCount} (2 bytes)";
                fields["FieldDescriptions"] = rowDesc.FieldDescriptions
                    .Select(field => (object)new Dictionary<string, object?>
                    {
                        ["Name"] = NullStrValue(field.ColumnName),
                        ["TableOid"] = $"{field.TableOid} (4 bytes)",
                        ["ColIdx"] = $"{field.ColumnIndex} (2 bytes)",
                        ["TypeOid"] = $"{field.TypeOid} (4 bytes)",
                        ["ColLen"] = $"{field.ColumnLength} (2 bytes)",
                        ["TypeMod"] = $"{field.TypeModifier} (4 bytes)",
                        ["Format"] = $"{(field.Format == 0 ? "Text" : "Binary")} (2 bytes)"
                    })
                    .ToList();
                break;

            case DataRowMessage dataRow:
                fields["Fields"] = $"{dataRow.FieldCount} (2 bytes)";
                fields["Columns"] = dataRow.ColumnValues
                    .Select(col =>
                    {
                        var c = new Dictionary<string, object?>
                        {
                            ["Len"] = $"{col.Length} (4 bytes)"
                        };
                        if (col.Length > 0)
                        {
                            string val = Truncate(col.TextRepresentation ?? "(null)", 40);
                            c["Value"] = col.Name != null
                                ? $"{col.Name}: \"{val}\" ({col.Length} bytes)"
                                : $"\"{val}\" ({col.Length} bytes)";
                        }
                        return (object)c;
                    })
                    .ToList();
                break;

            case CommandCompleteMessage cmdComplete:
                fields["Tag"] = NullStrValue(cmdComplete.Message);
                break;

            case ReadyForQueryMessage rfq:
                fields["Status"] = $"{rfq.Status} (1 byte)";
                break;

            case ParameterStatusMessage paramStatus:
                fields["Name"] = NullStrValue(paramStatus.ParameterName);
                fields["Value"] = NullStrValue(paramStatus.Value);
                break;

            case BackendKeyDataMessage bkd:
                fields["PID"] = $"{bkd.ProcessId} (4 bytes)";
                fields["Key"] = $"{bkd.SecretKey} (4 bytes)";
                break;

            case StartupMessageMessage startup:
                fields["Protocol"] = $"{startup.ProtocolMajorVersion}.{startup.ProtocolMinorVersion} (4 bytes)";
                if (startup.Parameters.Count > 0)
                    fields["Parameters"] = startup.Parameters
                        .Select(p => (object)new Dictionary<string, object?>
                        {
                            ["Name"] = NullStrValue(p.Key),
                            ["Value"] = NullStrValue(p.Value)
                        })
                        .ToList();
                break;

            case FieldListResponseMessage fieldList:
                if (fieldList.Fields.Count > 0)
                    fields["FieldList"] = fieldList.Fields
                        .Select(item => (object)new Dictionary<string, object?>
                        {
                            ["Type"] = $"{item.FieldType} (1 byte)",
                            ["Message"] = NullStrValue(item.Message)
                        })
                        .ToList();
                break;

            case CopyResponseBase copyResp:
                fields["OverallFormat"] = $"{(copyResp.OverallFormat == 1 ? "binary" : "text")} (1 byte)";
                fields["ColumnCount"] = $"{copyResp.ColumnFormats.Count} (2 bytes)";
                if (copyResp.ColumnFormats.Count > 0)
                    fields["ColumnFormats"] = copyResp.ColumnFormats
                        .Select(f => (object)$"{(f == 1 ? "binary" : "text")} (2 bytes)")
                        .ToList();
                break;

            case CopyDataMessage copyData when copyData.IsHeader && copyData.BinaryHeader is { } hdr:
                fields["DataLength"] = $"{copyData.DataLength} byte{(copyData.DataLength == 1 ? "" : "s")} [binary header]";
                fields["Signature"] = $"\"{Convert.ToHexString(hdr.Signature)}\" ({(hdr.SignatureValid ? "PGCOPY OK" : "MISMATCH")}, 11 bytes)";
                fields["Flags"] = $"0x{hdr.Flags:X8} (4 bytes)";
                fields["HeaderExtensionLength"] = $"{hdr.HeaderExtensionLength} (4 bytes)";
                if (hdr.HeaderExtensionPreview is { Length: > 0 } ext)
                    fields["HeaderExtension"] = $"\"{Convert.ToHexString(ext)}\" ({ext.Length} bytes)";
                int tupleBytes = copyData.DataLength - 19 - (hdr.HeaderExtensionPreview?.Length ?? 0);
                if (tupleBytes > 0)
                    fields["TupleData"] = $"({tupleBytes} bytes)";
                break;

            case CopyDataMessage copyData when copyData.IsTrailer:
                fields["DataLength"] = $"{copyData.DataLength} bytes [binary trailer]";
                fields["Trailer"] = "\"FFFF\" (end-of-stream, 2 bytes)";
                break;

            case CopyDataMessage copyData:
                string flavor = copyData.IsBinaryFormat == true ? " [binary]" : "";
                fields["DataLength"] = $"{copyData.DataLength} byte{(copyData.DataLength == 1 ? "" : "s")}{flavor}";
                if (copyData.PreviewBytes.Length > 0)
                    fields["Preview"] = $"\"{Convert.ToHexString(copyData.PreviewBytes)}\"{(copyData.DataLength > CopyDataMessage.PreviewMaxBytes ? " (truncated)" : "")} ({copyData.PreviewBytes.Length} bytes)";
                break;

            case CopyFailMessage copyFail:
                fields["Error"] = NullStrValue(copyFail.ErrorMessage);
                break;

            default:
                // Simple messages (Sync, Terminate, ParseComplete, BindComplete, etc.)
                // have no payload beyond Code + Length — nothing more to emit.
                break;
        }

        return fields;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static string NullStrValue(string? text)
    {
        int bytes = NullTermBytes(text);
        string val = Truncate(text, 50);
        return $"\"{val}\" ({bytes} byte{(bytes == 1 ? "" : "s")})";
    }

    static int NullTermBytes(string? text)
        => text is null or "" ? 1 : Encoding.UTF8.GetByteCount(text) + 1;

    static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
