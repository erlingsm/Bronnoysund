// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Caching;

/// <summary>
/// HybridCache decorator around <see cref="IEntityChangesProvider"/>. Change feeds are
/// volatile by definition (they are a change log), so the TTL is intentionally short
/// (2 minutes) — it deduplicates page-reload bursts without hiding new events for long.
/// Cache key includes page-size so consumers requesting different page sizes get
/// separate entries.
/// </summary>
/// <remarks>
/// The response type <see cref="EntityChangesResponse"/> is already a flat record graph
/// without discriminated-union members, so it serializes directly via System.Text.Json
/// — no flattening wrapper is needed.
/// </remarks>
internal sealed class CachingEntityChangesProvider(
    IEntityChangesProvider inner,
    HybridCache cache,
    ILogger<CachingEntityChangesProvider> logger) : IEntityChangesProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    public async Task<EntityChangesResponse> GetChangesAsync(OrganizationNumber org, int pageSize, CancellationToken ct)
    {
        var cacheKey = $"changes:{org.Value}:{pageSize.ToString(CultureInfo.InvariantCulture)}";
        logger.LogDebug("Cache lookup for {Key}", cacheKey);

        return await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => await inner.GetChangesAsync(org, pageSize, cancel),
            new HybridCacheEntryOptions { Expiration = Ttl },
            cancellationToken: ct);
    }
}
