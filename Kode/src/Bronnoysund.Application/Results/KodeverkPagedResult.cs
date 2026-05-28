// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;

namespace Bronnoysund.Application.Results;

/// <summary>
/// Discriminated union for paginated kodeverk listings (Kommuner is the only entry
/// large enough to need paging — Brreg caps page size at 100, so a single call
/// will not return all ~5200 kommuner). Mirrors the shape of
/// <see cref="KodeverkLookupResult"/> but carries paging metadata.
/// </summary>
public abstract record KodeverkPagedResult
{
    private KodeverkPagedResult() { }

    public sealed record Found(
        IReadOnlyList<KodeverkEntry> Entries,
        int Page,
        int Size,
        int TotalElements,
        int TotalPages) : KodeverkPagedResult;

    public sealed record Unavailable(string Message) : KodeverkPagedResult;
}
