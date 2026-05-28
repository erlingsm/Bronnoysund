// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Brreg.Generated.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Polly.CircuitBreaker;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IMatrikkelenhetProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. The Brreg endpoint requires exactly one filter
/// (matrikkelenhetid or matrikkelnummer); we enforce that client-side so a caller error
/// becomes <see cref="MatrikkelenhetLookupResult.InvalidInput"/> rather than a Brreg 400.
/// An empty result list maps to <see cref="MatrikkelenhetLookupResult.NotFound"/>.
/// </summary>
internal sealed class BrregMatrikkelenhetProvider(
    BrregClient client,
    ILogger<BrregMatrikkelenhetProvider> logger) : IMatrikkelenhetProvider
{
    public async Task<MatrikkelenhetLookupResult> LookupAsync(MatrikkelenhetQuery query, CancellationToken ct)
    {
        if (!ValidateQuery(query, out var validationError))
        {
            return new MatrikkelenhetLookupResult.InvalidInput(validationError);
        }

        var label = query.MatrikkelenhetId ?? query.Matrikkelnummer ?? string.Empty;
        const string Path = "/matrikkelenhet";

        try
        {
            var response = await client.Enhetsregisteret.Api.Matrikkelenhet
                .GetAsync(cfg =>
                {
                    if (!string.IsNullOrWhiteSpace(query.MatrikkelenhetId))
                    {
                        cfg.QueryParameters.Matrikkelenhetid = query.MatrikkelenhetId;
                    }
                    if (!string.IsNullOrWhiteSpace(query.Matrikkelnummer))
                    {
                        cfg.QueryParameters.Matrikkelnummer = query.Matrikkelnummer;
                    }
                }, ct).ConfigureAwait(false);

            // Null body from Kiota means an unexpected response structure (consistent with F7/H4
            // — empty list is a legitimate "no match", but null is a deserialiser/transport issue
            // we surface as Unavailable so callers can retry instead of caching NotFound).
            if (response is null)
            {
                return new MatrikkelenhetLookupResult.Unavailable(
                    "Brreg returned an unexpected response structure for /matrikkelenhet.");
            }

            var rawCount = response.Count;
            var items = response.Select(Map).OfType<MatrikkelenhetResponse>().ToList();

            // I7: Entries with missing matrikkelenhetid or orgnr are silently dropped by Map.
            // Surface the count so operators can spot upstream-shape regressions in logs without
            // every row needing a noisy per-entry warning.
            if (items.Count < rawCount)
            {
                logger.LogInformation(
                    "Skipped {Count} of {Total} matrikkelenheter due to missing matrikkelenhetid or orgnr (query: {Query})",
                    rawCount - items.Count, rawCount, label);
            }

            return items.Count == 0
                ? new MatrikkelenhetLookupResult.NotFound(label)
                : new MatrikkelenhetLookupResult.Found(items);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg Matrikkelenhet upstream error for {Query}", label);
            return new MatrikkelenhetLookupResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for {Path}");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Brreg Matrikkelenhet circuit open for {Query}", label);
            return new MatrikkelenhetLookupResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg Matrikkelenhet transport error for {Query}", label);
            return new MatrikkelenhetLookupResult.Unavailable(
                $"Could not contact Brreg for {Path}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg Matrikkelenhet timed out for {Query}", label);
            return new MatrikkelenhetLookupResult.Unavailable(
                $"Brreg did not respond within the timeout for {Path}");
        }
    }

    private static bool ValidateQuery(MatrikkelenhetQuery query, out string error)
    {
        var hasId = !string.IsNullOrWhiteSpace(query.MatrikkelenhetId);
        var hasNumber = !string.IsNullOrWhiteSpace(query.Matrikkelnummer);

        if (hasId == hasNumber)
        {
            error = hasId
                ? "Provide exactly one of MatrikkelenhetId or Matrikkelnummer — not both."
                : "Either MatrikkelenhetId or Matrikkelnummer must be provided.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static MatrikkelenhetResponse? Map(Matrikkelenhet m)
    {
        if (string.IsNullOrWhiteSpace(m.Matrikkelenhetid) || string.IsNullOrWhiteSpace(m.Orgnr))
        {
            return null;
        }

        return new MatrikkelenhetResponse(
            MatrikkelenhetId: m.Matrikkelenhetid,
            OrganizationNumber: m.Orgnr,
            KommuneNumber: NullIfEmpty(m.Kommnr),
            GardsNumber: NullIfEmpty(m.Gaardsnr),
            BruksNumber: NullIfEmpty(m.Bruksnr),
            FesteNumber: NullIfEmpty(m.Festenr),
            Order: int.TryParse(m.Rekkefolge, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                ? n
                : null);
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
