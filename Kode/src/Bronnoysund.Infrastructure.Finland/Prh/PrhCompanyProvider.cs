// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.International;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Finland.Generated;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace Bronnoysund.Infrastructure.Finland.Prh;

/// <summary>
/// Adapter for <see cref="ICompanyProvider"/> backed by the Kiota-generated
/// <see cref="PrhClient"/>. Maps Finnish PRH/YTJ <c>Company</c> data to the country-agnostic
/// <see cref="CompanyResponse"/>. Mirrors <c>BrregCompanyProvider</c>'s error-handling
/// shape so the rest of the stack (caching, aggregator, UI) treats Finland exactly like
/// Norway.
/// </summary>
internal sealed class PrhCompanyProvider(
    PrhClient client,
    ILogger<PrhCompanyProvider> logger) : ICompanyProvider
{
    public string CountryCode => "FI";

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not FinnishBusinessId fi)
        {
            return new CompanyLookupResult.InvalidInput(
                $"PrhCompanyProvider only accepts Finnish Business IDs (got {id.CountryCode}:{id.Value}).");
        }

        return await ProviderExceptionTranslator.CatchUpstreamAsync(async () =>
        {
            try
            {
                // PRH's /companies endpoint is a search — a businessId filter yields zero
                // or one result. The dashed and the digits-only forms are both accepted; we
                // send the dashed normalised form for traceability in logs.
                var response = await client.Companies.GetAsync(req =>
                {
                    req.QueryParameters.BusinessId = fi.Value;
                }, ct).ConfigureAwait(false);

                var company = response?.Companies?.FirstOrDefault();
                if (company is null)
                {
                    return new CompanyLookupResult.NotFound(fi.Value);
                }

                var mapped = PrhMapper.Map(company);
                return mapped is null
                    ? new CompanyLookupResult.Unavailable("PRH returned an unexpected response structure.")
                    : new CompanyLookupResult.Found(mapped);
            }
            catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound)
            {
                // Kiota-specific — the shared translator doesn't know about ApiException.
                logger.LogInformation("PRH returned 404 for {BusinessId}", fi.Value);
                return new CompanyLookupResult.NotFound(fi.Value);
            }
            catch (ApiException ex)
            {
                logger.LogWarning(ex, "PRH upstream error for {BusinessId}", fi.Value);
                return new CompanyLookupResult.Unavailable(
                    $"PRH returned HTTP {ex.ResponseStatusCode} for /companies?businessId={fi.Value}");
            }
        }, "Finland (PRH/YTJ)", fi.Value, logger, ct).ConfigureAwait(false);
    }
}
