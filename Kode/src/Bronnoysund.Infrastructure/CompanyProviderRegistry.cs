// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;

namespace Bronnoysund.Infrastructure;

/// <summary>
/// DI-fed registry over every <see cref="ICompanyProvider"/> registered for the running host.
/// One provider per country code; if two implementations claim the same code, registration
/// fails fast with a descriptive exception (a misconfigured composition root is worse than a
/// crash on startup).
/// </summary>
internal sealed class CompanyProviderRegistry : ICompanyProviderRegistry
{
    private readonly Dictionary<string, ICompanyProvider> _byCountry;

    public CompanyProviderRegistry(IEnumerable<ICompanyProvider> providers)
    {
        _byCountry = new Dictionary<string, ICompanyProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            if (!_byCountry.TryAdd(provider.CountryCode, provider))
            {
                throw new InvalidOperationException(
                    $"Two ICompanyProvider implementations claim country code '{provider.CountryCode}': " +
                    $"{_byCountry[provider.CountryCode].GetType().FullName} and {provider.GetType().FullName}.");
            }
        }
    }

    public ICompanyProvider? GetForCountry(string countryCode) =>
        _byCountry.GetValueOrDefault(countryCode);

    public IReadOnlyCollection<string> SupportedCountries => _byCountry.Keys;
}
