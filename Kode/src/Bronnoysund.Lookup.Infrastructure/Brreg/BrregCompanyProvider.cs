// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Dtos;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Domain;
using Bronnoysund.Lookup.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

/// <summary>
/// Adapter that implements <see cref="ICompanyProvider"/> via the Brreg Enhetsregisteret.
/// Maps the Brreg DTO to our domain entity and then to the English-field
/// <see cref="CompanyResponse"/>.
/// </summary>
internal sealed class BrregCompanyProvider(
    BrregHttpClient httpClient,
    ILogger<BrregCompanyProvider> logger) : ICompanyProvider
{
    public async Task<CompanyLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct)
    {
        try
        {
            var dto = await httpClient.GetEnhetAsync(org, ct);
            if (dto is null)
            {
                return new CompanyLookupResult.NotFound(org.Value);
            }

            var domain = dto.ToDomain();
            if (domain is null)
            {
                logger.LogWarning("Brreg returned a response for {OrgNumber} but mapping did not produce a valid Company", org.Value);
                return new CompanyLookupResult.Unavailable(
                    "Brreg returned an unexpected response structure.");
            }

            var response = new CompanyResponse(
                OrganizationNumber: domain.OrganizationNumber.Value,
                OrganizationName: domain.Name,
                CompanyType: domain.OrganizationFormCode,
                LanguageForm: domain.LanguageForm.ToString()
            );
            return new CompanyLookupResult.Found(response);
        }
        catch (BrregUnavailableException ex)
        {
            logger.LogWarning(ex, "Brreg unavailable for {OrgNumber}", org.Value);
            return new CompanyLookupResult.Unavailable(ex.Message);
        }
    }
}
