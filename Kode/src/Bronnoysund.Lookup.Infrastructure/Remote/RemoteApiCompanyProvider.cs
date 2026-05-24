// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Lookup.Application.Dtos;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Domain;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Infrastructure.Remote;

/// <summary>
/// ICompanyProvider adapter that calls OUR own Web API (in the cloud) instead of Brreg directly.
/// Used when the app runs as a Thin Client — typically for the subscription version in app stores.
/// </summary>
internal sealed class RemoteApiCompanyProvider(
    HttpClient http,
    ILogger<RemoteApiCompanyProvider> logger) : ICompanyProvider
{
    public async Task<CompanyLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct)
    {
        var path = $"companies/{org.Value}";
        logger.LogDebug("Remote API GET {Path}", path);

        try
        {
            using var response = await http.GetAsync(path, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new CompanyLookupResult.NotFound(org.Value);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var problem = await response.Content.ReadAsStringAsync(ct);
                return new CompanyLookupResult.InvalidInput(problem);
            }

            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<CompanyResponse>(ct);
            return dto is null
                ? new CompanyLookupResult.Unavailable("Remote API returned an empty response.")
                : new CompanyLookupResult.Found(dto);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return new CompanyLookupResult.Unavailable($"Remote API did not respond in time: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            return new CompanyLookupResult.Unavailable($"Could not contact the Remote API: {ex.Message}");
        }
    }
}
