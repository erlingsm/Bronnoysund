// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IBankruptcyProvider"/>. Brreg has no separate per-orgnr bankruptcy
/// endpoint; the truth lives on the entity payload itself (<c>konkurs</c> + <c>konkursdato</c>).
/// HybridCache deduplicates the second hit on <c>/enheter/{orgnr}</c> made by the aggregator,
/// so the cost of asking via this provider on top of the core lookup is L1-only.
/// </summary>
internal sealed class BrregBankruptcyProvider(
    BrregClient client,
    ILogger<BrregBankruptcyProvider> logger) : IBankruptcyProvider
{
    public async Task<BankruptcyResponse?> GetAsync(OrganizationNumber org, CancellationToken ct)
    {
        try
        {
            var response = await client.Enhetsregisteret.Api.Enheter[org.Value]
                .GetAsWithEnhetorgnrGetResponseAsync(cancellationToken: ct).ConfigureAwait(false);
            if (response?.Enhet is not { } enhet)
            {
                return null;
            }

            DateOnly? declared = enhet.Konkursdato is { } d
                ? new DateOnly(d.Year, d.Month, d.Day)
                : null;

            return new BankruptcyResponse(IsBankrupt: enhet.Konkurs == true, Declared: declared);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound ||
                                       ex.ResponseStatusCode == (int)HttpStatusCode.Gone)
        {
            return null;
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg bankruptcy upstream error for {OrgNumber}", org.Value);
            throw new BrregUnavailableException(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for /enheter/{org.Value}", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg bankruptcy transport error for {OrgNumber}", org.Value);
            throw new BrregUnavailableException(
                $"Could not contact Brreg for /enheter/{org.Value}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new BrregUnavailableException(
                $"Brreg did not respond within the timeout for /enheter/{org.Value}", ex);
        }
    }
}
