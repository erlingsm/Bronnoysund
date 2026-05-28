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

    /// <summary>
    /// Norwegian municipalities (Kommuner) — ~5200 entries, hence the paged signature.
    /// Brreg caps server-side <c>size</c> at 100. Sort defaults to ascending kommunenummer.
    /// </summary>
    Task<KodeverkPagedResult> GetKommunerAsync(int page, int size, CancellationToken ct);

    /// <summary>
    /// Look up a single Kommune by its kommunenummer. Today's catalogue uses zero-padded
    /// four-digit codes (e.g. <c>"0301"</c> for Oslo); historical reorganisations have
    /// occasionally used shorter or merged codes, so callers should not assume strict
    /// 4-digit semantics. Brreg returns 404 for unknown numbers.
    /// </summary>
    Task<KodeverkSingleResult> GetKommuneAsync(string kommunenummer, CancellationToken ct);

    /// <summary>
    /// Role types (e.g. <c>DAGL</c>=Daglig leder, <c>STYR</c>=Styreleder). Small list (~30 entries).
    /// </summary>
    Task<KodeverkLookupResult> GetRolletyperAsync(CancellationToken ct);

    /// <summary>
    /// Role group types — categories that group rolletyper (e.g. "Styre", "Daglig leder").
    /// </summary>
    Task<KodeverkLookupResult> GetRollegruppetyperAsync(CancellationToken ct);

    /// <summary>
    /// Legal-representative type codes (kontaktperson-roller used for AML reporting).
    /// </summary>
    Task<KodeverkLookupResult> GetRepresentanterAsync(CancellationToken ct);

    /// <summary>
    /// Look up a single organisasjonsform by code (e.g. <c>AS</c>, <c>ENK</c>, <c>FLI</c>).
    /// </summary>
    Task<KodeverkSingleResult> GetOrganisasjonsformAsync(string kode, CancellationToken ct);

    /// <summary>
    /// Returns the list of <strong>organisasjonsformer</strong> that have at least one main
    /// entity (Enhet) registered against them — i.e. a filtered <em>organisasjonsform</em>-list,
    /// NOT a list of Enheter. Useful filter for UIs that want to hide retired forms with
    /// no live registrations. Backed by Brreg's <c>/organisasjonsformer/enheter</c>.
    /// </summary>
    Task<KodeverkLookupResult> GetOrganisasjonsformerWithEnheterAsync(CancellationToken ct);

    /// <summary>
    /// Returns the list of <strong>organisasjonsformer</strong> that have at least one
    /// sub-unit (Underenhet) registered against them — i.e. a filtered <em>organisasjonsform</em>-list,
    /// NOT a list of Underenheter. Backed by Brreg's <c>/organisasjonsformer/underenheter</c>.
    /// </summary>
    Task<KodeverkLookupResult> GetOrganisasjonsformerWithUnderenheterAsync(CancellationToken ct);
}

/// <summary>
/// One row in a kodeverk-table. The shape matches Brreg's standard kodeverk-respons:
/// short code + human-readable description, optionally marked as retired.
/// </summary>
public sealed record KodeverkEntry(string Code, string Description, bool IsRetired = false);
