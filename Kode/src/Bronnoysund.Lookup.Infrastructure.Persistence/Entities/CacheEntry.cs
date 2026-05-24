// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistent cache-entry. GZip-komprimert. LRU-evicted ved overskridelse av maks-størrelse.
/// Brukes som L2 bak HybridCache (L1 = in-memory).
/// </summary>
public sealed class CacheEntry
{
    public required string Key { get; init; }                    // PK, f.eks. "org:919300388"
    public required byte[] Value { get; init; }                  // GZip-komprimert av GzipHybridCacheSerializer over
    public required int SizeBytes { get; init; }                 // = Value.Length, for max-size-beregning
    public required DateTimeOffset StoredAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required DateTimeOffset LastAccessedAt { get; set; }  // Oppdateres ved hver read (for LRU)
}
