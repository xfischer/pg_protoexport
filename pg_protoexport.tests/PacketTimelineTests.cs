namespace pg_protoexport.tests;

public class PacketTimelineTests
{
    [Fact]
    public void Advance_FirstCall_ReturnsIsFirst_NoDeltaOrTotal()
    {
        var tracker = new PacketTimelineTracker();
        var current = new DateTime(2026, 1, 1, 12, 0, 0, 500);

        var (isFirst, absolute, delta, total) = tracker.Advance(current);

        Assert.True(isFirst);
        Assert.Null(delta);
        Assert.Null(total);
        Assert.Equal("12:00:00.500000", absolute);
    }

    [Fact]
    public void Advance_SecondCall_ReturnsDeltaSincePrevious_AndTotalSinceFirst()
    {
        var tracker = new PacketTimelineTracker();
        var first = new DateTime(2026, 1, 1, 12, 0, 0, 500);
        tracker.Advance(first);

        var (isFirst, _, delta, total) = tracker.Advance(first.AddMilliseconds(42));

        Assert.False(isFirst);
        Assert.Equal(TimeSpan.FromMilliseconds(42), delta);
        Assert.Equal(TimeSpan.FromMilliseconds(42), total);
    }

    [Fact]
    public void Advance_ThirdCall_DeltaIsSincePreviousCall_TotalIsSinceFirstCall()
    {
        var tracker = new PacketTimelineTracker();
        var first = new DateTime(2026, 1, 1, 12, 0, 0, 0);
        tracker.Advance(first);
        tracker.Advance(first.AddMilliseconds(10));

        var (_, _, delta, total) = tracker.Advance(first.AddMilliseconds(30));

        Assert.Equal(TimeSpan.FromMilliseconds(20), delta);
        Assert.Equal(TimeSpan.FromMilliseconds(30), total);
    }

    [Fact]
    public void DeltaMicroseconds_TruncatesToWholeMicroseconds()
    {
        Assert.Equal(0, PacketTimeline.DeltaMicroseconds(TimeSpan.FromTicks(9)));
        Assert.Equal(1_500_000, PacketTimeline.DeltaMicroseconds(TimeSpan.FromSeconds(1.5)));
        Assert.Equal(312, PacketTimeline.DeltaMicroseconds(TimeSpan.FromTicks(3120)));
    }

    [Fact]
    public void FormatDelta_RendersPlainTextMicroseconds()
    {
        Assert.Equal("+42000 µs", PacketTimeline.FormatDelta(TimeSpan.FromMilliseconds(42)));
        Assert.Equal("+312 µs", PacketTimeline.FormatDelta(TimeSpan.FromTicks(3120)));
    }
}
