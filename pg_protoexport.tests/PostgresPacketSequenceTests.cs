using System.Net;

namespace pg_protoexport.tests;

public class PostgresPacketSequenceTests
{
    static readonly PostgresMessageDescriptor QueryDesc = new('Q', "Query", IsFrontEnd: true);
    static readonly PostgresMessageDescriptor SyncDesc = new('S', "Sync", IsFrontEnd: true);
    static readonly PostgresMessageDescriptor TerminateDesc = new('X', "Terminate", IsFrontEnd: true);
    static readonly PostgresMessageDescriptor ReadyDesc = new('Z', "ReadyForQuery", IsFrontEnd: false);
    static readonly PostgresMessageDescriptor RowDescDesc = new('T', "RowDescription", IsFrontEnd: false);
    static readonly PostgresMessageDescriptor DataRowDesc = new('D', "DataRow", IsFrontEnd: false);
    static readonly PostgresMessageDescriptor CmdCompleteDesc = new('C', "CommandComplete", IsFrontEnd: false);

    static PostgresPacket PacketOf(params PostgresMessageBase[] messages) =>
        new() { Messages = [.. messages] };

    // ── GroupMessages ────────────────────────────────────────────────────

    [Fact]
    public void GroupMessages_EmptyPacket_ReturnsEmpty()
    {
        var packet = PacketOf();
        var groups = PostgresPacketSequence.GroupMessages(packet);
        Assert.Empty(groups);
    }

    [Fact]
    public void GroupMessages_SingleMessage_ReturnsOneGroupWithCountOne()
    {
        var packet = PacketOf(new QueryMessage(QueryDesc, 5));

        var groups = PostgresPacketSequence.GroupMessages(packet);

        Assert.Single(groups);
        Assert.Equal('Q', groups[0].Message.Code);
        Assert.Equal(1, groups[0].Count);
    }

    [Fact]
    public void GroupMessages_CollapsesConsecutiveSameCodeMessages()
    {
        var packet = PacketOf(
            new DataRowMessage(DataRowDesc, 5),
            new DataRowMessage(DataRowDesc, 5),
            new DataRowMessage(DataRowDesc, 5));

        var groups = PostgresPacketSequence.GroupMessages(packet);

        Assert.Single(groups);
        Assert.Equal('D', groups[0].Message.Code);
        Assert.Equal(3, groups[0].Count);
    }

    [Fact]
    public void GroupMessages_KeepsNonConsecutiveSameCodeMessagesSeparate()
    {
        var packet = PacketOf(
            new DataRowMessage(DataRowDesc, 5),
            new CommandCompleteMessage(CmdCompleteDesc, 5),
            new DataRowMessage(DataRowDesc, 5));

        var groups = PostgresPacketSequence.GroupMessages(packet);

        Assert.Equal(3, groups.Count);
        Assert.Equal('D', groups[0].Message.Code);
        Assert.Equal(1, groups[0].Count);
        Assert.Equal('C', groups[1].Message.Code);
        Assert.Equal('D', groups[2].Message.Code);
    }

    // ── BuildSequenceLines ───────────────────────────────────────────────

    [Fact]
    public void BuildSequenceLines_MergesSameDirectionSinglesWithSlash()
    {
        // Three consecutive front-end single-count messages -> one merged line.
        var packet = PacketOf(
            new ParseMessage(new('P', "Parse", IsFrontEnd: true), 5),
            new BindMessage(new('B', "Bind", IsFrontEnd: true), 5),
            new SyncMessage(SyncDesc, 5));

        var lines = PostgresPacketSequence.BuildSequenceLines(packet);

        Assert.Single(lines);
        Assert.True(lines[0].FrontEnd);
        Assert.Equal("Parse / Bind / Sync", lines[0].Label);
        Assert.False(lines[0].ClosesDiagram);
    }

    [Fact]
    public void BuildSequenceLines_SplitsOnDirectionChange()
    {
        var packet = PacketOf(
            new QueryMessage(QueryDesc, 5),
            new RowDescriptionMessage(RowDescDesc, 5));

        var lines = PostgresPacketSequence.BuildSequenceLines(packet);

        Assert.Equal(2, lines.Count);
        Assert.True(lines[0].FrontEnd);
        Assert.Equal("Query", lines[0].Label);
        Assert.False(lines[1].FrontEnd);
        Assert.Equal("RowDescription", lines[1].Label);
    }

