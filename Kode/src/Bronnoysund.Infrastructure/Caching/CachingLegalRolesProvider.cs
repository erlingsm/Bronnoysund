// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Caching;

/// <summary>
/// HybridCache decorator around <see cref="ILegalRolesProvider"/>. Legal-role
/// holdings change roughly monthly, so a 30-minute TTL balances freshness against
/// the cost of repeated Brreg calls during a single user session.
/// </summary>
internal sealed class CachingLegalRolesProvider(
    ILegalRolesProvider inner,
    HybridCache cache,
    ILogger<CachingLegalRolesProvider> logger) : ILegalRolesProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    public async Task<LegalRolesLookupResult> GetLegalRolesAsync(OrganizationNumber org, CancellationToken ct)
    {
        var cacheKey = $"legalroles:{org.Value}";
        logger.LogDebug("Cache lookup for {Key}", cacheKey);

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var result = await inner.GetLegalRolesAsync(org, cancel);
                return CachedLegalRolesLookup.From(result);
            },
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);

        return cached.ToResult();
    }
}

/// <summary>Cache-friendly flat representation of <see cref="LegalRolesLookupResult"/>.</summary>
public sealed record CachedLegalRolesLookup(
    LegalRolesResponse? Found,
    string? NotFoundOrgnr,
    string? UnavailableMessage)
{
    public static CachedLegalRolesLookup From(LegalRolesLookupResult r) => r switch
    {
        LegalRolesLookupResult.Found f => new CachedLegalRolesLookup(f.Roles, null, null),
        LegalRolesLookupResult.NotFound nf => new CachedLegalRolesLookup(null, nf.OrganizationNumber, null),
        LegalRolesLookupResult.Unavailable u => new CachedLegalRolesLookup(null, null, u.Message),
        _ => throw new InvalidOperationException($"Unknown LegalRolesLookupResult: {r.GetType().Name}"),
    };

    public LegalRolesLookupResult ToResult()
    {
        if (Found is not null) return new LegalRolesLookupResult.Found(Found);
        if (NotFoundOrgnr is not null) return new LegalRolesLookupResult.NotFound(NotFoundOrgnr);
        if (UnavailableMessage is not null) return new LegalRolesLookupResult.Unavailable(UnavailableMessage);
        throw new InvalidOperationException("CachedLegalRolesLookup is empty — invalid state.");
    }
}
