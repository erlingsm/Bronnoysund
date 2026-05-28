// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Caching;

/// <summary>
/// HybridCache decorator around <see cref="IMatrikkelenhetProvider"/>. Brreg's matrikkelenhet
/// rows change rarely (cadastral updates flow via municipalities into the registry on the
/// order of days), so a one-hour TTL gives Lookup-page renders a free hit without holding
/// stale data for long.
/// </summary>
/// <remarks>
/// <para>
/// Caching policy:
/// </para>
/// <list type="bullet">
///   <item>
///     <c>InvalidInput</c> is never cached. A query with both filters or neither is a caller
///     bug — caching the error would mask a fix on the caller side.
///   </item>
///   <item>
///     <c>Found</c>, <c>NotFound</c>, and <c>Unavailable</c> all share the same TTL. A short
///     TTL on NotFound is acceptable because cadastral additions are rare, and on Unavailable
///     because the upstream resilience layer already smooths brief outages — we do not want to
///     pin a transient error in cache for an hour.
///   </item>
/// </list>
/// </remarks>
internal sealed class CachingMatrikkelenhetProvider(
    IMatrikkelenhetProvider inner,
    HybridCache cache,
    ILogger<CachingMatrikkelenhetProvider> logger) : IMatrikkelenhetProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public async Task<MatrikkelenhetLookupResult> LookupAsync(MatrikkelenhetQuery query, CancellationToken ct)
    {
        // InvalidInput is delegated to the inner adapter without caching: the adapter owns the
        // begge-eller-ingen rule and we want every malformed call to surface the same error.
        var hasId = !string.IsNullOrWhiteSpace(query.MatrikkelenhetId);
        var hasNumber = !string.IsNullOrWhiteSpace(query.Matrikkelnummer);
        if (hasId == hasNumber)
        {
            return await inner.LookupAsync(query, ct).ConfigureAwait(false);
        }

        // Cache key uses the populated filter; the prefix keeps the two query-modes in disjoint
        // namespaces so a matrikkelnummer like "abc-123" cannot collide with a matrikkelenhetid.
        var cacheKey = hasId
            ? $"matrikkelenhet:id:{query.MatrikkelenhetId}"
            : $"matrikkelenhet:nr:{query.Matrikkelnummer}";
        logger.LogDebug("Cache lookup for {Key}", cacheKey);

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var result = await inner.LookupAsync(query, cancel).ConfigureAwait(false);
                return CachedMatrikkelenhet.From(result);
            },
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct).ConfigureAwait(false);

        return cached.ToResult();
    }
}

/// <summary>Cache-friendly flat representation of <see cref="MatrikkelenhetLookupResult"/>.</summary>
public sealed record CachedMatrikkelenhet(
    IReadOnlyList<MatrikkelenhetResponse>? Found,
    string? NotFoundQuery,
    string? UnavailableMessage)
{
    public static CachedMatrikkelenhet From(MatrikkelenhetLookupResult r) => r switch
    {
        MatrikkelenhetLookupResult.Found f => new CachedMatrikkelenhet(f.Matrikkelenheter, null, null),
        MatrikkelenhetLookupResult.NotFound nf => new CachedMatrikkelenhet(null, nf.Query, null),
        MatrikkelenhetLookupResult.Unavailable u => new CachedMatrikkelenhet(null, null, u.Message),
        // InvalidInput is filtered out by the decorator before this branch is reached. If we
        // ever do see it here it indicates a logic regression, hence the explicit throw rather
        // than a silent fall-through.
        MatrikkelenhetLookupResult.InvalidInput =>
            throw new InvalidOperationException("InvalidInput must not be cached."),
        _ => throw new InvalidOperationException($"Unknown MatrikkelenhetLookupResult: {r.GetType().Name}"),
    };

    public MatrikkelenhetLookupResult ToResult()
    {
        if (Found is not null) return new MatrikkelenhetLookupResult.Found(Found);
        if (NotFoundQuery is not null) return new MatrikkelenhetLookupResult.NotFound(NotFoundQuery);
        if (UnavailableMessage is not null) return new MatrikkelenhetLookupResult.Unavailable(UnavailableMessage);
        throw new InvalidOperationException("CachedMatrikkelenhet is empty — invalid state.");
    }
}
