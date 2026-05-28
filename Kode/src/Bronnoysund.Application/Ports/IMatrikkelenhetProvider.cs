// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Provider for <c>/enhetsregisteret/api/matrikkelenhet</c>. The Brreg endpoint requires
/// exactly one of <see cref="MatrikkelenhetQuery.MatrikkelenhetId"/> or
/// <see cref="MatrikkelenhetQuery.Matrikkelnummer"/> — the adapter enforces this
/// client-side and returns <see cref="MatrikkelenhetLookupResult.InvalidInput"/> when both
/// or neither is supplied.
/// </summary>
public interface IMatrikkelenhetProvider
{
    Task<MatrikkelenhetLookupResult> LookupAsync(MatrikkelenhetQuery query, CancellationToken ct);
}

/// <summary>
/// Query for matrikkelenhet lookup. Provide exactly one of the two filters:
/// <see cref="MatrikkelenhetId"/> resolves a single cadastral unit by Brreg's internal id;
/// <see cref="Matrikkelnummer"/> uses Brreg's composite-string format
/// (e.g. <c>0301-1/1</c> for Oslo kommunenummer + gnr/bnr).
/// </summary>
/// <remarks>
/// The constructor normalises whitespace-only inputs to <c>null</c> and trims surrounding
/// whitespace from real values, so a query like <c>(MatrikkelenhetId: "   ")</c> is treated
/// the same as <c>(MatrikkelenhetId: null)</c> — i.e. unset. This makes the begge-eller-ingen
/// validation in the adapter behave predictably for query-string inputs that arrive with
/// stray whitespace.
/// </remarks>
public sealed record MatrikkelenhetQuery
{
    public string? MatrikkelenhetId { get; }
    public string? Matrikkelnummer { get; }

    public MatrikkelenhetQuery(string? MatrikkelenhetId = null, string? Matrikkelnummer = null)
    {
        this.MatrikkelenhetId = NormaliseOrNull(MatrikkelenhetId);
        this.Matrikkelnummer = NormaliseOrNull(Matrikkelnummer);
    }

    private static string? NormaliseOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
