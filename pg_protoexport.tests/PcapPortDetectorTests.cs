using Microsoft.Extensions.Logging.Abstractions;

namespace pg_protoexport.tests;

public class PcapPortDetectorTests
{
    private static PcapPortDetector NewDetector() =>
        new PcapPortDetector(NullLogger<PcapPortDetector>.Instance);

    [Fact]
    public void Detect_ExtendedQueryCapture_Returns5432()
    {
        // TestData/extendedQuery.pcapng is captured against a Postgres on the default port.
        var detector = NewDetector();
        var port = detector.Detect("TestData/extendedQuery.pcapng");
        Assert.Equal(5432, port);
    }

    [Fact]
    public void Detect_PagilaCapture_Returns5434()
    {
        // The pagila example captures were taken against the user's local pagila DB on port 5434.
        // Any one of the 14 .pcapng files should yield 5434 (some via SYN handshake, others via
        // packet-count majority for mid-conversation captures).
        var detector = NewDetector();
        var port = detector.Detect("../../../../docs/examples/captures/pagila-01-simple-query-single-statement.pcapng");
        Assert.Equal(5434, port);
    }

    [Fact]
    public void Detect_PagilaStartupHandshake_Returns5434FromSyn()
    {
        // pagila-00 contains the full TCP SYN handshake. Should detect via SYN unambiguously.
        var detector = NewDetector();
        var port = detector.Detect("../../../../docs/examples/captures/pagila-00-startup-handshake-startup-authentication-parameterstatus-readyforquery.pcapng");
        Assert.Equal(5434, port);
    }

    [Fact]
    public void Detect_MissingFile_Throws()
    {
        var detector = NewDetector();
        Assert.Throws<FileNotFoundException>(() => detector.Detect("does-not-exist.pcapng"));
    }

    [Fact]
    public void Detect_EmptyPath_Throws()
    {
        var detector = NewDetector();
        Assert.Throws<ArgumentException>(() => detector.Detect(""));
    }
}
