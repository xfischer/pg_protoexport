using System.Runtime.InteropServices;

namespace pg_protoexport.tests;

public class CaptureLibraryTests
{
    [Fact]
    public void DownloadUrl_MatchesCurrentPlatform()
    {
        var expected =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "https://npcap.com/#download" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "https://formulae.brew.sh/formula/libpcap" :
            "https://www.tcpdump.org/";

        Assert.Equal(expected, CaptureLibrary.DownloadUrl);
    }

    [Fact]
    public void BuildMessage_MentionsTheRightLibraryForCurrentPlatform()
    {
        var message = CaptureLibrary.BuildMessage();

        Assert.False(string.IsNullOrWhiteSpace(message));
        var expectedLibrary = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Npcap" : "libpcap";
        Assert.Contains(expectedLibrary, message);
    }

    [Fact]
    public void CaptureLibraryMissingException_IsCaughtAsProtoexportException()
    {
        // Existing catch sites catch pg_protoexportException; the new typed exception
        // must remain assignable to it so those keep working.
        var ex = new CaptureLibraryMissingException("msg", "https://example.com", new DllNotFoundException());

        Assert.IsAssignableFrom<pg_protoexportException>(ex);
        Assert.Equal("https://example.com", ex.DownloadUrl);
    }
}
