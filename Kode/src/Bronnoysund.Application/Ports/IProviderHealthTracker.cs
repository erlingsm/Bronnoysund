// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Records the most recent live-call outcome per country code so the UI status dot
/// (Plan 26, Trinn B2/Fase 4) can distinguish "configured but failing" from "configured
/// and live". The implementation is in-memory + process-local — restarts reset the
/// snapshot, which matches the rest of the registry-level health state.
/// </summary>
public interface IProviderHealthTracker
{
    /// <summary>Stamp a country code with a successful live call.</summary>
    void RecordSuccess(string countryCode);

    /// <summary>Stamp a country code with a failed live call (Unavailable / transport error).</summary>
    void RecordFailure(string countryCode, string? reason);

    /// <summary>
    /// Snapshot of the recorded outcomes. Countries that have never been called are absent
    /// from the dictionary.
    /// </summary>
    IReadOnlyDictionary<string, ProviderHealthSnapshot> Snapshot { get; }
}

public sealed record ProviderHealthSnapshot(
    DateTimeOffset? LastSuccess,
    DateTimeOffset? LastFailure,
    string? LastFailureReason)
{
    /// <summary>
    /// True when the most recent recorded outcome was a failure (or only failures are on
    /// record). Drives the "red" picker dot in the UI.
    /// </summary>
    public bool IsFailingNow =>
        LastFailure is { } f && (LastSuccess is null || f > LastSuccess);
}
