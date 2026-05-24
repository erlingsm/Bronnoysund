// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Lookup.Domain;
using Bronnoysund.Lookup.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

/// <summary>
/// Typed HTTP client for the Brreg Enhetsregisteret. Sends GET /enheter/{orgnr}. Returns
/// null on 404 ("not found" as a business outcome), throws <see cref="BrregUnavailableException"/>
/// on technical failures (timeout, 5xx after Polly retry, circuit breaker open).
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
                logger.LogInformation("Brreg returned 404 for {OrgNumber}", org.Value);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Gone)
            {
                logger.LogInformation("Brreg returned 410 (deleted) for {OrgNumber}", org.Value);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<BrregEnhetDto>(ct);
            return dto;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new BrregUnavailableException(
                $"Brreg did not respond within the timeout for {org.Value}.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new BrregUnavailableException(
                $"Could not contact Brreg for {org.Value}: {ex.Message}", ex);
        }
    }
}
