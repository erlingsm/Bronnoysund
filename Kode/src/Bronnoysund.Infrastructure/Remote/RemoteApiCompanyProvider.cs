// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Remote;

/// <summary>
/// ICompanyProvider adapter that calls OUR own Web API (in the cloud) instead of Brreg directly.
/// Used when the app runs as a Thin Client — typically for the subscription version in app stores.
/// </summary>
internal sealed class RemoteApiCompanyProvider(
    HttpClient http,
    ILogger<RemoteApiCompanyProvider> logger) : ICompanyProvider
{
    public string CountryCode => "NO";

    // The RemoteApi mode only works when the host's BaseAddress points at a real WebApi.
    public bool IsConfigured => http.BaseAddress is not null;

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not OrganizationNumber org)
        {
            return new CompanyLookupResult.InvalidInput(
                $"RemoteApiCompanyProvider only accepts Norwegian organization numbers (got {id.CountryCode}:{id.Value}).");
        }

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
