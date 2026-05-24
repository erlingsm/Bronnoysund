// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Maintenance;

/// <summary>
/// Kjører HistoryCleanupService + CacheEvictionService periodisk (default hver 6. time).
/// Også én gang ved oppstart for å rydde fra forrige kjøring. Trenger ikke å være kontinuerlig
/// i MAUI-prosesser; hosted services kjører bare så lenge IHost er aktiv, som er hele app-livet.
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
