// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Provider for Brreg kodeverk-endepunkter (Organisasjonsformer, Kommuner, Rolletyper, etc.).
/// These change rarely so callers should cache aggressively (24h+ TTL). Plan 54's Browse-page
/// is the primary consumer. All three methods return a <see cref="KodeverkLookupResult"/>
/// discriminated union so callers can handle <c>Unavailable</c> as a value rather than
/// catching exceptions — mirrors the per-entity Result-types added in iteration 8.
/// </summary>
public interface IKodeverkProvider
{
    Task<KodeverkLookupResult> GetOrganisasjonsformerAsync(CancellationToken ct);

    /// <summary>
    /// ICNPO (International Classification of Non-Profit Organizations) categories used by
    /// Frivillighetsregisteret to tag voluntary organizations.
    /// </summary>
    Task<KodeverkLookupResult> GetIcnpoCategoriesAsync(CancellationToken ct);

    /// <summary>
    /// Metadata catalogue describing the information types Frivillighetsregisteret exposes
    /// per organization. Useful for tooling that consumes the voluntary-org API.
    /// </summary>
    Task<KodeverkLookupResult> GetVoluntaryInformationTypesAsync(CancellationToken ct);
}

/// <summary>
/// One row in a kodeverk-table. The shape matches Brreg's standard kodeverk-respons:
/// short code + human-readable description, optionally marked as retired.
/// </summary>
public sealed record KodeverkEntry(string Code, string Description, bool IsRetired = false);
