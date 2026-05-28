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

    public Task<KodeverkLookupResult> GetOrganisasjonsformerAsync(CancellationToken ct) =>
        CachedListAsync("kodeverk:organisasjonsformer", inner.GetOrganisasjonsformerAsync, ct);

    public Task<KodeverkLookupResult> GetIcnpoCategoriesAsync(CancellationToken ct) =>
        CachedListAsync("kodeverk:icnpo-kategorier", inner.GetIcnpoCategoriesAsync, ct);

    public Task<KodeverkLookupResult> GetVoluntaryInformationTypesAsync(CancellationToken ct) =>
        CachedListAsync("kodeverk:informasjonstyper", inner.GetVoluntaryInformationTypesAsync, ct);

    // Kommuner is paged so each (page, size) is its own cache entry. ~52 entries for
    // page-size 100 across all ~5200 entries; cache-fills walk the whole catalogue once
    // per 24h.
    public Task<KodeverkPagedResult> GetKommunerAsync(int page, int size, CancellationToken ct) =>
        CachedPagedAsync(
            $"kodeverk:kommuner:p{page}:s{size}",
            cancel => inner.GetKommunerAsync(page, size, cancel),
            ct);

    public Task<KodeverkSingleResult> GetKommuneAsync(string kommunenummer, CancellationToken ct) =>
        CachedSingleAsync(
            $"kodeverk:kommune:{kommunenummer}",
            cancel => inner.GetKommuneAsync(kommunenummer, cancel),
            ct);

    public Task<KodeverkLookupResult> GetRolletyperAsync(CancellationToken ct) =>
        CachedListAsync("kodeverk:rolletyper", inner.GetRolletyperAsync, ct);

    public Task<KodeverkLookupResult> GetRollegruppetyperAsync(CancellationToken ct) =>
        CachedListAsync("kodeverk:rollegruppetyper", inner.GetRollegruppetyperAsync, ct);

    public Task<KodeverkLookupResult> GetRepresentanterAsync(CancellationToken ct) =>
        CachedListAsync("kodeverk:representanter", inner.GetRepresentanterAsync, ct);

    // H6: "organisasjonsform-detalj" (singular + "-detalj" suffix) is unambiguously distinct
    // from any "kodeverk:organisasjonsformer:*" prefix-key, so a future
    // InvalidateByPrefix("kodeverk:organisasjonsformer") would not accidentally evict
    // single-entity cache entries.
    public Task<KodeverkSingleResult> GetOrganisasjonsformAsync(string kode, CancellationToken ct) =>
        CachedSingleAsync(
            $"kodeverk:organisasjonsform-detalj:{kode}",
            cancel => inner.GetOrganisasjonsformAsync(kode, cancel),
            ct);

    // H6: Renamed from "kodeverk:organisasjonsformer:enheter" to align the cache key with
    // the WebApi URL ("organisasjonsformer-med-enheter") and disambiguate from "all
    // organisasjonsformer" (= "kodeverk:organisasjonsformer"). Same rationale for the
    // underenheter variant below.
    public Task<KodeverkLookupResult> GetOrganisasjonsformerWithEnheterAsync(CancellationToken ct) =>
        CachedListAsync(
            "kodeverk:organisasjonsformer-med-enheter",
            inner.GetOrganisasjonsformerWithEnheterAsync,
            ct);

    public Task<KodeverkLookupResult> GetOrganisasjonsformerWithUnderenheterAsync(CancellationToken ct) =>
        CachedListAsync(
            "kodeverk:organisasjonsformer-med-underenheter",
            inner.GetOrganisasjonsformerWithUnderenheterAsync,
            ct);

    // ---- Cache helpers (H5) ----
    // 13 endpoint methods used to copy/paste the same six-line lookup block. Lifting the
    // pattern into three private helpers (one per Result-type) collapses each call-site
    // to a one-line delegation. Functional behaviour is identical; only the boilerplate
    // is removed.

    private async Task<KodeverkLookupResult> CachedListAsync(
        string cacheKey,
        Func<CancellationToken, Task<KodeverkLookupResult>> fetch,
        CancellationToken ct)
    {
        logger.LogDebug("Cache lookup for {Key}", cacheKey);
        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => CachedKodeverk.From(await fetch(cancel)),
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);
        return cached.ToResult();
    }

    private async Task<KodeverkSingleResult> CachedSingleAsync(
        string cacheKey,
        Func<CancellationToken, Task<KodeverkSingleResult>> fetch,
        CancellationToken ct)
    {
        logger.LogDebug("Cache lookup for {Key}", cacheKey);
        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => CachedKodeverkSingle.From(await fetch(cancel)),
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);
        return cached.ToResult();
    }

    private async Task<KodeverkPagedResult> CachedPagedAsync(
        string cacheKey,
        Func<CancellationToken, Task<KodeverkPagedResult>> fetch,
        CancellationToken ct)
    {
        logger.LogDebug("Cache lookup for {Key}", cacheKey);
        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => CachedKodeverkPaged.From(await fetch(cancel)),
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

/// <summary>
/// Tri-state cached representation of <see cref="KodeverkSingleResult"/>: <see cref="Entry"/>
/// non-null indicates Found, <see cref="NotFoundCode"/> non-null indicates NotFound,
/// <see cref="UnavailableMessage"/> non-null indicates Unavailable. The three states are
/// mutually exclusive — <see cref="ToResult"/> throws if all three are null, which acts
/// as a runtime guard against malformed cache payloads (e.g. corrupted distributed-cache
/// entries).
/// </summary>
public sealed record CachedKodeverkSingle(
    KodeverkEntry? Entry,
    string? NotFoundCode,
    string? UnavailableMessage)
{
    public static CachedKodeverkSingle From(KodeverkSingleResult r) => r switch
    {
        KodeverkSingleResult.Found f => new CachedKodeverkSingle(f.Entry, null, null),
        KodeverkSingleResult.NotFound nf => new CachedKodeverkSingle(null, nf.Code, null),
        KodeverkSingleResult.Unavailable u => new CachedKodeverkSingle(null, null, u.Message),
        _ => throw new InvalidOperationException($"Unknown KodeverkSingleResult: {r.GetType().Name}"),
    };

    public KodeverkSingleResult ToResult()
    {
        if (Entry is not null) return new KodeverkSingleResult.Found(Entry);
        if (NotFoundCode is not null) return new KodeverkSingleResult.NotFound(NotFoundCode);
        if (UnavailableMessage is not null) return new KodeverkSingleResult.Unavailable(UnavailableMessage);
        throw new InvalidOperationException("CachedKodeverkSingle is empty — invalid state.");
    }
}

/// <summary>
/// Cache-friendly flat representation of <see cref="KodeverkPagedResult"/>. Paging fields
/// are nullable (<see cref="int"/>?) so a cached <em>Unavailable</em> entry can store
/// <c>null</c> rather than a sentinel like <c>0</c> — that way the absence of paging
/// metadata is impossible to mistake for "the upstream said page 0 has 0 entries". (H7)
/// </summary>
public sealed record CachedKodeverkPaged(
    IReadOnlyList<KodeverkEntry>? Entries,
    int? Page,
    int? Size,
    int? TotalElements,
    int? TotalPages,
    string? UnavailableMessage)
{
    public static CachedKodeverkPaged From(KodeverkPagedResult r) => r switch
    {
        KodeverkPagedResult.Found f => new CachedKodeverkPaged(f.Entries, f.Page, f.Size, f.TotalElements, f.TotalPages, null),
        KodeverkPagedResult.Unavailable u => new CachedKodeverkPaged(null, null, null, null, null, u.Message),
        _ => throw new InvalidOperationException($"Unknown KodeverkPagedResult: {r.GetType().Name}"),
    };

    public KodeverkPagedResult ToResult()
    {
        if (Entries is not null)
        {
            // Paging metadata is guaranteed by From(Found) to be non-null in the Found arm.
            return new KodeverkPagedResult.Found(
                Entries,
                Page ?? 0,
                Size ?? Entries.Count,
                TotalElements ?? Entries.Count,
                TotalPages ?? 1);
        }
        if (UnavailableMessage is not null) return new KodeverkPagedResult.Unavailable(UnavailableMessage);
        throw new InvalidOperationException("CachedKodeverkPaged is empty — invalid state.");
    }
}
