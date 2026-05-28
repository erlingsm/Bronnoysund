// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Lookup table from ISO 3166-1 alpha-2 country code to the <see cref="ICompanyProvider"/>
/// registered for that country. Populated from <c>IEnumerable&lt;ICompanyProvider&gt;</c> via DI;
/// callers (typically a router fronting all registries) use <see cref="GetForCountry"/> to pick
/// the provider that matches a detected <c>CompanyIdentifier.CountryCode</c>.
/// </summary>
public interface ICompanyProviderRegistry
{
    ICompanyProvider? GetForCountry(string countryCode);

    IReadOnlyCollection<string> SupportedCountries { get; }
}
