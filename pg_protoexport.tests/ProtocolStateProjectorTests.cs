using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport.tests;

public class ProtocolStateProjectorTests
{
    private static IEnumerable<PostgresPacket> ConvertSample()
    {
        var pcapOptions = new PcapPostgresOptions();
        pcapOptions.AddDefaultPostgresMessages();
        var pcapService = new PcapService(NullLogger<PcapService>.Instance, Options.Create(pcapOptions));
        return pcapService.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432);
    }

    [Fact]
    public void Project_EmitsOneSnapshotPerMessage()
    {
        // arrange
        var packets = ConvertSample().ToList();
        int expected = packets.Sum(p => p.Messages.Count);

        // act
        var snapshots = ProtocolStateProjector.Project(packets).ToList();

        // assert
        Assert.Equal(expected, snapshots.Count);
    }

    [Fact]
    public void Project_TerminatesWithTerminatedConnState()
    {
        // arrange
        var packets = ConvertSample().ToList();

        // act
        var last = ProtocolStateProjector.Project(packets).Last();

        // assert — the trace ends with a Terminate message
        Assert.IsType<TerminateMessage>(last.Message);
        Assert.Equal(ConnectionState.Terminated, last.Snapshot.ConnState);
    }

    [Fact]
    public void Project_RecordsPreparedStatementAfterParse()
    {
        // arrange
        var packets = ConvertSample().ToList();

        // act
        var afterParse = ProtocolStateProjector.Project(packets)
            .FirstOrDefault(t => t.Message is ParseMessage);

        // assert
        Assert.NotNull(afterParse.Snapshot);
        Assert.NotEmpty(afterParse.Snapshot.Prepared);
    }

    [Fact]
    public void Project_SetsTransactionStatusFromReadyForQuery()
    {
        // arrange
        var packets = ConvertSample().ToList();

        // act
        var afterRfq = ProtocolStateProjector.Project(packets)
            .First(t => t.Message is ReadyForQueryMessage);

        // assert — extendedQuery sample ends a batch with ReadyForQuery(Idle)
        Assert.Equal(ConnectionState.Ready, afterRfq.Snapshot.ConnState);
        Assert.Equal(TransactionStatus.Idle, afterRfq.Snapshot.TxStatus);
    }

    [Fact]
    public void Project_EmptyPacketSequence_YieldsNoSnapshots()
    {
        // act
        var snapshots = ProtocolStateProjector.Project([]).ToList();

        // assert
        Assert.Empty(snapshots);
    }

    [Fact]
    public void Project_AuthenticationOk_AdvancesToAuthenticated()
    {
        // arrange — a successful authentication completes the auth phase
        var authOk = new AuthenticationGenericMessage(
            new('R', "AuthenticationRequest", IsFrontEnd: false), length: 8, data: 0, commonName: "AuthenticationOK");
        var packet = new PostgresPacket { Messages = { authOk } };

        // act
        var snapshot = ProtocolStateProjector.Project([packet]).Single().Snapshot;

        // assert
        Assert.Equal(ConnectionState.Authenticated, snapshot.ConnState);
    }

    [Fact]
    public void Project_AuthenticationChallenge_StaysAuthenticating()
    {
        // arrange — any non-OK Authentication* message is an in-progress challenge
        var challenge = new AuthenticationGenericMessage(
            new('R', "AuthenticationRequest", IsFrontEnd: false), length: 12, data: 5, commonName: "AuthenticationMD5Password");
        var packet = new PostgresPacket { Messages = { challenge } };

        // act
        var snapshot = ProtocolStateProjector.Project([packet]).Single().Snapshot;

        // assert
        Assert.Equal(ConnectionState.Authenticating, snapshot.ConnState);
    }
}
