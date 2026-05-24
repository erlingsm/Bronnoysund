// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Domain;
using Bronnoysund.Lookup.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

/// <summary>
/// ISubUnitsProvider adapter for Brreg's open underenheter listing. The endpoint paginates
/// — we ask for size=100 which covers almost every Norwegian parent. If we later hit a
/// parent with more than 100 sub-units in practice, page through the HAL links.
/// </summary>
internal sealed class BrregSubUnitsProvider(
    BrregHttpClient http,
    ILogger<BrregSubUnitsProvider> logger) : ISubUnitsProvider
{
    public async Task<SubUnitsResponse?> GetSubUnitsAsync(OrganizationNumber org, CancellationToken ct)
    {
        try
        {
            var page = await http.GetUnderenheterAsync(org, ct);
            if (page is null)
            {
                return null;
            }

            var items = page.Embedded?.Underenheter ?? [];
            var subUnits = items
                .Where(u => !string.IsNullOrWhiteSpace(u.Organisasjonsnummer))
                .Select(u => new SubUnit(
                    OrganizationNumber: u.Organisasjonsnummer!,
                    Name: u.Navn ?? string.Empty))
                .ToList();

            return new SubUnitsResponse(subUnits);
        }
        catch (BrregUnavailableException ex)
        {
            logger.LogWarning(ex, "Brreg Underenheter unavailable for {OrgNumber}", org.Value);
            throw;
        }
    }
}
