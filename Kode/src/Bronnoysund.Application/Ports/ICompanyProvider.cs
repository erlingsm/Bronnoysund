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

    Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct);
}
