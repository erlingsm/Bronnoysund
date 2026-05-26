// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistent cache entry. GZip-compressed. LRU-evicted when the max size is exceeded.
/// Used as L2 behind HybridCache (L1 = in-memory).
/// </summary>
public sealed class CacheEntry
{
    public required string Key { get; init; }                    // PK, e.g. "org:919300388"
    public required byte[] Value { get; init; }                  // GZip-compressed by GzipHybridCacheSerializer above
    public required int SizeBytes { get; init; }                 // = Value.Length, for the max-size calculation
    public required DateTimeOffset StoredAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required DateTimeOffset LastAccessedAt { get; set; }  // Updated on every read (for LRU)
}
