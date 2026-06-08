using Microsoft.Extensions.Logging;

namespace pg_protoexport;

internal sealed class LiveCaptureSessionFactory(ILoggerFactory loggerFactory) : ILiveCaptureSessionFactory
{
    public Task<LiveCaptureSession> StartAsync(LiveCaptureOptions options, CancellationToken cancellationToken = default)
        => LiveCaptureSession.StartAsync(options, loggerFactory, cancellationToken);
}
