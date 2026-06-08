using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport.tests;

public class PcapServiceTests
{
    [Fact]
    public void ExtractPcapPostgres_ExtendedQuery()
    {
        // arrange
        var options = new PcapPostgresOptions();
        options.AddDefaultPostgresMessages();
        var service = new PcapService(NullLogger<PcapService>.Instance, Options.Create(options));

        // act
        var packets = service.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432).ToList();

        // assert
        Assert.NotEmpty(packets);
        Assert.Equal(3, packets.Count);

        Assert.Equal(5, packets[0].Messages.Count);
        Assert.Equal(15, packets[1].Messages.Count);
        Assert.Single(packets[2].Messages);

        Assert.IsType<ParseMessage>(packets[0].Messages[0]);
        Assert.IsType<BindMessage>(packets[0].Messages[1]);
        Assert.IsType<DescribeMessage>(packets[0].Messages[2]);
        Assert.IsType<ExecuteMessage>(packets[0].Messages[3]);
        Assert.IsType<SyncMessage>(packets[0].Messages[4]);

        Assert.IsType<ParseCompleteMessage>(packets[1].Messages[0]);
        Assert.IsType<BindCompleteMessage>(packets[1].Messages[1]);
        Assert.IsType<RowDescriptionMessage>(packets[1].Messages[2]);
        for (int i = 0; i< 10; i++)
            Assert.IsType<DataRowMessage>(packets[1].Messages[3+i]);

        Assert.IsType<CommandCompleteMessage>(packets[1].Messages[13]);
        Assert.IsType<ReadyForQueryMessage>(packets[1].Messages[14]);


        Assert.IsType<TerminateMessage>(packets[2].Messages[0]);
    }

    [Fact]
    public void ExtractPcapPostgres_ExtendedQuery_BadPort()
    {
        // arrange
        var options = new PcapPostgresOptions();
        options.AddDefaultPostgresMessages();
        PcapService service = new PcapService(NullLogger<PcapService>.Instance, Options.Create(options));

        // act
        var packets = service.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 1000).ToList();

        // assert
        Assert.Empty(packets);
    }
}
