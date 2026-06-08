using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace pg_protoexport;

public class CaptureCommand(ILiveCaptureSessionFactory factory, ILogger<CaptureCommand> logger)
    : AsyncCommand<CaptureSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context, CaptureSettings settings, CancellationToken cancellation)
    {
        if (settings.ListDevices)
        {
            foreach (var dev in PcapDevicePicker.Enumerate())
            {
                AnsiConsole.MarkupLine($"[bold]{Markup.Escape(dev.Name)}[/]  {Markup.Escape(dev.Description)}");
                foreach (var addr in dev.Addresses)
                    AnsiConsole.MarkupLine($"    [dim]{Markup.Escape(addr)}[/]");
            }
            return 0;
        }

        var options = new LiveCaptureOptions(settings.OutputPath!)
        {
            Host = settings.Host,
            Port = settings.Port,
            DeviceName = settings.Device,
            EchoPackets = !settings.Quiet,
        };

        await using var session = await factory.StartAsync(options, cancellation);

        if (settings.Duration is { } d)
        {
            logger.LogInformation("capturing for {Duration}; Ctrl+C to stop early", d);
            try { await Task.Delay(d, cancellation); }
            catch (OperationCanceledException) { }
        }
        else
        {
            logger.LogInformation("capturing until Ctrl+C...");
            try { await Task.Delay(Timeout.Infinite, cancellation); }
            catch (OperationCanceledException) { }
        }

        return 0;
    }
}
