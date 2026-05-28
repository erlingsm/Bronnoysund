// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Caching;

/// <summary>
/// HybridCache decorator around <see cref="IVoluntaryOrganizationProvider"/>. The
/// Frivillighetsregister status changes rarely (typically once per month per entity),
/// so a one-hour TTL is appropriate.
/// </summary>
internal sealed class CachingVoluntaryOrganizationProvider(
    IVoluntaryOrganizationProvider inner,
    HybridCache cache,
    ILogger<CachingVoluntaryOrganizationProvider> logger) : IVoluntaryOrganizationProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public async Task<VoluntaryOrganizationLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct)
    {
        var cacheKey = $"voluntary:{org.Value}";
        logger.LogDebug("Cache lookup for {Key}", cacheKey);

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var result = await inner.LookupAsync(org, cancel);
                return CachedVoluntaryLookup.From(result);
            },
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);

        return cached.ToResult();
    }
}

/// <summary>Cache-friendly flat representation of <see cref="VoluntaryOrganizationLookupResult"/>.</summary>
public sealed record CachedVoluntaryLookup(
    VoluntaryOrganizationResponse? Found,
    string? NotRegisteredOrgnr,
    string? UnavailableMessage)
{
    public static CachedVoluntaryLookup From(VoluntaryOrganizationLookupResult r) => r switch
    {
        VoluntaryOrganizationLookupResult.Found f => new CachedVoluntaryLookup(f.Organization, null, null),
        VoluntaryOrganizationLookupResult.NotRegistered nr => new CachedVoluntaryLookup(null, nr.OrganizationNumber, null),
        VoluntaryOrganizationLookupResult.Unavailable u => new CachedVoluntaryLookup(null, null, u.Message),
        _ => throw new InvalidOperationException($"Unknown VoluntaryOrganizationLookupResult: {r.GetType().Name}"),
    };

    public VoluntaryOrganizationLookupResult ToResult()
    {
        if (Found is not null) return new VoluntaryOrganizationLookupResult.Found(Found);
        if (NotRegisteredOrgnr is not null) return new VoluntaryOrganizationLookupResult.NotRegistered(NotRegisteredOrgnr);
        if (UnavailableMessage is not null) return new VoluntaryOrganizationLookupResult.Unavailable(UnavailableMessage);
        throw new InvalidOperationException("CachedVoluntaryLookup is empty — invalid state.");
    }
}
