// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Application.UseCases.LookupCompany;

/// <summary>
/// Country-agnostic use case handler for company-identifier lookups. Detects which national
/// registry the raw input belongs to via <see cref="ICountryDetector"/>, then dispatches to
/// the matching <see cref="ICompanyProvider"/> in the
/// <see cref="ICompanyProviderRegistry"/>. Returns a type-safe
/// <see cref="CompanyLookupResult"/> — no exceptions for business outcomes.
/// </summary>
public sealed class LookupCompanyHandler(
    ICountryDetector detector,
    ICompanyProviderRegistry registry,
    ILogger<LookupCompanyHandler> logger)
{
    public async Task<CompanyLookupResult> HandleAsync(LookupCompanyQuery query, CancellationToken ct)
    {
        var identifier = detector.Detect(query.OrganizationNumberInput);
        if (identifier is null)
        {
            logger.LogInformation("Country detection failed for input '{Input}'", query.OrganizationNumberInput);
            return new CompanyLookupResult.InvalidInput(
                $"Could not detect a known company-identifier format in '{query.OrganizationNumberInput}'. " +
                "Try prefixing with the country code (e.g. SE for Sweden, EE for Estonia).");
        }

        var provider = registry.GetForCountry(identifier.CountryCode);
        if (provider is null)
        {
            logger.LogWarning("No provider registered for detected country {Country}", identifier.CountryCode);
            return new CompanyLookupResult.Unavailable(
                $"No provider is registered for country {identifier.CountryCode}.");
        }

        logger.LogInformation("Looking up {Country}:{Id} via {Provider}",
            identifier.CountryCode, identifier.Value, provider.GetType().Name);
        return await provider.LookupAsync(identifier, ct);
    }
}
