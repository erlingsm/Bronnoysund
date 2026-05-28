// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Caching;

/// <summary>
/// HybridCache decorator around <see cref="ISubUnitDetailsProvider"/>. Sub-unit details
/// rarely change, so a one-hour TTL is a comfortable compromise. Mirrors the wrapper
/// pattern from <see cref="CachingCompanyProvider"/> — the Result discriminated union
/// is flattened into <see cref="CachedSubUnitLookup"/> before serialization because
/// HybridCache + System.Text.Json do not support polymorphic serialization.
/// </summary>
internal sealed class CachingSubUnitDetailsProvider(
    ISubUnitDetailsProvider inner,
    HybridCache cache,
    ILogger<CachingSubUnitDetailsProvider> logger) : ISubUnitDetailsProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public async Task<SubUnitLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct)
    {
        var cacheKey = $"subunit:{org.Value}";
        logger.LogDebug("Cache lookup for {Key}", cacheKey);

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var result = await inner.LookupAsync(org, cancel);
                return CachedSubUnitLookup.From(result);
            },
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);

        return cached.ToResult();
    }
}

/// <summary>Cache-friendly flat representation of <see cref="SubUnitLookupResult"/>.</summary>
public sealed record CachedSubUnitLookup(
    SubUnitDetailsResponse? Found,
    string? NotFoundOrgnr,
    string? UnavailableMessage)
{
    public static CachedSubUnitLookup From(SubUnitLookupResult r) => r switch
    {
        SubUnitLookupResult.Found f => new CachedSubUnitLookup(f.SubUnit, null, null),
        SubUnitLookupResult.NotFound nf => new CachedSubUnitLookup(null, nf.OrganizationNumber, null),
        SubUnitLookupResult.Unavailable u => new CachedSubUnitLookup(null, null, u.Message),
        _ => throw new InvalidOperationException($"Unknown SubUnitLookupResult: {r.GetType().Name}"),
    };

    public SubUnitLookupResult ToResult()
    {
        if (Found is not null) return new SubUnitLookupResult.Found(Found);
        if (NotFoundOrgnr is not null) return new SubUnitLookupResult.NotFound(NotFoundOrgnr);
        if (UnavailableMessage is not null) return new SubUnitLookupResult.Unavailable(UnavailableMessage);
        throw new InvalidOperationException("CachedSubUnitLookup is empty — invalid state.");
    }
}
