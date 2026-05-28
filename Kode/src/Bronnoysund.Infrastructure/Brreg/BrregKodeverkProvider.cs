// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Polly.CircuitBreaker;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IKodeverkProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Exposes Organisasjonsformer (Enhetsregisteret) plus
/// Frivillighetsregisteret's ICNPO categories and information-type catalogue.
/// Kommuner + Rolletyper + Rollegruppetyper land in iteration C.
/// All transient failures (5xx, broken circuit, transport, server-side timeout) are
/// mapped to <see cref="KodeverkLookupResult.Unavailable"/> so callers don't need to
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
}
