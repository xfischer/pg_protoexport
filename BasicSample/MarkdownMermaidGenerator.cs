using System.Text;
using pg_protoexport;

namespace BasicSample;

internal static class MarkdownMermaidGenerator
{
    private const int RowBits = 32;

    public static string GenerateMarkdownMermaid(List<PostgresPacket> pgPackets)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine($"""
            # Sequence diagram

            Recorded : {pgPackets[0].Timestamp:O}

            ## Compact diagram

            """);

        markdown.AppendLine(PacketsToCompactMarkdownMermaid(pgPackets));

        markdown.AppendLine("""
            ## Detailed diagram

            """);
        markdown.AppendLine(PacketsToDetailedMarkdownMermaid(pgPackets));

        markdown.AppendLine("""
            ## Packet detail

            """);

        int packetIndex = 0;
        foreach (var packet in pgPackets)
        {
            packetIndex++;
            string direction = packet.IsFrontEnd
                ? "FrontEnd --> BackEnd"
                : "FrontEnd <-- BackEnd";
            markdown.AppendLine($"### Packet {packetIndex} ({packet.Messages.Count} messages, {direction})");
            markdown.AppendLine();
            markdown.AppendLine(PacketToMermaidPacketDiagram(packet));
        }

        return markdown.ToString();
    }

    static string PacketToMermaidPacketDiagram(PostgresPacket packet)
    {
        var sb = new StringBuilder();
        sb.AppendLine("```mermaid");
        sb.AppendLine("packet");

        // Group consecutive messages of the same type
        List<(PostgresMessageBase Message, int Count)> messagesGrouped = [];
        foreach (var message in packet.Messages)
        {
            if (messagesGrouped.Count > 0 && messagesGrouped[^1].Message.Code == message.Code)
            {
                messagesGrouped[^1] = (messagesGrouped[^1].Message, messagesGrouped[^1].Count + 1);
            }
            else
            {
                messagesGrouped.Add((message, 1));
            }
        }

        int bit = 0;
        foreach (var (message, count) in messagesGrouped)
        {
            if (count > 1)
            {
                // First occurrence: render full detail
                bit = AppendMessageFields(sb, message, bit);
                // Collapsed remainder
                int skippedStart = bit;
                int skippedEnd = FillRow(bit);
                sb.AppendLine($"    {skippedStart}-{skippedEnd}: \"{message.Name} x{count - 1} skipped\"");
                bit = skippedEnd + 1;
            }
            else
            {
                bit = AppendMessageFields(sb, message, bit);
            }
        }

        sb.AppendLine("```");
        return sb.ToString();
    }

    static int AppendMessageFields(StringBuilder sb, PostgresMessageBase message, int bit)
    {
        // Standard header: Code (1 unit) + Length (4 units)
        sb.AppendLine($"    {bit}-{bit}: \"{message.Code}\"");
        bit += 1;
        sb.AppendLine($"    {bit}-{bit + 3}: \"length: {message.Length}\"");
        bit += 4;

        int remaining = RowBits - 5; // 27 units left on this row

        switch (message)
        {
            case ParseMessage parse:
            {
                string stmt = Truncate(parse.Statement, 12) ?? "(null)";
                string query = Truncate(parse.Query, 30);
                sb.AppendLine($"    {bit}-{bit + 5}: \"Stmt: {Escape(stmt)}\"");
                bit += 6;
                sb.AppendLine($"    {bit}-{bit + 18}: \"Query: {Escape(query)}\"");
                bit += 19;
                sb.AppendLine($"    {bit}-{bit + 1}: \"params: {parse.ParameterCount}\"");
                bit += 2;
                // Parameter OIDs on next row
                foreach (var oid in parse.ParameterOids)
                {
                    sb.AppendLine($"    {bit}-{bit + 3}: \"oid: {oid}\"");
                    bit += 4;
                }
                bit = PadRow(sb, bit);
                break;
            }
            case BindMessage bind:
            {
                sb.AppendLine($"    {bit}-{bit + 12}: \"portal: {Escape(Truncate(bind.PortalName, 10))}\"");
                bit += 13;
                sb.AppendLine($"    {bit}-{bit + 13}: \"statement: {Escape(Truncate(bind.StatementName, 10))}\"");
                bit += 14;
                // Formats row
                sb.AppendLine($"    {bit}-{bit + 1}: \"fmt cnt: {bind.ParameterFormatsCount}\"");
                bit += 2;
                bit = PadRow(sb, bit);
                // Values row
                sb.AppendLine($"    {bit}-{bit + 1}: \"val cnt: {bind.ParameterValuesCount}\"");
                bit += 2;
                foreach (var (len, _) in bind.ParameterValues.Take(4))
                {
                    sb.AppendLine($"    {bit}-{bit + 3}: \"len: {len}\"");
                    bit += 4;
                }
                bit = PadRow(sb, bit);
                // Results format row
                sb.AppendLine($"    {bit}-{bit + 1}: \"res cnt: {bind.ResultsFormatCount}\"");
                bit += 2;
                foreach (var fmt in bind.ResultsFormat.Take(6))
                {
                    sb.AppendLine($"    {bit}-{bit + 1}: \"{(fmt == 0 ? "Text" : "Binary")}\"");
                    bit += 2;
                }
                bit = PadRow(sb, bit);
                break;
            }
            case DescribeMessage describe:
            {
                sb.AppendLine($"    {bit}-{bit}: \"{describe.PortalOrStatement}\"");
                bit += 1;
                string name = Truncate(describe.PortalOrStatementName, 30);
                sb.AppendLine($"    {bit}-{bit + 25}: \"{(describe.PortalOrStatement == 'P' ? "portal" : "statement")}: {Escape(name)}\"");
                bit += 26;
                break;
            }
            case ExecuteMessage execute:
            {
                string portal = Truncate(execute.PortalName, 20);
                sb.AppendLine($"    {bit}-{bit + 22}: \"portal: {Escape(portal)}\"");
                bit += 23;
                sb.AppendLine($"    {bit}-{bit + 3}: \"maxrows: {execute.MaxRows}\"");
                bit += 4;
                break;
            }
            case QueryMessage query:
            {
                string q = Truncate(query.Query, 40);
                sb.AppendLine($"    {bit}-{bit + remaining - 1}: \"Query: {Escape(q)}\"");
                bit += remaining;
                break;
            }
            case RowDescriptionMessage rowDesc:
            {
                sb.AppendLine($"    {bit}-{bit + 1}: \"fields: {rowDesc.FieldCount}\"");
                bit += 2;
                bit = PadRow(sb, bit);
                // Each field description on its own row
                foreach (var field in rowDesc.FieldDescriptions)
                {
                    sb.AppendLine($"    {bit}-{bit + 13}: \"name: {Escape(Truncate(field.ColumnName, 12))}\"");
                    bit += 14;
                    sb.AppendLine($"    {bit}-{bit + 3}: \"tbl oid: {field.TableOid}\"");
                    bit += 4;
                    sb.AppendLine($"    {bit}-{bit + 1}: \"idx: {field.ColumnIndex}\"");
                    bit += 2;
                    sb.AppendLine($"    {bit}-{bit + 3}: \"type: {field.TypeOid}\"");
                    bit += 4;
                    sb.AppendLine($"    {bit}-{bit + 1}: \"len: {field.ColumnLength}\"");
                    bit += 2;
                    sb.AppendLine($"    {bit}-{bit + 3}: \"mod: {field.TypeModifier}\"");
                    bit += 4;
                    sb.AppendLine($"    {bit}-{bit + 1}: \"{(field.Format == 0 ? "Text" : "Binary")}\"");
                    bit += 2;
                }
                break;
            }
            case DataRowMessage dataRow:
            {
                sb.AppendLine($"    {bit}-{bit + 1}: \"fields: {dataRow.FieldCount}\"");
                bit += 2;
                bit = PadRow(sb, bit);
                // Each column value on its own row
                foreach (var col in dataRow.ColumnValues)
                {
                    sb.AppendLine($"    {bit}-{bit + 3}: \"len: {col.Length}\"");
                    bit += 4;
                    string val = Truncate(col.TextRepresentation ?? "(null)", 30);
                    string label = col.Name != null ? $"{Escape(col.Name)}: {Escape(val)}" : Escape(val);
                    sb.AppendLine($"    {bit}-{bit + 27}: \"{label}\"");
                    bit += 28;
                }
                break;
            }
            case CommandCompleteMessage cmdComplete:
            {
                sb.AppendLine($"    {bit}-{bit + remaining - 1}: \"{Escape(Truncate(cmdComplete.Message, 30))}\"");
                bit += remaining;
                break;
            }
            case ReadyForQueryMessage rfq:
            {
                sb.AppendLine($"    {bit}-{bit + remaining - 1}: \"{rfq.Status}\"");
                bit += remaining;
                break;
            }
            case ParameterStatusMessage paramStatus:
            {
                sb.AppendLine($"    {bit}-{bit + 12}: \"{Escape(Truncate(paramStatus.ParameterName, 14))}\"");
                bit += 13;
                sb.AppendLine($"    {bit}-{bit + 13}: \"{Escape(Truncate(paramStatus.Value, 16))}\"");
                bit += 14;
                break;
            }
            case BackendKeyDataMessage bkd:
            {
                sb.AppendLine($"    {bit}-{bit + 13}: \"PID: {bkd.ProcessId}\"");
                bit += 14;
                sb.AppendLine($"    {bit}-{bit + 12}: \"Key: {bkd.SecretKey}\"");
                bit += 13;
                break;
            }
            case StartupMessageMessage startup:
            {
                sb.AppendLine($"    {bit}-{bit + 3}: \"v{startup.ProtocolMajorVersion}.{startup.ProtocolMinorVersion}\"");
                bit += 4;
                bit = PadRow(sb, bit);
                foreach (var param in startup.Parameters)
                {
                    sb.AppendLine($"    {bit}-{bit + 13}: \"{Escape(Truncate(param.Key, 14))}\"");
                    bit += 14;
                    sb.AppendLine($"    {bit}-{bit + 17}: \"{Escape(Truncate(param.Value, 20))}\"");
                    bit += 18;
                }
                break;
            }
            case FieldListResponseMessage fieldList: // ErrorResponse, NoticeResponse
            {
                bit = PadRow(sb, bit);
                foreach (var (fieldType, fieldMessage) in fieldList.Fields)
                {
                    sb.AppendLine($"    {bit}-{bit + 1}: \"{fieldType}\"");
                    bit += 2;
                    sb.AppendLine($"    {bit}-{bit + 29}: \"{Escape(Truncate(fieldMessage, 35))}\"");
                    bit += 30;
                }
                break;
            }
            default:
            {
                // Simple messages (Sync, Terminate, ParseComplete, BindComplete, NoData, etc.)
                string repr = message.GetStringRepresentation();
                if (!string.IsNullOrWhiteSpace(repr) && repr != message.GetType().Name)
                {
                    sb.AppendLine($"    {bit}-{bit + remaining - 1}: \"{Escape(Truncate(repr, 35))}\"");
                }
                else
                {
                    sb.AppendLine($"    {bit}-{bit + remaining - 1}: \" \"");
                }
                bit += remaining;
                break;
            }
        }

        return bit;
    }

    /// <summary>Pad the rest of the current row with empty space if not already at a row boundary.</summary>
    static int PadRow(StringBuilder sb, int bit)
    {
        int remainder = bit % RowBits;
        if (remainder != 0)
        {
            int end = FillRow(bit);
            sb.AppendLine($"    {bit}-{end}: \" \"");
            return end + 1;
        }
        return bit;
    }

    /// <summary>Returns the last bit index of the current row.</summary>
    static int FillRow(int bit)
    {
        int row = bit / RowBits;
        return (row + 1) * RowBits - 1;
    }

    static string Escape(string text)
    {
        return text.Replace("\"", "'");
    }

    static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }

    // ── Sequence diagram helpers (compact + detailed) ───────────────────

    static string PacketsToDetailedMarkdownMermaid(List<PostgresPacket> pgPackets)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine($"""
            ```mermaid
            sequenceDiagram
                participant C as Client<br/>{pgPackets[0].SourceAddress}:{pgPackets[0].SourcePort}
                participant S as Server<br/>{pgPackets[0].DestinationAddress}:{pgPackets[0].DestinationPort}
            """);

        int packetIndex = 0;
        DateTime initialDate = default;
        DateTime lastDate = default;
        foreach (var packet in pgPackets)
        {
            if (packetIndex == 0)
            {
                initialDate = packet.Timestamp;
                markdown.AppendLine($"    rect rgb(240, 240, 240)");
                markdown.AppendLine($"    Note over C,S: packet {++packetIndex}");
            }
            else
            {
                markdown.AppendLine($"    rect rgb(240, 240, 240)");
                markdown.Append($"    Note over C,S: packet {++packetIndex} | ");
                markdown.Append($"{(packet.Timestamp - initialDate).TotalMilliseconds:N1} ms TOTAL");
                markdown.AppendLine($" | +{(packet.Timestamp - lastDate).TotalMilliseconds:N1} ms SINCE last");
            }
            lastDate = packet.Timestamp;

            markdown.AppendLine(PacketToSequenceMermaid(packet));
            markdown.AppendLine("    end");
        }

        markdown.AppendLine("""
            ```
            """);

        return markdown.ToString();
    }

    static string PacketsToCompactMarkdownMermaid(List<PostgresPacket> pgPackets)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine($"""
            ```mermaid
            sequenceDiagram
                participant C as Client<br/>{pgPackets[0].SourceAddress}:{pgPackets[0].SourcePort}
                participant S as Server<br/>{pgPackets[0].DestinationAddress}:{pgPackets[0].DestinationPort}
            """);

        foreach (var packet in pgPackets)
        {
            markdown.AppendLine(PacketToCompactSequenceMermaid(packet));
        }

        markdown.AppendLine("""
            ```
            """);

        return markdown.ToString();
    }

    static List<(PostgresMessageBase Message, int Count)> GroupMessages(PostgresPacket packet)
    {
        List<(PostgresMessageBase Message, int Count)> grouped = [];
        foreach (var message in packet.Messages)
        {
            if (grouped.Count > 0 && grouped[^1].Message.Code == message.Code)
            {
                grouped[^1] = (grouped[^1].Message, grouped[^1].Count + 1);
            }
            else
            {
                grouped.Add((message, 1));
            }
        }
        return grouped;
    }

    static string PacketToSequenceMermaid(PostgresPacket packet)
    {
        var sb = new StringBuilder();
        string arrow = packet.IsFrontEnd ? "C->>S" : "S->>C";
        foreach (var (message, count) in GroupMessages(packet))
        {
            sb.Append($"    {arrow}: ");
            sb.AppendLine(count == 1 ? message.Name : $"{message.Name} (x{count})");
        }
        return sb.ToString();
    }

    static string PacketToCompactSequenceMermaid(PostgresPacket packet)
    {
        var sb = new StringBuilder();
        string arrow = packet.IsFrontEnd ? "C->>S" : "S->>C";
        sb.Append($"    {arrow}: ");
        sb.AppendLine(string.Join(" / ", GroupMessages(packet).Select(m =>
                                    m.Count == 1 ?
                                    m.Message.Name
                                    : $"{m.Message.Name} (x{m.Count})"
                                    )));
        return sb.ToString();
    }
}
