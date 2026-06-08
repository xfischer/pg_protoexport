namespace pg_protoexport.tests;

public class CancelCorrelatorTests
{
    [Fact]
    public void RegisterThenLookup_RoundTripsClientPort()
    {
        var state = new PcapReadState();
        state.RegisterCancelKey(processId: 12345, secretKey: 0xCAFEBABE, clientPort: 57480);

        var found = state.LookupCancelTargetClientPort(12345, 0xCAFEBABE);
        Assert.Equal((ushort)57480, found);
    }

    [Fact]
    public void Lookup_ReturnsNull_WhenKeyUnknown()
    {
        var state = new PcapReadState();
        Assert.Null(state.LookupCancelTargetClientPort(processId: 1, secretKey: 1));
    }

    [Fact]
    public void Lookup_DistinguishesSamePidDifferentSecrets()
    {
        var state = new PcapReadState();
        state.RegisterCancelKey(processId: 1000, secretKey: 0x11111111, clientPort: 50001);
        state.RegisterCancelKey(processId: 1000, secretKey: 0x22222222, clientPort: 50002);

        Assert.Equal((ushort)50001, state.LookupCancelTargetClientPort(1000, 0x11111111));
        Assert.Equal((ushort)50002, state.LookupCancelTargetClientPort(1000, 0x22222222));
    }
}
