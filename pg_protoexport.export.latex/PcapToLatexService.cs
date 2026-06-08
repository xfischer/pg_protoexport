using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text;
using pg_protoexport.Templates;

namespace pg_protoexport;

public sealed class PcapToLatexService(ILogger<PcapToLatexService> logger, IOptions<PcapToLatexOptions> pcapPostgresOptions) : IPcapToLatexService, IPcapExporter
{
    public string Name => "latex";
    public string DefaultExtension => ".tex";

    public IExportResult Export(IEnumerable<PostgresPacket> packets, string outputPath, string? mode, IExportOptions? options)
    {
        var opts = options as LatexExportOptions ?? LatexExportOptions.Default;
        var render = opts.Render ?? ResolveDefaultRender();

        var state = opts.MultipleFiles
            ? PcapToLaTeX_MultipleFiles(packets, outputPath, render)
            : PcapToLaTeX(packets, outputPath, opts.Standalone, render);

        return LatexExportResult.From(state);
    }

    public static IPcapToLatexService Create(ILoggerFactory? loggerFactory = null, PcapToLatexOptions? options = null)
    {
        options ??= new PcapToLatexOptions();

        var logger = loggerFactory == null ?
                        NullLogger<PcapToLatexService>.Instance
                        : loggerFactory.CreateLogger<PcapToLatexService>();

        return new PcapToLatexService(logger, Options.Create(options));
    }

    private PcapToLatexOptions LatexOptions { get; init; } = pcapPostgresOptions.Value;

    const int MaxLatexRowsPerPage = 21;

    public GenerationState PcapToLaTeX(IEnumerable<PostgresPacket> pgSqlPackets, string latexOutputFile, bool standalone = true)
        => PcapToLaTeX(pgSqlPackets, latexOutputFile, standalone, ResolveDefaultRender());

    public GenerationState PcapToLaTeX(IEnumerable<PostgresPacket> pgSqlPackets, string latexOutputFile, bool standalone, LatexRenderOptions render)
    {
        var outputDir = Path.GetDirectoryName(latexOutputFile);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir!);

