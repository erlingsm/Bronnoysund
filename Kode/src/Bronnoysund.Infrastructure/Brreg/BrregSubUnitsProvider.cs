// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="ISubUnitsProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Filters underenheter where <c>overordnetEnhet={orgnr}</c>;
/// page size 100 covers the long tail of Norwegian parent entities. Pagination beyond
/// page 0 is not exposed today — if a parent ever has more than 100 sub-units, follow
/// up by paging the Browse-Underenheter page (Plan 54) instead of expanding the
/// aggregator response.
/// </summary>
internal sealed class BrregSubUnitsProvider(
    BrregClient client,
    ILogger<BrregSubUnitsProvider> logger) : ISubUnitsProvider
{
    private const int PageSize = 100;

    public async Task<SubUnitsResponse?> GetSubUnitsAsync(OrganizationNumber org, CancellationToken ct)
    {
        try
        {
            var page = await client.Enhetsregisteret.Api.Underenheter
                .GetAsync(cfg =>
                {
                    cfg.QueryParameters.OverordnetEnhet = org.Value;
                    cfg.QueryParameters.Size = PageSize;
                }, ct).ConfigureAwait(false);

            var items = (page?.Embedded?.Underenheter ?? [])
                .Where(u => !string.IsNullOrWhiteSpace(u.Organisasjonsnummer))
                .Select(u => new SubUnit(
                    OrganizationNumber: u.Organisasjonsnummer!,
                    Name: u.Navn ?? string.Empty))
                .ToList();

            return new SubUnitsResponse(items);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg Underenheter upstream error for {OrgNumber}", org.Value);
            throw new BrregUnavailableException(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for /underenheter?overordnetEnhet={org.Value}", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg Underenheter transport error for {OrgNumber}", org.Value);
            throw new BrregUnavailableException(
                $"Could not contact Brreg for /underenheter?overordnetEnhet={org.Value}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new BrregUnavailableException(
                $"Brreg did not respond within the timeout for /underenheter?overordnetEnhet={org.Value}", ex);
        }
    }
}
