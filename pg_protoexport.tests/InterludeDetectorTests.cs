namespace pg_protoexport.tests;

public class InterludeDetectorTests
{
    private static readonly InterludeCatalog FullCatalog = new(new Dictionary<string, InterludeEntry>
    {
        ["startup"] = new("Startup", "..."),
        ["scram_exchange"] = new("SCRAM", "..."),
        ["extended_query_batch"] = new("Pipelining", "..."),
        ["simple_query_cycle"] = new("Simple cycle", "..."),
        ["error_recovery"] = new("Error recovery", "..."),
    });

    private static PostgresMessageDescriptor Fe(char code, string name) => new(code, name, IsFrontEnd: true);
    private static PostgresMessageDescriptor Be(char code, string name) => new(code, name, IsFrontEnd: false);

    [Fact]
    public void Detect_EmptyMessageList_ReturnsEmpty()
    {
        var result = InterludeDetector.Detect(Array.Empty<PostgresMessageBase>(), FullCatalog);
        Assert.Empty(result);
    }

    [Fact]
    public void Detect_FirstQuery_EmitsSimpleQueryCycleAtItsIndex()
    {
        var messages = new List<PostgresMessageBase>
        {
            new QueryMessage(Fe('Q', "Query"), 10),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        var simple = Assert.Single(result, i => i.PatternId == "simple_query_cycle");
        Assert.Equal(0, simple.InsertBeforeCardIdx);
    }

    [Fact]
    public void Detect_ParseBindSync_EmitsExtendedQueryBatchAtParse()
    {
        var messages = new List<PostgresMessageBase>
        {
            new ParseMessage(Fe('P', "Parse"), 10),
            new BindMessage(Fe('B', "Bind"), 10),
            new SyncMessage(Fe('S', "Sync"), 4),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        var batch = Assert.Single(result, i => i.PatternId == "extended_query_batch");
        Assert.Equal(0, batch.InsertBeforeCardIdx);
    }

    [Fact]
    public void Detect_LoneParseWithoutFollowupFrontendBatchMember_DoesNotEmitBatch()
    {
        var messages = new List<PostgresMessageBase>
        {
            new ParseMessage(Fe('P', "Parse"), 10),
            new ErrorResponseMessage(Be('E', "ErrorResponse"), 20),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        Assert.DoesNotContain(result, i => i.PatternId == "extended_query_batch");
    }

    [Fact]
    public void Detect_StartupThenAuthOkThenReadyForQuery_EmitsStartupAfterRfq()
    {
        var messages = new List<PostgresMessageBase>
        {
            new StartupMessageMessage(Fe('?', "StartupMessage"), 30),
            new AuthenticationGenericMessage(Be('R', "AuthenticationRequest"), 8, 0, "AuthenticationOK"),
            new ReadyForQueryMessage(Be('Z', "ReadyForQuery"), 5),
            new QueryMessage(Fe('Q', "Query"), 10),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        var startup = Assert.Single(result, i => i.PatternId == "startup");
        Assert.Equal(3, startup.InsertBeforeCardIdx);
    }

    [Fact]
    public void Detect_ReadyForQueryWithoutPriorStartupEvidence_DoesNotEmitStartup()
    {
        // Regression: a partial capture that starts mid-session should not emit the
        // startup interlude just because it contains an end-of-batch ReadyForQuery.
        var messages = new List<PostgresMessageBase>
        {
            new ParseMessage(Fe('P', "Parse"), 10),
            new BindMessage(Fe('B', "Bind"), 10),
            new SyncMessage(Fe('S', "Sync"), 4),
            new ReadyForQueryMessage(Be('Z', "ReadyForQuery"), 5),
            new QueryMessage(Fe('Q', "Query"), 10),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        Assert.DoesNotContain(result, i => i.PatternId == "startup");
    }

    [Fact]
    public void Detect_StartupThenReadyForQueryIsLastMessage_DoesNotEmitStartup()
    {
        var messages = new List<PostgresMessageBase>
        {
            new StartupMessageMessage(Fe('?', "StartupMessage"), 30),
            new ReadyForQueryMessage(Be('Z', "ReadyForQuery"), 5),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        Assert.DoesNotContain(result, i => i.PatternId == "startup");
    }

    [Fact]
    public void Detect_SaslThenAuthenticationOk_EmitsScramExchange()
    {
        var messages = new List<PostgresMessageBase>
        {
            new AuthenticationSASLMessage(Be('R', "AuthenticationRequest"), 20, 10),
            new AuthenticationGenericMessage(Be('R', "AuthenticationRequest"), 8, 0, "AuthenticationOK"),
            new ReadyForQueryMessage(Be('Z', "ReadyForQuery"), 5),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        Assert.Contains(result, i => i.PatternId == "scram_exchange");
    }

    [Fact]
    public void Detect_AuthenticationOkWithoutPrecedingSasl_DoesNotEmitScram()
    {
        var messages = new List<PostgresMessageBase>
        {
            new AuthenticationGenericMessage(Be('R', "AuthenticationRequest"), 8, 0, "AuthenticationOK"),
            new ReadyForQueryMessage(Be('Z', "ReadyForQuery"), 5),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        Assert.DoesNotContain(result, i => i.PatternId == "scram_exchange");
    }

    [Fact]
    public void Detect_ErrorResponse_EmitsErrorRecoveryAtItsIndex()
    {
        var messages = new List<PostgresMessageBase>
        {
            new QueryMessage(Fe('Q', "Query"), 10),
            new ErrorResponseMessage(Be('E', "ErrorResponse"), 50),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        var err = Assert.Single(result, i => i.PatternId == "error_recovery");
        Assert.Equal(1, err.InsertBeforeCardIdx);
    }

    [Fact]
    public void Detect_CatalogMissingPattern_DoesNotEmitInterlude()
    {
        var emptyCatalog = InterludeCatalog.Empty;
        var messages = new List<PostgresMessageBase>
        {
            new QueryMessage(Fe('Q', "Query"), 10),
        };

        var result = InterludeDetector.Detect(messages, emptyCatalog);

        Assert.Empty(result);
    }

    [Fact]
    public void Detect_MultiplePatterns_SortedByInsertBeforeCardIdx()
    {
        var messages = new List<PostgresMessageBase>
        {
            new QueryMessage(Fe('Q', "Query"), 10),
            new ReadyForQueryMessage(Be('Z', "ReadyForQuery"), 5),
            new ErrorResponseMessage(Be('E', "ErrorResponse"), 20),
        };

        var result = InterludeDetector.Detect(messages, FullCatalog);

        for (int i = 1; i < result.Count; i++)
            Assert.True(result[i].InsertBeforeCardIdx >= result[i - 1].InsertBeforeCardIdx);
    }
}