        using var writer = new FileStream(latexOutputFile, FileMode.Create);
        return PcapToLaTeX(pgSqlPackets, writer, standalone, render);
    }

    public GenerationState PcapToLaTeX(IEnumerable<PostgresPacket> pgSqlPackets, Stream outputStream, bool standalone = true)
        => PcapToLaTeX(pgSqlPackets, outputStream, standalone, ResolveDefaultRender());

    public GenerationState PcapToLaTeX(IEnumerable<PostgresPacket> pgSqlPackets, Stream outputStream, bool standalone, LatexRenderOptions render)
    {
        GenerationState state = new(standalone: standalone) { Render = render };

        var fileLatexBuilder = new StringBuilder();
        int packetIndex = 1;

        try
        {
            foreach (var packet in pgSqlPackets)
            {
                state.LatexRowCount = 0;
                // Packet Footer
                if (packetIndex > 1)
                {
                    bool newChapter = state.LastMessage is ReadyForQueryMessage;

                    fileLatexBuilder.AppendLine(new PacketFooter(newChapter, state).TransformText());
                    state.LatexRowCount += (newChapter ? 1 : 0);
                }

                // Packet Header
                fileLatexBuilder.AppendLine(new PacketHeader(packet.Messages, packet.IsFrontEnd, packetIndex, state).TransformText());

                foreach (var pgMessage in packet.Messages)
                {
                    if (ProcessPostgresMessage(pgMessage, state, fileLatexBuilder))
                    {
                        state.StatsMessagesProcessed++;
                    }
                    else
                    {
                        state.StatsMessagesInvalid++;
                        var msgCode = pgMessage.GetType().Name;
                        fileLatexBuilder.AppendLine($"No message definition found for code '{msgCode}'");
                        fileLatexBuilder.AppendLine("\\\\");
                        logger.LogWarning("No message definition found for code '{MessageCode}'", pgMessage.GetType().Name);
                    }
                }
                packetIndex++;
                state.StatsPacketsProcessed++;
            }
        }
        finally // write even if error occured
        {
            // Last packet Footer
            fileLatexBuilder.AppendLine(new PacketFooter(newChapter: false, state).TransformText());

            // Footer
            fileLatexBuilder.AppendLine(new Footer(state).TransformText());

            // Header INSERTION AT BEGINNING
            var headerMsg = $"PostgreSQL packets. {packetIndex - 1} packet(s).";
            var headerDelegate = LatexOptions?.CustomHeaderProvider;
            
            ITextTransformer? header = headerDelegate?.Invoke(headerMsg, state);
            header ??= new Header(headerMsg, state);
            
            fileLatexBuilder.Insert(0, header!.TransformText() + Environment.NewLine);

            var finalLatex = fileLatexBuilder.ToString();
            using var writer = new StreamWriter(outputStream, leaveOpen: true);
            writer.Write(finalLatex);
        }

        return state;
    }

    public GenerationState PcapToLaTeX_MultipleFiles(IEnumerable<PostgresPacket> pgSqlPackets, string latexOutputDirectory)
        => PcapToLaTeX_MultipleFiles(pgSqlPackets, latexOutputDirectory, ResolveDefaultRender());

    public GenerationState PcapToLaTeX_MultipleFiles(IEnumerable<PostgresPacket> pgSqlPackets, string latexOutputDirectory, LatexRenderOptions render)
    {
        GenerationState state = new(standalone: true, multiple: true) { Render = render };

        int packetIndex = 1;
        if (!Directory.Exists(latexOutputDirectory))
            Directory.CreateDirectory(latexOutputDirectory);

        foreach (var packet in pgSqlPackets)
        {
            foreach (var pgMessage in packet.Messages)
            {
                var fileLatexBuilder = new StringBuilder();
                // Packet Header
                fileLatexBuilder.AppendLine(new PacketHeader(packet.Messages, packet.IsFrontEnd, packetIndex, state).TransformText());

                bool success = ProcessPostgresMessage(pgMessage, state, fileLatexBuilder, (builder, stateObj) =>
                {
                    state.StatsMessagesProcessed++;

                    // Last packet Footer
                    fileLatexBuilder.AppendLine(new PacketFooter(newChapter: false, state).TransformText());

                    // Footer
                    fileLatexBuilder.AppendLine(new Footer(state).TransformText());

                    // Header INSERTION AT BEGINNING
                    fileLatexBuilder.Insert(0, new Header(null, state).TransformText() + Environment.NewLine);

                    var finalLatex = fileLatexBuilder.ToString();
                    var fileName = Path.Combine(latexOutputDirectory, $"packet{packetIndex:0000}_message{state.StatsMessagesProcessed:0000}.tex");
                    File.WriteAllText(fileName, finalLatex);
                    fileLatexBuilder.Clear();

                    // Packet Header
                    fileLatexBuilder.AppendLine(new PacketHeader(packet.Messages, packet.IsFrontEnd, packetIndex, state).TransformText());
                });

                if (!success)
                {
                    state.StatsMessagesInvalid++;
                    fileLatexBuilder.AppendLine($"No message definition found for code '{pgMessage.GetType().Name}'");
                    fileLatexBuilder.AppendLine("\\\\");
                    logger.LogWarning("No message definition found for code '{MessageCode}'", pgMessage.GetType().Name);
                }
            }
            packetIndex++;
            state.StatsPacketsProcessed++;
        }


        return state;
    }

    bool ProcessPostgresMessage(PostgresMessageBase message, GenerationState state, StringBuilder latexBuilder, Action<StringBuilder, GenerationState>? messageReadyAction = null)
    {
        // check consecutive datarows
        // if max datarows reached, skip until the last and write a "n skipped rows" skippedwords
        if (state.LastMessage is DataRowMessage)
        {
            if (message is DataRowMessage)
            {
                state.ConsecutiveDataRows++;
                if (state.ConsecutiveDataRows >= LatexOptions.MaxDataRows)
                    return true;
            }
            else
            {
                if (state.ConsecutiveDataRows >= LatexOptions.MaxDataRows)
                {
                    // next message after n datarows
                    ITextTransformer skippedWordsTransformer = new SkippedWords("DataRow", skippedItems: state.ConsecutiveDataRows);

                    WriteTextTransformation(state, latexBuilder, skippedWordsTransformer);

                    // Send event (for multiple mode)
                    messageReadyAction?.Invoke(latexBuilder, state);
                }
                state.ConsecutiveDataRows = 0;
                // continue
            }
        }

        state.LastMessage = message;

        ITextTransformer? textTransformer = LatexOptions.CustomTemplateProvider?.Invoke(message) ?? FindTextTransformer(message);

        if (textTransformer == null)
        {
            latexBuilder.AppendLine($"No template found for '{message.GetType().Name}' \\\\");
            latexBuilder.AppendLine(new MessageSeparator().TransformText());
            return false;
        }

        textTransformer.Render = state.Render;
        WriteTextTransformation(state, latexBuilder, textTransformer);

        // Send event (for multiple mode)
        messageReadyAction?.Invoke(latexBuilder, state);

        return true;
    }

    static void WriteTextTransformation(GenerationState state, StringBuilder latexBuilder, ITextTransformer textTransformer)
    {
        var estimatedRowCount = textTransformer.EstimateBytefieldRowCount();
        latexBuilder.AppendLineIfDebug($"% row count: {state.LatexRowCount}, estimated next: {estimatedRowCount}, new row count: {state.LatexRowCount + estimatedRowCount} (max: {MaxLatexRowsPerPage})");
        state.LatexRowCount += estimatedRowCount;

        if (state.LatexRowCount > MaxLatexRowsPerPage && !state.Standalone)
        {
            latexBuilder.AppendLineIfDebug($"% page break. row count: {state.LatexRowCount}, max: {MaxLatexRowsPerPage}");
            latexBuilder.AppendLine(new PacketFooter(newChapter: true, state, "Conversation (continuation)").TransformText());
            state.LatexRowCount = 1; // new chapter takes 1 row vertical space
            latexBuilder.AppendLine(new PacketHeader(state).TransformText());
        }

        latexBuilder.AppendLine(textTransformer.TransformText());
        latexBuilder.AppendLine(new MessageSeparator().TransformText());
    }

    LatexRenderOptions ResolveDefaultRender() => new()
    {
        Exact = LatexOptions.DefaultExact,
        RowWidthBytes = LatexOptions.DefaultRowWidthBytes,
    };

    static ITextTransformer? FindTextTransformer(PostgresMessageBase message) => message switch
    {
        QueryMessage m => new Query(m),
        ParseMessage m => new Parse(m),
        DescribeMessage m => new Describe(m),
        SyncMessage _ => new Sync(),
        NoDataMessage _ => new NoData(),
        BindCompleteMessage _ => new BindComplete(),
        ParseCompleteMessage _ => new ParseComplete(),
        ParameterDescriptionMessage m => new ParameterDescription(m),
        RowDescriptionMessage m => new RowDescription(m),
        ReadyForQueryMessage m => new ReadyForQuery(m),
        BindMessage m => new Bind(m),
        ExecuteMessage m => new Execute(m),
        DataRowMessage m => new DataRow(m),
        CommandCompleteMessage m => new CommandComplete(m),
        NoticeResponseMessage m => new NoticeResponse(m),
        TerminateMessage _ => new Terminate(),
        SSLRequestMessage m => new SSLRequest(m),
        SSLResponseMessage m => new SSLResponse(m),
        GSSENCRequestMessage m => new GSSENCRequest(m),
        GSSENCResponseMessage m => new GSSENCResponse(m),
        CancelRequestMessage m => new CancelRequest(m),
        CopyInResponseMessage m => new CopyInResponse(m),
        CopyOutResponseMessage m => new CopyOutResponse(m),
        CopyBothResponseMessage m => new CopyBothResponse(m),
        CopyDataMessage m => new CopyData(m),
        CopyDoneMessage _ => new CopyDone(),
        CopyFailMessage m => new CopyFail(m),
        StartupMessageMessage m => new StartupMessage(m),
        AuthenticationGenericMessage m => new AuthenticationGeneric(m),
        SASLInitialResponseMessage m => new SASLInitialResponse(m),
        SASLResponseMessage m => new SASLResponse(m),
        ParameterStatusMessage m => new ParameterStatus(m),
        BackendKeyDataMessage m => new BackendKeyData(m),
        ErrorResponseMessage m => new ErrorResponse(m),
        UnknownMessage m => new Unknown(m),
        _ => null,
    };
}
