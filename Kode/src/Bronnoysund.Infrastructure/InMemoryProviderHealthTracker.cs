// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Collections.Concurrent;
using Bronnoysund.Application.Ports;

namespace Bronnoysund.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="IProviderHealthTracker"/>. ConcurrentDictionary
/// keyed by ISO 3166-1 alpha-2 country code. Read paths (the /health/providers endpoint
/// and the UI picker) take a snapshot view; writes from the lookup handlers don't block
/// each other.
/// </summary>
internal sealed class InMemoryProviderHealthTracker : IProviderHealthTracker
{
    private readonly ConcurrentDictionary<string, ProviderHealthSnapshot> _byCountry =
        new(StringComparer.OrdinalIgnoreCase);

    public void RecordSuccess(string countryCode)
    {
        var now = DateTimeOffset.UtcNow;
        _byCountry.AddOrUpdate(countryCode,
            _ => new ProviderHealthSnapshot(now, null, null),
            (_, existing) => existing with { LastSuccess = now });
    }

    public void RecordFailure(string countryCode, string? reason)
    {
        var now = DateTimeOffset.UtcNow;
        _byCountry.AddOrUpdate(countryCode,
            _ => new ProviderHealthSnapshot(null, now, reason),
            (_, existing) => existing with { LastFailure = now, LastFailureReason = reason });
    }

    public IReadOnlyDictionary<string, ProviderHealthSnapshot> Snapshot => _byCountry;
}
