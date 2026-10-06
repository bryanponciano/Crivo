using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Crive.Agent.Protection;

public class WatchdogService : BackgroundService
{
    private readonly ILogger<WatchdogService> _logger;

    public WatchdogService(ILogger<WatchdogService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Check health of primary components
            await Task.Delay(5000, stoppingToken);
        }
    }
}
