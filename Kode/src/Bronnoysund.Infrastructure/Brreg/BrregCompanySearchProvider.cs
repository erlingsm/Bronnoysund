// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="ICompanySearchProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Supports pagination via Brreg's <c>page</c>/<c>size</c> query
/// parameters. Returns <see cref="CompanySearchResult"/> with both the hit window and total
/// element count so callers can render "X of Y" + a MudPagination control.
/// </summary>
internal sealed class BrregCompanySearchProvider(
    BrregClient client,
    ILogger<BrregCompanySearchProvider> logger) : ICompanySearchProvider
{
    public async Task<CompanySearchResult> SearchByNameAsync(string query, int maxResults, CancellationToken ct)
    {
        var paged = await SearchByNameAsync(query, new PagedRequest(Page: 0, PageSize: maxResults), ct)
            .ConfigureAwait(false);
        return new CompanySearchResult(paged.Items, paged.TotalElements);
    }

    public async Task<PagedResult<CompanySearchHit>> SearchByNameAsync(
        string query, PagedRequest paging, CancellationToken ct)
    {
        try
        {
            var response = await client.Enhetsregisteret.Api.Enheter
                .GetAsync(cfg =>
                {
                    cfg.QueryParameters.Navn = query;
                    cfg.QueryParameters.Page = paging.Page;
                    cfg.QueryParameters.Size = paging.PageSize;
                }, ct).ConfigureAwait(false);

            var hits = (response?.Embedded?.Enheter ?? [])
                .Where(e => !string.IsNullOrWhiteSpace(e.Organisasjonsnummer))
                .Select(e => new CompanySearchHit(
                    OrganizationNumber: e.Organisasjonsnummer!,
                    Name: e.Navn ?? string.Empty,
                    OrganizationFormCode: e.Organisasjonsform?.Kode ?? string.Empty,
                    PostalCity: e.Postadresse?.Poststed))
                .ToList();

            var totalElements = (int?)response?.Page?.TotalElements ?? hits.Count;
            var totalPages = (int?)response?.Page?.TotalPages ?? (hits.Count == 0 ? 0 : 1);

            return new PagedResult<CompanySearchHit>(
                Items: hits,
                Page: paging.Page,
                PageSize: paging.PageSize,
                TotalElements: totalElements,
                TotalPages: totalPages);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound)
        {
            // 404 on search means "no results" — return empty page, not an error.
            return new PagedResult<CompanySearchHit>([], paging.Page, paging.PageSize, 0, 0);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg search upstream error for '{Query}'", query);
            throw new BrregUnavailableException(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for /enheter?navn={query}", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg search transport error for '{Query}'", query);
            throw new BrregUnavailableException(
                $"Could not contact Brreg for /enheter?navn={query}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new BrregUnavailableException(
                $"Brreg did not respond within the timeout for /enheter?navn={query}", ex);
        }
    }
}
