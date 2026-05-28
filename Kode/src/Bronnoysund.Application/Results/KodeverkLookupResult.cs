// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;

namespace Bronnoysund.Application.Results;

/// <summary>
/// Discriminated union for a kodeverk lookup. Empty result list is still
/// <see cref="Found"/> with an empty list — distinct from <see cref="Unavailable"/>
/// which signals that we could not reach Brreg / Frivillighetsregisteret. The shape
/// mirrors the four enrichment-port Result-types so WebApi + UI can switch over them
/// uniformly.
/// </summary>
public abstract record KodeverkLookupResult
{
    private KodeverkLookupResult() { }

    public sealed record Found(IReadOnlyList<KodeverkEntry> Entries) : KodeverkLookupResult;

    public sealed record Unavailable(string Message) : KodeverkLookupResult;
}
