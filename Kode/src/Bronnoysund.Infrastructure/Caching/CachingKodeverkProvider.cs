// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Caching;

/// <summary>
/// HybridCache decorator around <see cref="IKodeverkProvider"/>. Kodeverk-tables change
/// rarely (ICNPO is updated roughly yearly, Informasjonstyper is essentially constant,
/// Organisasjonsformer drifts at low frequency) so a 24-hour TTL is appropriate. Cache
/// keys are static per endpoint because the methods take no input parameters.
/// </summary>
internal sealed class CachingKodeverkProvider(
    IKodeverkProvider inner,
    HybridCache cache,
    ILogger<CachingKodeverkProvider> logger) : IKodeverkProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    public async Task<KodeverkLookupResult> GetOrganisasjonsformerAsync(CancellationToken ct)
    {
        const string cacheKey = "kodeverk:organisasjonsformer";
        logger.LogDebug("Cache lookup for {Key}", cacheKey);

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => CachedKodeverk.From(await inner.GetOrganisasjonsformerAsync(cancel)),
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);

        return cached.ToResult();
    }

    public async Task<KodeverkLookupResult> GetIcnpoCategoriesAsync(CancellationToken ct)
    {
        const string cacheKey = "kodeverk:icnpo-kategorier";
        logger.LogDebug("Cache lookup for {Key}", cacheKey);

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => CachedKodeverk.From(await inner.GetIcnpoCategoriesAsync(cancel)),
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);

        return cached.ToResult();
    }

    public async Task<KodeverkLookupResult> GetVoluntaryInformationTypesAsync(CancellationToken ct)
    {
        const string cacheKey = "kodeverk:informasjonstyper";
        logger.LogDebug("Cache lookup for {Key}", cacheKey);

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => CachedKodeverk.From(await inner.GetVoluntaryInformationTypesAsync(cancel)),
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);

        return cached.ToResult();
    }
}

/// <summary>Cache-friendly flat representation of <see cref="KodeverkLookupResult"/>.</summary>
public sealed record CachedKodeverk(
    IReadOnlyList<KodeverkEntry>? Entries,
    string? UnavailableMessage)
{
    public static CachedKodeverk From(KodeverkLookupResult r) => r switch
    {
        KodeverkLookupResult.Found f => new CachedKodeverk(f.Entries, null),
        KodeverkLookupResult.Unavailable u => new CachedKodeverk(null, u.Message),
        _ => throw new InvalidOperationException($"Unknown KodeverkLookupResult: {r.GetType().Name}"),
    };

    public KodeverkLookupResult ToResult()
    {
        if (Entries is not null) return new KodeverkLookupResult.Found(Entries);
        if (UnavailableMessage is not null) return new KodeverkLookupResult.Unavailable(UnavailableMessage);
        throw new InvalidOperationException("CachedKodeverk is empty — invalid state.");
    }
}
