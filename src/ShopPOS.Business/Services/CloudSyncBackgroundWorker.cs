using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public class CloudSyncBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly CloudSyncOptions _options;
    private readonly ILogger<CloudSyncBackgroundWorker>? _logger;

    public CloudSyncBackgroundWorker(
        IServiceProvider services,
        CloudSyncOptions options,
        ILogger<CloudSyncBackgroundWorker>? logger = null)
    {
        _services = services;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunSyncIfEnabledAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = Math.Max(1, _options.IntervalMinutes);
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunSyncIfEnabledAsync(stoppingToken);
        }
    }

    private async Task RunSyncIfEnabledAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        try
        {
            using var scope = _services.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<ICloudSyncService>();
            await sync.SyncAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Background cloud sync tick failed");
        }
    }
}
