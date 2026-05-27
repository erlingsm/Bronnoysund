// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Free-text name search against the source registry. Returns a paginated list of hits;
/// the caller is expected to follow up with a per-orgnr lookup on the chosen entry.
/// Separate from <see cref="ICompanyProvider"/> because the responses, caching strategy,
/// and consumer flow are all different.
/// </summary>
public interface ICompanySearchProvider
{
    /// <summary>
    /// Legacy single-page search. Returns the first <paramref name="maxResults"/> hits plus
    /// the source registry's total element count. Prefer <see cref="SearchByNameAsync(string, PagedRequest, CancellationToken)"/>
    /// in new code that needs browse-through-pages.
    /// </summary>
    Task<CompanySearchResult> SearchByNameAsync(string query, int maxResults, CancellationToken ct);

    /// <summary>
    /// Paginated search. Page is 0-based to match Brreg's HAL convention; UI converts to
    /// 1-based for display. Plan 54's Browse-page exercises this path.
    /// </summary>
    Task<PagedResult<CompanySearchHit>> SearchByNameAsync(string query, PagedRequest paging, CancellationToken ct);
}

/// <summary>One row in a name-search result list.</summary>
public sealed record CompanySearchHit(
    string OrganizationNumber,
    string Name,
    string OrganizationFormCode,
    string? PostalCity);

/// <summary>
/// A page of search hits. TotalElements reflects the registry's full result count so the UI
/// can hint the user to narrow the query when only the first N of M are shown.
/// </summary>
public sealed record CompanySearchResult(
    IReadOnlyList<CompanySearchHit> Hits,
    int TotalElements);
