namespace pg_protoexport;

public interface ILiveCaptureSessionFactory
{
    Task<LiveCaptureSession> StartAsync(LiveCaptureOptions options, CancellationToken cancellationToken = default);
}
