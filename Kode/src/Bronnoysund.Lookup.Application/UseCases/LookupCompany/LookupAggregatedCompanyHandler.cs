// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Domain;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Application.UseCases.LookupCompany;

/// <summary>
/// Use-case handler for "give me everything you can about this organization". Validates the
/// input, queries the core registry first via <see cref="ICompanyDataAggregator.CoreOnlyAsync"/>
/// to short-circuit not-found / invalid / unavailable, and only runs the full parallel
/// aggregation when the core lookup succeeded. Keeps the original
/// <see cref="LookupCompanyHandler"/> in place for callers that only need the four MVP fields.
/// </summary>
public sealed class LookupAggregatedCompanyHandler(
    ICompanyDataAggregator aggregator,
    ILogger<LookupAggregatedCompanyHandler> logger)
{
    public async Task<AggregatedLookupResult> HandleAsync(LookupCompanyQuery query, CancellationToken ct)
    {
        if (!OrganizationNumber.TryCreate(query.OrganizationNumberInput, out var orgNumber, out var error))
        {
            logger.LogInformation("Validation failed for aggregated lookup '{Input}': {Error}",
                query.OrganizationNumberInput, error);
            return new AggregatedLookupResult.InvalidInput(error ?? "Invalid organization number.");
        }

        var core = await aggregator.CoreOnlyAsync(orgNumber, ct);
        switch (core)
        {
            case CompanyLookupResult.NotFound nf:
                return new AggregatedLookupResult.NotFound(nf.OrganizationNumber);
            case CompanyLookupResult.InvalidInput inv:
                return new AggregatedLookupResult.InvalidInput(inv.Message);
            case CompanyLookupResult.Unavailable unav:
                return new AggregatedLookupResult.Unavailable(unav.Message);
            case CompanyLookupResult.Found:
                logger.LogInformation("Aggregating registries for {OrgNumber}", orgNumber.Value);
                var aggregated = await aggregator.AggregateAsync(orgNumber, ct);
                return new AggregatedLookupResult.Found(aggregated);
            default:
                return new AggregatedLookupResult.Unavailable("Unexpected result type from core registry.");
        }
    }
}

public abstract record AggregatedLookupResult
{
    public sealed record Found(AggregatedCompanyResponse Data) : AggregatedLookupResult;
    public sealed record NotFound(string OrganizationNumber) : AggregatedLookupResult;
    public sealed record InvalidInput(string Message) : AggregatedLookupResult;
    public sealed record Unavailable(string Message) : AggregatedLookupResult;
}
