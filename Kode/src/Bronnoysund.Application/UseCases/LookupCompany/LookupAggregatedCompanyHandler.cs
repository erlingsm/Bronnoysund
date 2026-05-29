// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Application.UseCases.LookupCompany;

/// <summary>
/// Country-aware "give me everything you can about this organization" handler. Routes
/// Norwegian identifiers through <see cref="ICompanyDataAggregator"/> for the full
/// parallel fan-out (Brreg roles, sub-units, voluntary org, etc.). Routes international
/// identifiers through <see cref="ICompanyProviderRegistry"/> and returns the core
/// <see cref="CompanyResponse"/> alone — enrichment providers for those registries are
/// not implemented yet (Plan 25 covers the cross-country roles/UBO work separately).
/// </summary>
public sealed class LookupAggregatedCompanyHandler(
    ICountryDetector detector,
    ICompanyProviderRegistry registry,
    ICompanyDataAggregator aggregator,
    IProviderHealthTracker health,
    ILookupMetrics metrics,
    ILogger<LookupAggregatedCompanyHandler> logger)
{
    public async Task<AggregatedLookupResult> HandleAsync(LookupCompanyQuery query, CancellationToken ct)
    {
        var identifier = detector.Detect(query.OrganizationNumberInput);
        if (identifier is null)
        {
            logger.LogInformation("Country detection failed for aggregated lookup '{Input}'",
                query.OrganizationNumberInput);
            return new AggregatedLookupResult.InvalidInput(
                $"Could not detect a known company-identifier format in '{query.OrganizationNumberInput}'.");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        AggregatedLookupResult outcome;
        if (identifier is OrganizationNumber norwegian)
        {
            outcome = await HandleNorwegianAsync(norwegian, ct).ConfigureAwait(false);
        }
        else
        {
            outcome = await HandleInternationalAsync(identifier, ct).ConfigureAwait(false);
        }
        sw.Stop();
        metrics.Record(identifier.CountryCode, MapKind(outcome), sw.Elapsed);
        return outcome;
    }

    private static LookupResultKind MapKind(AggregatedLookupResult result) => result switch
    {
        AggregatedLookupResult.Found => LookupResultKind.Found,
        AggregatedLookupResult.NotFound => LookupResultKind.NotFound,
        AggregatedLookupResult.InvalidInput => LookupResultKind.InvalidInput,
        _ => LookupResultKind.Unavailable,
    };

    /// <summary>
    /// Norwegian path keeps the parallel fan-out (Brreg roles, sub-units, voluntary, etc.).
    /// </summary>
    private async Task<AggregatedLookupResult> HandleNorwegianAsync(OrganizationNumber org, CancellationToken ct)
    {
        var core = await aggregator.CoreOnlyAsync(org, ct).ConfigureAwait(false);
        RecordOutcome(org.CountryCode, core);
        switch (core)
        {
            case CompanyLookupResult.NotFound nf:
                return new AggregatedLookupResult.NotFound(nf.OrganizationNumber);
            case CompanyLookupResult.InvalidInput inv:
                return new AggregatedLookupResult.InvalidInput(inv.Message);
            case CompanyLookupResult.Unavailable unav:
                return new AggregatedLookupResult.Unavailable(unav.Message);
            case CompanyLookupResult.Found:
                logger.LogInformation("Aggregating registries for {OrgNumber}", org.Value);
                var aggregated = await aggregator.AggregateAsync(org, ct).ConfigureAwait(false);
                return new AggregatedLookupResult.Found(aggregated);
            default:
                return new AggregatedLookupResult.Unavailable("Unexpected result type from core registry.");
        }
    }

    private void RecordOutcome(string countryCode, CompanyLookupResult result)
    {
        switch (result)
        {
            case CompanyLookupResult.Found:
            case CompanyLookupResult.NotFound:
                health.RecordSuccess(countryCode);
                break;
            case CompanyLookupResult.Unavailable un:
                health.RecordFailure(countryCode, un.Message);
                break;
        }
    }

    /// <summary>
    /// International path returns only the core <see cref="CompanyResponse"/> wrapped in
    /// an <see cref="AggregatedCompanyResponse"/>. Enrichment providers for non-Norwegian
    /// registries are deferred to Plan 25 (international roles/UBO with separate GDPR
    /// considerations per country).
    /// </summary>
    private async Task<AggregatedLookupResult> HandleInternationalAsync(CompanyIdentifier id, CancellationToken ct)
    {
        var provider = registry.GetForCountry(id.CountryCode);
        if (provider is null)
        {
            return new AggregatedLookupResult.Unavailable(
                $"No provider is registered for country {id.CountryCode}.");
        }

        logger.LogInformation("Looking up {Country}:{Id} (core-only — international)", id.CountryCode, id.Value);
        var result = await provider.LookupAsync(id, ct).ConfigureAwait(false);
        RecordOutcome(id.CountryCode, result);
        return result switch
        {
            CompanyLookupResult.Found f => new AggregatedLookupResult.Found(
                new AggregatedCompanyResponse(
                    Core: f.Company,
                    Roles: null,
                    LatestAnnualReport: null,
                    BeneficialOwners: null,
                    Debt: null,
                    Bankruptcy: null,
                    SubUnits: null,
                    Errors: System.Array.Empty<RegistryError>())),
            CompanyLookupResult.NotFound nf => new AggregatedLookupResult.NotFound(nf.OrganizationNumber),
            CompanyLookupResult.InvalidInput inv => new AggregatedLookupResult.InvalidInput(inv.Message),
            CompanyLookupResult.Unavailable un => new AggregatedLookupResult.Unavailable(un.Message),
            _ => new AggregatedLookupResult.Unavailable("Unexpected result type."),
        };
    }
}

public abstract record AggregatedLookupResult
{
    public sealed record Found(AggregatedCompanyResponse Data) : AggregatedLookupResult;
    public sealed record NotFound(string OrganizationNumber) : AggregatedLookupResult;
    public sealed record InvalidInput(string Message) : AggregatedLookupResult;
    public sealed record Unavailable(string Message) : AggregatedLookupResult;
}
