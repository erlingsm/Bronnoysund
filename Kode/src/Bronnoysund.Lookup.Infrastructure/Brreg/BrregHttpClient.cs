// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Lookup.Domain;
using Bronnoysund.Lookup.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

/// <summary>
/// Typed HTTP-klient mot Brreg Enhetsregisteret. Sender GET /enheter/{orgnr}. Returnerer
/// null ved 404 ("ikke funnet" som forretningsfeil), kaster <see cref="BrregUnavailableException"/>
/// ved tekniske feil (timeout, 5xx etter Polly-retry, circuit breaker open).
/// </summary>
internal sealed class BrregHttpClient(HttpClient http, ILogger<BrregHttpClient> logger)
{
    public async Task<BrregEnhetDto?> GetEnhetAsync(OrganizationNumber org, CancellationToken ct)
    {
        var path = $"enheter/{org.Value}";
        logger.LogDebug("Brreg GET {Path}", path);

        try
        {
            using var response = await http.GetAsync(path, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Brreg returnerte 404 for {OrgNumber}", org.Value);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Gone)
            {
                logger.LogInformation("Brreg returnerte 410 (slettet) for {OrgNumber}", org.Value);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<BrregEnhetDto>(ct);
            return dto;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new BrregUnavailableException(
                $"Brreg svarte ikke innen tidsfristen for {org.Value}.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new BrregUnavailableException(
                $"Kunne ikke kontakte Brreg for {org.Value}: {ex.Message}", ex);
        }
    }
}
