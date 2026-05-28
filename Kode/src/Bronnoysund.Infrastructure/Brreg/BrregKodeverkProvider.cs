// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using System.Net;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Polly.CircuitBreaker;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IKodeverkProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Exposes Enhetsregisteret kodeverk (Organisasjonsformer,
/// Kommuner, Rolletyper, Rollegruppetyper, Representanter) plus Frivillighetsregisteret's
/// ICNPO categories and information-type catalogue, and single-entity lookups for
/// Kommune + Organisasjonsform.
/// All transient failures (5xx, broken circuit, transport, server-side timeout) are
/// mapped to the corresponding <c>Unavailable</c> variant so callers don't need to
/// catch exceptions. Client-side cancellation is rethrown verbatim.
/// </summary>
internal sealed class BrregKodeverkProvider(
    BrregClient client,
    ILogger<BrregKodeverkProvider> logger) : IKodeverkProvider
{
    public async Task<KodeverkLookupResult> GetOrganisasjonsformerAsync(CancellationToken ct)
    {
        try
        {
            var response = await client.Enhetsregisteret.Api.Organisasjonsformer
                .GetAsync(cancellationToken: ct).ConfigureAwait(false);

            var entries = (response?.Embedded?.Organisasjonsformer ?? [])
                .Where(f => !string.IsNullOrWhiteSpace(f.Kode))
                .Select(f => new KodeverkEntry(
                    Code: f.Kode!,
                    Description: f.Beskrivelse ?? string.Empty,
                    IsRetired: !string.IsNullOrWhiteSpace(f.Utgaatt)))
                .ToList();

            return new KodeverkLookupResult.Found(entries);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsformer upstream error");
            return new KodeverkLookupResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for /organisasjonsformer");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsformer circuit open");
            return new KodeverkLookupResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsformer transport error");
            return new KodeverkLookupResult.Unavailable(
                $"Could not contact Brreg for /organisasjonsformer: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsformer timed out");
            return new KodeverkLookupResult.Unavailable(
                "Brreg did not respond within the timeout for /organisasjonsformer");
        }
    }

    public async Task<KodeverkLookupResult> GetIcnpoCategoriesAsync(CancellationToken ct)
    {
        try
        {
            var response = await client.Frivillighetsregisteret.Api.IcnpoKategorier
                .GetAsync(cfg => cfg.QueryParameters.Spraak = "NOB", ct).ConfigureAwait(false);

            var entries = (response?.Embedded?.IcnpoKategorier ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c.IcnpoNummer))
                .Select(c => new KodeverkEntry(
                    Code: c.IcnpoNummer!,
                    Description: c.Navn ?? string.Empty))
                .ToList();

            return new KodeverkLookupResult.Found(entries);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret ICNPO categories upstream error");
            return new KodeverkLookupResult.Unavailable(
                $"Frivillighetsregisteret returned HTTP {ex.ResponseStatusCode} for /icnpo-kategorier");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret ICNPO categories circuit open");
            return new KodeverkLookupResult.Unavailable("Frivillighetsregisteret is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret ICNPO categories transport error");
            return new KodeverkLookupResult.Unavailable(
                $"Could not contact Frivillighetsregisteret for /icnpo-kategorier: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret ICNPO categories timed out");
            return new KodeverkLookupResult.Unavailable(
                "Frivillighetsregisteret did not respond within the timeout for /icnpo-kategorier");
        }
    }

    public async Task<KodeverkLookupResult> GetVoluntaryInformationTypesAsync(CancellationToken ct)
    {
        try
        {
            var response = await client.Frivillighetsregisteret.Api.Informasjonstyper
                .GetAsync(cfg => cfg.QueryParameters.Spraak = "NOB", ct).ConfigureAwait(false);

            var entries = (response?.Embedded?.Informasjonstyper ?? [])
                .Where(t => !string.IsNullOrWhiteSpace(t.Identifikator))
                .Select(t => new KodeverkEntry(
                    Code: t.Identifikator!,
                    Description: t.Navn ?? string.Empty))
                .ToList();

            return new KodeverkLookupResult.Found(entries);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret information-types upstream error");
            return new KodeverkLookupResult.Unavailable(
                $"Frivillighetsregisteret returned HTTP {ex.ResponseStatusCode} for /informasjonstyper");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret information-types circuit open");
            return new KodeverkLookupResult.Unavailable("Frivillighetsregisteret is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret information-types transport error");
            return new KodeverkLookupResult.Unavailable(
                $"Could not contact Frivillighetsregisteret for /informasjonstyper: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret information-types timed out");
            return new KodeverkLookupResult.Unavailable(
                "Frivillighetsregisteret did not respond within the timeout for /informasjonstyper");
        }
    }

    public async Task<KodeverkPagedResult> GetKommunerAsync(int page, int size, CancellationToken ct)
    {
        const string Path = "/kommuner";
        try
        {
            var response = await client.Enhetsregisteret.Api.Kommuner
                .GetAsync(cfg =>
                {
                    cfg.QueryParameters.Page = page.ToString(CultureInfo.InvariantCulture);
                    cfg.QueryParameters.Size = size.ToString(CultureInfo.InvariantCulture);
                }, ct).ConfigureAwait(false);

            var entries = (response?.Embedded?.Kommuner ?? [])
                .Where(k => !string.IsNullOrWhiteSpace(k.Nummer))
                .Select(k => new KodeverkEntry(
                    Code: k.Nummer!,
                    Description: k.Navn ?? string.Empty))
                .ToList();

            // H2: Brreg's OpenAPI spec types Page.{Number,Size,TotalElements,TotalPages} as
            // `double` (a spec-quirk where `integer` was generated as `number`). A raw `(int)`
            // cast truncates and silently overflows for values > int.MaxValue. SafeInt clamps
            // into the int range and rounds, so future reuse with larger entity-counts cannot
            // produce negative or nonsense paging metadata.
            var totalElements = SafeInt(response?.Page?.TotalElements, entries.Count);
            var totalPages = SafeInt(response?.Page?.TotalPages, 1);
            var reportedPage = SafeInt(response?.Page?.Number, page);
            var reportedSize = SafeInt(response?.Page?.Size, size);

            return new KodeverkPagedResult.Found(entries, reportedPage, reportedSize, totalElements, totalPages);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg Kommuner upstream error");
            return new KodeverkPagedResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for {Path}");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Brreg Kommuner circuit open");
            return new KodeverkPagedResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg Kommuner transport error");
            return new KodeverkPagedResult.Unavailable(
                $"Could not contact Brreg for {Path}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg Kommuner timed out");
            return new KodeverkPagedResult.Unavailable(
                $"Brreg did not respond within the timeout for {Path}");
        }
    }

    public async Task<KodeverkSingleResult> GetKommuneAsync(string kommunenummer, CancellationToken ct)
    {
        var path = $"/kommuner/{kommunenummer}";
        try
        {
            var response = await client.Enhetsregisteret.Api.Kommuner[kommunenummer]
                .GetAsync(cancellationToken: ct).ConfigureAwait(false);

            // H4: Distinguish "Brreg returned 200 but the body could not be parsed" (spec
            // drift → Unavailable) from "Brreg returned the entity but Nummer is empty"
            // (treated as NotFound — Brreg normally returns 404, this is a belt-and-braces
            // path). Matches F7's resolution in BrregLegalRolesProvider.
            if (response is null)
            {
                return new KodeverkSingleResult.Unavailable(
                    $"Brreg returned an unexpected response structure for {path}.");
            }
            if (string.IsNullOrWhiteSpace(response.Nummer))
            {
                return new KodeverkSingleResult.NotFound(kommunenummer);
            }

            return new KodeverkSingleResult.Found(new KodeverkEntry(
                Code: response.Nummer,
                Description: response.Navn ?? string.Empty));
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound ||
                                       ex.ResponseStatusCode == (int)HttpStatusCode.Gone)
        {
            return new KodeverkSingleResult.NotFound(kommunenummer);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg Kommune upstream error for {Kommunenummer}", kommunenummer);
            return new KodeverkSingleResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for {path}");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Brreg Kommune circuit open for {Kommunenummer}", kommunenummer);
            return new KodeverkSingleResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg Kommune transport error for {Kommunenummer}", kommunenummer);
            return new KodeverkSingleResult.Unavailable(
                $"Could not contact Brreg for {path}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg Kommune timed out for {Kommunenummer}", kommunenummer);
            return new KodeverkSingleResult.Unavailable(
                $"Brreg did not respond within the timeout for {path}");
        }
    }

    public async Task<KodeverkLookupResult> GetRolletyperAsync(CancellationToken ct) =>
        await GetSimpleListAsync(
            "/roller/rolletyper",
            async cancel => (await client.Enhetsregisteret.Api.Roller.Rolletyper.GetAsync(cancellationToken: cancel).ConfigureAwait(false))
                ?.Embedded?.Rolletyper?
                .Where(r => !string.IsNullOrWhiteSpace(r.Kode))
                .Select(r => new KodeverkEntry(r.Kode!, r.Beskrivelse ?? string.Empty))
                .ToList() ?? [],
            ct).ConfigureAwait(false);

    public async Task<KodeverkLookupResult> GetRollegruppetyperAsync(CancellationToken ct) =>
        await GetSimpleListAsync(
            "/roller/rollegruppetyper",
            async cancel => (await client.Enhetsregisteret.Api.Roller.Rollegruppetyper.GetAsync(cancellationToken: cancel).ConfigureAwait(false))
                ?.Embedded?.Rollegruppetyper?
                .Where(r => !string.IsNullOrWhiteSpace(r.Kode))
                .Select(r => new KodeverkEntry(r.Kode!, r.Beskrivelse ?? string.Empty))
                .ToList() ?? [],
            ct).ConfigureAwait(false);

    public async Task<KodeverkLookupResult> GetRepresentanterAsync(CancellationToken ct) =>
        await GetSimpleListAsync(
            "/roller/representanter",
            async cancel => (await client.Enhetsregisteret.Api.Roller.Representanter.GetAsync(cancellationToken: cancel).ConfigureAwait(false))
                ?.Embedded?.Representanter?
                .Where(r => !string.IsNullOrWhiteSpace(r.Kode))
                .Select(r => new KodeverkEntry(r.Kode!, r.Beskrivelse ?? string.Empty))
                .ToList() ?? [],
            ct).ConfigureAwait(false);

    public async Task<KodeverkSingleResult> GetOrganisasjonsformAsync(string kode, CancellationToken ct)
    {
        var path = $"/organisasjonsformer/{kode}";
        try
        {
            var response = await client.Enhetsregisteret.Api.Organisasjonsformer[kode]
                .GetAsync(cancellationToken: ct).ConfigureAwait(false);

            // H4: Distinguish "Brreg returned 200 but the body could not be parsed" (spec
            // drift → Unavailable) from "Brreg returned the entity but Kode is empty"
            // (treated as NotFound). Matches F7's resolution in BrregLegalRolesProvider.
            if (response is null)
            {
                return new KodeverkSingleResult.Unavailable(
                    $"Brreg returned an unexpected response structure for {path}.");
            }
            if (string.IsNullOrWhiteSpace(response.Kode))
            {
                return new KodeverkSingleResult.NotFound(kode);
            }

            return new KodeverkSingleResult.Found(new KodeverkEntry(
                Code: response.Kode,
                Description: response.Beskrivelse ?? string.Empty,
                IsRetired: !string.IsNullOrWhiteSpace(response.Utgaatt)));
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound ||
                                       ex.ResponseStatusCode == (int)HttpStatusCode.Gone)
        {
            return new KodeverkSingleResult.NotFound(kode);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsform upstream error for {Kode}", kode);
            return new KodeverkSingleResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for {path}");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsform circuit open for {Kode}", kode);
            return new KodeverkSingleResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsform transport error for {Kode}", kode);
            return new KodeverkSingleResult.Unavailable(
                $"Could not contact Brreg for {path}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsform timed out for {Kode}", kode);
            return new KodeverkSingleResult.Unavailable(
                $"Brreg did not respond within the timeout for {path}");
        }
    }

    public async Task<KodeverkLookupResult> GetOrganisasjonsformerWithEnheterAsync(CancellationToken ct) =>
        await GetSimpleListAsync(
            "/organisasjonsformer/enheter",
            async cancel => (await client.Enhetsregisteret.Api.Organisasjonsformer.Enheter.GetAsync(cancellationToken: cancel).ConfigureAwait(false))
                ?.Embedded?.Organisasjonsformer?
                .Where(o => !string.IsNullOrWhiteSpace(o.Kode))
                .Select(o => new KodeverkEntry(
                    Code: o.Kode!,
                    Description: o.Beskrivelse ?? string.Empty,
                    IsRetired: !string.IsNullOrWhiteSpace(o.Utgaatt)))
                .ToList() ?? [],
            ct).ConfigureAwait(false);

    public async Task<KodeverkLookupResult> GetOrganisasjonsformerWithUnderenheterAsync(CancellationToken ct) =>
        await GetSimpleListAsync(
            "/organisasjonsformer/underenheter",
            async cancel => (await client.Enhetsregisteret.Api.Organisasjonsformer.Underenheter.GetAsync(cancellationToken: cancel).ConfigureAwait(false))
                ?.Embedded?.Organisasjonsformer?
                .Where(o => !string.IsNullOrWhiteSpace(o.Kode))
                .Select(o => new KodeverkEntry(
                    Code: o.Kode!,
                    Description: o.Beskrivelse ?? string.Empty,
                    IsRetired: !string.IsNullOrWhiteSpace(o.Utgaatt)))
                .ToList() ?? [],
            ct).ConfigureAwait(false);

    /// <summary>
    /// Shared try/catch helper for flat (non-paged) kodeverk-list calls. The caller supplies
    /// the path (used in log + error messages) and a thunk that projects to a
    /// <see cref="KodeverkEntry"/> list.
    /// <para>
    /// Used by five methods: <see cref="GetRolletyperAsync"/>,
    /// <see cref="GetRollegruppetyperAsync"/>, <see cref="GetRepresentanterAsync"/>,
    /// <see cref="GetOrganisasjonsformerWithEnheterAsync"/>, and
    /// <see cref="GetOrganisasjonsformerWithUnderenheterAsync"/>.
    /// </para>
    /// <para>
    /// Implements the S1+S2 resilience pattern by mapping the standard four-exception set
    /// (<see cref="ApiException"/>, <see cref="BrokenCircuitException"/>,
    /// <see cref="HttpRequestException"/>, <see cref="TaskCanceledException"/>) to
    /// <see cref="KodeverkLookupResult.Unavailable"/>. Client-side cancellation is
    /// rethrown verbatim via the <c>when (!ct.IsCancellationRequested)</c> guard.
    /// </para>
    /// <para>
    /// Deliberately does NOT handle pagination — only suitable for endpoints whose
    /// payload is a single embedded list (no <c>page</c> / <c>_links</c> traversal).
    /// Paginated endpoints (Kommuner) keep their own try/catch so the helper signature
    /// stays simple.
    /// </para>
    /// <para>
    /// Cancellation-token scoping: the catch guard uses the caller's <paramref name="ct"/>,
    /// NOT the inner <c>cancel</c> parameter Kiota threads through the lambda. The two
    /// tokens are the same in practice because the caller forwards <paramref name="ct"/>
    /// into the thunk, but the guard intentionally bind to the outer name so it stays
    /// correct even if a future caller forwards a different token.
    /// </para>
    /// </summary>
    private async Task<KodeverkLookupResult> GetSimpleListAsync(
        string path,
        Func<CancellationToken, Task<List<KodeverkEntry>>> fetch,
        CancellationToken ct)
    {
        try
        {
            var entries = await fetch(ct).ConfigureAwait(false);
            return new KodeverkLookupResult.Found(entries);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg {Path} upstream error", path);
            return new KodeverkLookupResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for {path}");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Brreg {Path} circuit open", path);
            return new KodeverkLookupResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg {Path} transport error", path);
            return new KodeverkLookupResult.Unavailable(
                $"Could not contact Brreg for {path}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg {Path} timed out", path);
            return new KodeverkLookupResult.Unavailable(
                $"Brreg did not respond within the timeout for {path}");
        }
    }

    /// <summary>
    /// Defensive cast from Kiota's <c>double?</c> paging fields into our <c>int</c>-typed
    /// Result-shape. Rounds half-away-from-zero, clamps into <c>[0, int.MaxValue]</c>, and
    /// falls back to <paramref name="fallback"/> when the source is <c>null</c>. Replaces
    /// a raw <c>(int)</c> cast which would overflow / truncate for large values.
    /// </summary>
    private static int SafeInt(double? value, int fallback) =>
        value is double v ? (int)Math.Clamp(Math.Round(v), 0, int.MaxValue) : fallback;
}
