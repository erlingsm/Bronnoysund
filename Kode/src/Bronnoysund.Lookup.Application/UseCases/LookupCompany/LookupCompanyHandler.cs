// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Domain;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Application.UseCases.LookupCompany;

/// <summary>
/// Use case-handler for org.nr-oppslag. Validerer input, bygger <see cref="OrganizationNumber"/>,
/// delegerer til <see cref="ICompanyProvider"/>. Returnerer type-safe
/// <see cref="CompanyLookupResult"/> — ingen exceptions for forretningsfeil.
/// </summary>
public sealed class LookupCompanyHandler(
    ICompanyProvider provider,
    ILogger<LookupCompanyHandler> logger)
{
    public async Task<CompanyLookupResult> HandleAsync(LookupCompanyQuery query, CancellationToken ct)
    {
        if (!OrganizationNumber.TryCreate(query.OrganizationNumberInput, out var orgNumber, out var error))
        {
            logger.LogInformation("Validering feilet for orgnr-input '{Input}': {Error}",
                query.OrganizationNumberInput, error);
            return new CompanyLookupResult.InvalidInput(error ?? "Ugyldig organisasjonsnummer.");
        }

        logger.LogInformation("Slår opp orgnr {OrgNumber}", orgNumber.Value);
        return await provider.LookupAsync(orgNumber, ct);
    }
}
