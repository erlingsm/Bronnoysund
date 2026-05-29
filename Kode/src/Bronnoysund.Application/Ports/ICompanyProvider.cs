// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;
using Bronnoysund.Domain;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Fetches the core data for a company from a national registry. Adapter implementations live
/// in the Infrastructure layer, one per country. <see cref="CountryCode"/> drives provider
/// selection in <see cref="ICompanyProviderRegistry"/>; implementations return
/// <see cref="CompanyLookupResult.InvalidInput"/> when handed an identifier from a country
/// they don't serve.
/// </summary>
public interface ICompanyProvider
{
    string CountryCode { get; }

    /// <summary>
    /// Whether the adapter has every credential / setting it needs to actually reach its
    /// upstream registry. Norwegian Brreg + Finnish PRH + Irish CKAN + Polish KRS +
    /// Lithuanian data.gov.lt are open APIs that are always configured. The 10 other
    /// adapters delegate to their respective per-country Options' IsConfigured. Plan 26
    /// trinn B2 (UI status-prikk) reads this through <see cref="ICompanyProviderRegistry"/>.
    /// </summary>
    bool IsConfigured { get; }

    Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct);
}
