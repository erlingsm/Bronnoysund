// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Infrastructure.Persistence.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Persistence.Maintenance;

/// <summary>
/// Runs HistoryCleanupService + CacheEvictionService periodically (default every 6 hours).
/// Also runs once at startup to clean up from the previous run. Does not need to be continuous
/// in MAUI processes; hosted services only run as long as IHost is active, which is the entire app lifetime.
/// </summary>
internal sealed class PersistenceMaintenanceHostedService(
    IServiceScopeFactory scopes,
    IOptionsMonitor<PersistenceOptions> opts,
    ILogger<PersistenceMaintenanceHostedService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = opts.CurrentValue.Maintenance.Interval;
        try
        {
            await RunOnceAsync(stoppingToken);
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            log.LogError(ex, "Persistens-vedlikehold krasjet — hosted service stopper");
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var history = scope.ServiceProvider.GetRequiredService<HistoryCleanupService>();
        var eviction = scope.ServiceProvider.GetRequiredService<CacheEvictionService>();

        await history.CleanupAsync(ct);
        await eviction.EvictIfNeededAsync(ct);
    }
}
