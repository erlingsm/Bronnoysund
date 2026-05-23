// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Dtos;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Domain;
using Bronnoysund.Lookup.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

/// <summary>
/// Adapter som implementerer <see cref="ICompanyProvider"/> via Brreg Enhetsregisteret.
/// Mapper Brreg-DTOen til vår domeneentitet og deretter til engelsk-felt
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
                logger.LogWarning("Brreg returnerte respons for {OrgNumber} men mappingen ga ikke en gyldig Company", org.Value);
                return new CompanyLookupResult.Unavailable(
                    "Brreg returnerte en uventet respons-struktur.");
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
            logger.LogWarning(ex, "Brreg utilgjengelig for {OrgNumber}", org.Value);
            return new CompanyLookupResult.Unavailable(ex.Message);
        }
    }
}