    [Fact]
    public void BuildSequenceLines_RepeatedMessagesGetOwnLineWithCountSuffix()
    {
        var packet = PacketOf(
            new DataRowMessage(DataRowDesc, 5),
            new DataRowMessage(DataRowDesc, 5),
            new DataRowMessage(DataRowDesc, 5));

        var lines = PostgresPacketSequence.BuildSequenceLines(packet);

        Assert.Single(lines);
        Assert.Equal("DataRow (x3)", lines[0].Label);
        Assert.False(lines[0].ClosesDiagram);
    }

    [Fact]
    public void BuildSequenceLines_RepeatedMessageBreaksMerge()
    {
        // Single Query, then three DataRows: the run cannot merge through the repeat.
        var packet = PacketOf(
            new RowDescriptionMessage(RowDescDesc, 5),
            new DataRowMessage(DataRowDesc, 5),
            new DataRowMessage(DataRowDesc, 5),
            new CommandCompleteMessage(CmdCompleteDesc, 5));

        var lines = PostgresPacketSequence.BuildSequenceLines(packet);

        Assert.Equal(3, lines.Count);
        Assert.Equal("RowDescription", lines[0].Label);
        Assert.Equal("DataRow (x2)", lines[1].Label);
        Assert.Equal("CommandComplete", lines[2].Label);
    }

    [Fact]
    public void BuildSequenceLines_ReadyForQueryMarksClosesDiagram()
    {
        var packet = PacketOf(
            new CommandCompleteMessage(CmdCompleteDesc, 5),
            new ReadyForQueryMessage(ReadyDesc, 5));

        var lines = PostgresPacketSequence.BuildSequenceLines(packet);

        Assert.Equal(2, lines.Count);
        Assert.Equal("CommandComplete", lines[0].Label);
        Assert.False(lines[0].ClosesDiagram);
        Assert.Equal("ReadyForQuery", lines[1].Label);
        Assert.True(lines[1].ClosesDiagram);
    }

    [Fact]
    public void BuildSequenceLines_TerminateMarksClosesDiagram()
    {
        var packet = PacketOf(new TerminateMessage(TerminateDesc, 5));

        var lines = PostgresPacketSequence.BuildSequenceLines(packet);

        Assert.Single(lines);
        Assert.Equal("Terminate", lines[0].Label);
        Assert.True(lines[0].ClosesDiagram);
    }

    [Fact]
    public void BuildSequenceLines_BoundaryDoesNotMergeIntoPriorRun()
    {
        // Sync (front-end, mergeable) followed by Terminate (front-end, boundary)
        // -> Sync stays on its own line, Terminate flagged as boundary.
        var packet = PacketOf(
            new SyncMessage(SyncDesc, 5),
            new TerminateMessage(TerminateDesc, 5));

        var lines = PostgresPacketSequence.BuildSequenceLines(packet);

        Assert.Equal(2, lines.Count);
        Assert.Equal("Sync", lines[0].Label);
        Assert.False(lines[0].ClosesDiagram);
        Assert.Equal("Terminate", lines[1].Label);
        Assert.True(lines[1].ClosesDiagram);
    }

    // ── SessionEndpoints ─────────────────────────────────────────────────

    [Fact]
    public void SessionEndpoints_FromFrontEndPacket_MapsSourceToClient()
    {
        var packet = new PostgresPacket
        {
            IsFrontEnd = true,
            SourceAddress = IPAddress.Parse("10.0.0.1"),
            SourcePort = 1234,
            DestinationAddress = IPAddress.Parse("10.0.0.2"),
            DestinationPort = 5432,
        };

        var endpoints = SessionEndpoints.FromFirstPacket(packet);

        Assert.Equal(IPAddress.Parse("10.0.0.1"), endpoints.Client);
        Assert.Equal(1234, endpoints.ClientPort);
        Assert.Equal(IPAddress.Parse("10.0.0.2"), endpoints.Server);
        Assert.Equal(5432, endpoints.ServerPort);
    }

    [Fact]
    public void SessionEndpoints_FromBackEndPacket_MapsSourceToServer()
    {
        var packet = new PostgresPacket
        {
            IsFrontEnd = false,
            SourceAddress = IPAddress.Parse("10.0.0.2"),
            SourcePort = 5432,
            DestinationAddress = IPAddress.Parse("10.0.0.1"),
            DestinationPort = 1234,
        };

        var endpoints = SessionEndpoints.FromFirstPacket(packet);

        Assert.Equal(IPAddress.Parse("10.0.0.1"), endpoints.Client);
        Assert.Equal(1234, endpoints.ClientPort);
        Assert.Equal(IPAddress.Parse("10.0.0.2"), endpoints.Server);
        Assert.Equal(5432, endpoints.ServerPort);
    }
}
