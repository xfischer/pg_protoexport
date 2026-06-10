namespace pg_protoexport;

public sealed record LiveCaptureOptions(string OutputFile)
{
    public string Host { get; init; } = "localhost";
    public ushort Port { get; init; } = 5432;

    public string? DeviceName { get; init; }

    public int ReadTimeoutMs { get; init; } = 250;
    public bool Promiscuous { get; init; } = false;

    public string? BpfFilter { get; init; }

    public bool EchoPackets { get; init; } = false;
}
