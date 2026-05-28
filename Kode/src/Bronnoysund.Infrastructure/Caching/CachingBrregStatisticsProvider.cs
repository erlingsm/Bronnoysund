// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Caching;

/// <summary>
/// HybridCache decorator around <see cref="IBrregStatisticsProvider"/>. Statistics like
/// <c>/roller/totalbestand</c> change continuously upstream but consumers (dashboard tiles,
/// landing pages) typically re-render at much higher frequency than the number is interesting.
/// A 2-minute TTL gives the same trade-off as the entity-changes feed: near-live data
/// without hammering Brreg on every page refresh.
/// </summary>
internal sealed class CachingBrregStatisticsProvider(
    IBrregStatisticsProvider inner,
    HybridCache cache,
    ILogger<CachingBrregStatisticsProvider> logger) : IBrregStatisticsProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    public async Task<RolesTotalCountResult> GetRolesTotalCountAsync(CancellationToken ct)
    {
        const string cacheKey = "statistics:roles-total-count";
        logger.LogDebug("Cache lookup for {Key}", cacheKey);

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => CachedRolesTotalCount.From(await inner.GetRolesTotalCountAsync(cancel)),
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);

        return cached.ToResult();
    }
}

/// <summary>
/// Cache-friendly flat representation of <see cref="RolesTotalCountResult"/>. <see cref="TotalCount"/>
/// non-null indicates Found; <see cref="UnavailableMessage"/> non-null indicates Unavailable.
/// The two states are mutually exclusive — <see cref="ToResult"/> throws on a malformed
/// payload as a runtime guard.
/// </summary>
public sealed record CachedRolesTotalCount(long? TotalCount, string? UnavailableMessage)
{
    public static CachedRolesTotalCount From(RolesTotalCountResult r) => r switch
    {
        RolesTotalCountResult.Found f => new CachedRolesTotalCount(f.TotalCount, null),
        RolesTotalCountResult.Unavailable u => new CachedRolesTotalCount(null, u.Message),
        _ => throw new InvalidOperationException($"Unknown RolesTotalCountResult: {r.GetType().Name}"),
    };

    public RolesTotalCountResult ToResult()
    {
        if (TotalCount is long total) return new RolesTotalCountResult.Found(total);
        if (UnavailableMessage is not null) return new RolesTotalCountResult.Unavailable(UnavailableMessage);
        throw new InvalidOperationException("CachedRolesTotalCount is empty — invalid state.");
    }
}
