using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace pg_protoexport;

public class PcapToHtmlService(ILogger<PcapToHtmlService> logger, IPcapToMermaidService mermaidService) : IPcapToHtmlService, IPcapExporter
{
    public string Name => "html";
    public string DefaultExtension => ".html";

    public IExportResult Export(IEnumerable<PostgresPacket> packets, string outputPath, string? mode, IExportOptions? options)
    {
        PcapToHtml(packets, outputPath);
        return new EmptyExportResult();
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static IPcapToHtmlService Create(ILoggerFactory? loggerFactory = null)
    {
        return new PcapToHtmlService(loggerFactory.CreateLoggerOrNull<PcapToHtmlService>(), PcapToMermaidService.Create(loggerFactory));
    }

    public void PcapToHtml(IEnumerable<PostgresPacket> packets, string outputFile)
    {
        var materialized = packets.ToList();

        var mermaidWriter = new StringWriter();
        mermaidService.PcapToSequenceDiagram(materialized, mermaidWriter);
        var mermaidDiagrams = ExtractMermaidGraphs(mermaidWriter.ToString());

        var rationales = LoadEmbeddedJson<Dictionary<string, string>>("Assets.rationales.json") ?? new();
        var cards = new List<HtmlMessageCard>();
        var orderedMessages = new List<PostgresMessageBase>();
        // Track the first card index for each client-side TCP port we encounter, so CancelRequest
        // cards can link back to the originating session's first card.
        var firstCardIdxByClientPort = new Dictionary<ushort, int>();
        int idx = 0;
        foreach (var (packet, message, snapshot) in ProtocolStateProjector.Project(materialized))
        {
            ushort clientPort = packet.IsFrontEnd ? packet.SourcePort : packet.DestinationPort;
            if (!firstCardIdxByClientPort.ContainsKey(clientPort))
                firstCardIdxByClientPort[clientPort] = idx;

            var card = BuildCard(idx, packet, message, snapshot, rationales);
            if (message is CancelRequestMessage cancel
                && cancel.CorrelatedClientPort is ushort targetPort
                && firstCardIdxByClientPort.TryGetValue(targetPort, out var targetIdx))
            {
                card = card with { CorrelatedCardIdx = targetIdx };
            }
            cards.Add(card);
            orderedMessages.Add(message);
            idx++;
        }

        var interludeEntries = LoadEmbeddedJson<Dictionary<string, InterludeEntry>>("Assets.interludes.json") ?? new();
        var interludes = InterludeDetector.Detect(orderedMessages, new InterludeCatalog(interludeEntries));

        var data = new HtmlExportData(
            Title: Path.GetFileNameWithoutExtension(outputFile),
            GeneratedAt: DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            MermaidDiagrams: mermaidDiagrams,
            Cards: cards,
            Interludes: interludes,
            Glossary: LoadEmbeddedJson<Dictionary<string, HtmlGlossaryEntry>>("Assets.glossary.json") ?? new(),
            Rationales: rationales);

        string template = LoadEmbeddedText("Assets.template.html");
        string json = JsonSerializer.Serialize(data, JsonOpts);
        string html = template
            .Replace("{{TITLE}}", System.Net.WebUtility.HtmlEncode(data.Title))
            .Replace("{{INLINE_DATA}}", json);

        File.WriteAllText(outputFile, html);

        string assetsDir = Path.Combine(
            Path.GetDirectoryName(outputFile) ?? ".",
            Path.GetFileNameWithoutExtension(outputFile) + "_assets");
        Directory.CreateDirectory(assetsDir);
        File.WriteAllText(Path.Combine(assetsDir, "styles.css"), LoadEmbeddedText("Assets.styles.css"));
        File.WriteAllText(Path.Combine(assetsDir, "app.js"), LoadEmbeddedText("Assets.app.js"));
        File.WriteAllText(Path.Combine(assetsDir, "mermaid.min.js"), LoadEmbeddedText("Assets.mermaid.min.js"));

        logger.LogInformation("Wrote HTML report to {OutputFile} ({CardCount} cards, {InterludeCount} interludes)", outputFile, cards.Count, interludes.Count);
    }

    private static HtmlMessageCard BuildCard(int idx, PostgresPacket packet, PostgresMessageBase message, ProtocolStateSnapshot snap, Dictionary<string, string> rationales)
    {
        string direction = message.FrontEnd ? "C->S" : "S->C";

        int length = message.OnWireLength > 0 ? message.OnWireLength : 0;

        var fields = new List<HtmlField>();
        foreach (var pf in message.ParsedFields)
        {
            string display = pf.DisplayValue ?? "";
            fields.Add(new HtmlField(
                Id: $"f{idx}-{Sanitize(pf.Name)}",
                Name: pf.Name,
                Display: display,
                Offset: pf.Offset,
                Length: pf.Length));
        }

        rationales.TryGetValue(message.Name, out var rationale);
        if (message is AuthenticationGenericMessage authMsg && rationales.TryGetValue(authMsg.AuthenticationName, out var authRationale))
            rationale = authRationale;
        if (message is CopyDataMessage cd)
        {
            string? subKey = cd.IsHeader ? "CopyDataBinaryHeader"
                : cd.IsTrailer ? "CopyDataBinaryTrailer"
                : cd.IsBinaryFormat == true ? "CopyDataBinary"
                : null;
            if (subKey is not null && rationales.TryGetValue(subKey, out var sub))
                rationale = sub;
        }

        return new HtmlMessageCard(
            Idx: idx,
            PacketIndex: packet.PacketIndex,
            Direction: direction,
            Code: message.Code.ToString(),
            Name: message.Name,
            LengthBytes: length,
            Headline: BuildHeadline(message),
            Fields: fields,
            Rationale: rationale,
            StateAfter: ToHtmlSnapshot(snap));
    }

    private static HtmlStateSnapshot ToHtmlSnapshot(ProtocolStateSnapshot s) => new(
        ConnState: s.ConnState.ToString(),
        TxStatus: s.TxStatus.ToString(),
        Prepared: new Dictionary<string, string>(s.Prepared),
        Portals: new Dictionary<string, string>(s.Portals),
        ServerParams: new Dictionary<string, string>(s.ServerParams),
        BackendPid: s.BackendPid,
        CopyMode: s.CopyMode == CopyStreamKind.None ? null : s.CopyMode.ToString(),
        CopyFormat: s.CopyFormat?.ToString());

    private static string BuildHeadline(PostgresMessageBase m) => m switch
    {
        SSLRequestMessage => "Client asks: do you accept TLS?",
        GSSENCRequestMessage => "Client asks: do you accept GSSAPI encryption?",
        CancelRequestMessage c => $"Client requests cancellation of in-flight query (pid {c.ProcessId})",
        SSLResponseMessage s => s.Accepted ? "Server: yes, TLS is supported" : "Server: no, continue without TLS",
        GSSENCResponseMessage g => g.Accepted ? "Server: yes, GSSAPI is supported" : "Server: no, continue without GSSAPI",
        CopyInResponseMessage c => $"Server: ready to receive bulk data ({CopyFormatLabel(c.OverallFormat)}, {c.ColumnFormats.Count} column(s))",
        CopyOutResponseMessage c => $"Server: about to stream bulk data ({CopyFormatLabel(c.OverallFormat)}, {c.ColumnFormats.Count} column(s))",
        CopyBothResponseMessage c => $"Server: bidirectional COPY for replication ({CopyFormatLabel(c.OverallFormat)}, {c.ColumnFormats.Count} column(s))",
        CopyDataMessage d when d.IsHeader && d.BinaryHeader is { SignatureValid: true } okHdr
            => $"Binary COPY header — magic OK, flags=0x{okHdr.Flags:X8}, {okHdr.HeaderExtensionLength} extension byte(s)",
        CopyDataMessage d when d.IsHeader && d.BinaryHeader is { } badHdr
            => $"Binary COPY header — magic MISMATCH (got {Convert.ToHexString(badHdr.Signature)}, capture may be truncated)",
        CopyDataMessage d when d.IsTrailer
            => "Binary COPY trailer (end-of-stream marker FF FF)",
        CopyDataMessage d when d.IsBinaryFormat == true
            => $"Binary COPY tuple data ({d.DataLength} byte(s))",
        CopyDataMessage d => $"COPY data chunk ({d.DataLength} byte(s))",
        CopyDoneMessage => "End of COPY stream",
        CopyFailMessage f => $"Client aborts COPY: {Truncate(f.ErrorMessage, 80)}",
        StartupMessageMessage s => $"Client connects (protocol {s.ProtocolMajorVersion}.{s.ProtocolMinorVersion})",
        AuthenticationGenericMessage a => $"Server authentication: {a.AuthenticationName}",
        ParameterStatusMessage p => $"Server reports {p.ParameterName} = {p.Value}",
        BackendKeyDataMessage b => $"Server identifies itself (PID {b.ProcessId})",
        ReadyForQueryMessage r => $"Server ready ({r.Status})",
        QueryMessage q => $"Client runs simple query: {Truncate(q.Query, 80)}",
        ParseMessage p => $"Client prepares {NameOrUnnamed(p.Statement, "statement")}",
        BindMessage b => $"Client binds {NameOrUnnamed(b.PortalName, "portal")} to {NameOrUnnamed(b.StatementName, "statement")}",
        DescribeMessage d => $"Client asks to describe {NameOrUnnamed(d.PortalOrStatementName, d.PortalOrStatement == 'P' ? "portal" : "statement")}",
        ExecuteMessage e => $"Client executes {NameOrUnnamed(e.PortalName, "portal")} ({(e.MaxRows == 0 ? "all rows" : $"max {e.MaxRows} rows")})",
        SyncMessage => "Client sync: end of extended-query batch",
        RowDescriptionMessage r => $"Server describes {r.FieldCount} result column(s)",
        DataRowMessage d => $"Server returns row with {d.FieldCount} value(s)",
        CommandCompleteMessage c => $"Server completes: {c.Message}",
        ErrorResponseMessage => "Server reports an error",
        TerminateMessage => "Client terminates the connection",
        _ => m.Name
    };

    private static string NameOrUnnamed(string? name, string kind) =>
        string.IsNullOrEmpty(name) ? $"unnamed {kind}" : $"{kind} '{name}'";

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 3)] + "...";

    private static string CopyFormatLabel(byte overallFormat) => overallFormat == 1 ? "binary" : "text";

    private static string Sanitize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }

    private static List<string> ExtractMermaidGraphs(string markdownWithFences)
    {
        var diagrams = new List<string>();
        StringBuilder? current = null;
        foreach (var line in markdownWithFences.Split('\n'))
        {
            string trimmed = line.TrimEnd('\r');
            if (trimmed.StartsWith("```mermaid"))
            {
                current = new StringBuilder();
                continue;
            }
            if (trimmed == "```" && current is not null)
            {
                diagrams.Add(current.ToString().TrimEnd());
                current = null;
                continue;
            }
            current?.AppendLine(trimmed);
        }
        return diagrams;
    }

    private static string LoadEmbeddedText(string resourceSuffix)
    {
        var asm = typeof(PcapToHtmlService).Assembly;
        string fullName = $"pg_protoexport.export.html.{resourceSuffix}";
        using var stream = asm.GetManifestResourceStream(fullName)
            ?? throw new FileNotFoundException($"Embedded resource '{fullName}' not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static T? LoadEmbeddedJson<T>(string resourceSuffix)
    {
        var text = LoadEmbeddedText(resourceSuffix);
        return JsonSerializer.Deserialize<T>(text, JsonOpts);
    }
}
