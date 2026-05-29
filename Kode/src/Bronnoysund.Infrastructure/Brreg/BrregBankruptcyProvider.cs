// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IBankruptcyProvider"/>. Brreg has no separate per-orgnr
/// bankruptcy endpoint; the truth lives on the entity payload itself
/// (<c>konkurs</c> + <c>konkursdato</c>), already projected into
/// <see cref="Application.Dtos.CompanyResponse.IsBankrupt"/> +
/// <see cref="Application.Dtos.CompanyResponse.BankruptcyDate"/>.
/// </summary>
/// <remarks>
/// Calls <see cref="ICompanyProvider"/> rather than going to the generated Brreg
/// client directly so this lookup shares <c>CachingCompanyProvider</c>'s cache —
/// the parallel aggregator now makes one HTTP request per orgnr instead of two.
/// </remarks>
internal sealed class BrregBankruptcyProvider(
    ICompanyProviderRegistry registry,
    ILogger<BrregBankruptcyProvider> logger) : IBankruptcyProvider
{
    // Same singular-injection trap the aggregators hit (commit e3d010d). After Plan 21
    // Bølge 1-4 registered fourteen international ICompanyProvider implementations,
    // resolving the bare interface picks the last-registered one — typically Serbia —
    // and the bankruptcy lookup returns a "wrong country" error for every Norwegian org.
    // Route through the registry's "NO" entry so we always hit Brreg's cached payload.
    private ICompanyProvider NorwegianCompanies =>
        registry.GetForCountry("NO")
            ?? throw new InvalidOperationException("Norwegian ICompanyProvider is not registered.");

    public async Task<BankruptcyResponse?> GetAsync(OrganizationNumber org, CancellationToken ct)
    {
        var result = await NorwegianCompanies.LookupAsync(org, ct).ConfigureAwait(false);
        return result switch
        {
            CompanyLookupResult.Found f => new BankruptcyResponse(
                IsBankrupt: f.Company.IsBankrupt,
                Declared: f.Company.BankruptcyDate),
            CompanyLookupResult.NotFound => null,
            CompanyLookupResult.Unavailable u =>
                LogAndRethrow(u.Message, org),
            CompanyLookupResult.InvalidInput =>
                null,
            _ => null,
        };
    }

    private BankruptcyResponse LogAndRethrow(string message, OrganizationNumber org)
    {
        // The aggregator's per-provider catch converts this to a RegistryError entry,
        // keeping the rest of the aggregated response intact when only the upstream
        // bankruptcy column is unavailable.
        logger.LogWarning("Brreg bankruptcy lookup unavailable for {OrgNumber}: {Reason}",
            org.Value, message);
        throw new Exceptions.BrregUnavailableException(message);
    }
}
