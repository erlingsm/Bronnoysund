// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Domain;
using Bronnoysund.Lookup.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

/// <summary>
/// IBankruptcyProvider derived from the entity record's `konkurs`/`konkursdato` fields.
/// Brreg does not expose a separate per-orgnr bankruptcy endpoint; the truth lives on the
/// entity payload itself. This means we hit /enheter/{orgnr} twice during parallel aggregation
/// (once for core, once for bankruptcy) — HybridCache deduplicates so the second is L1-hit.
/// </summary>
internal sealed class BrregBankruptcyProvider(
    BrregHttpClient http,
    ILogger<BrregBankruptcyProvider> logger) : IBankruptcyProvider
{
    public async Task<BankruptcyResponse?> GetAsync(OrganizationNumber org, CancellationToken ct)
    {
        try
        {
            var dto = await http.GetEnhetAsync(org, ct);
            if (dto is null)
            {
                return null;
            }

            DateOnly? declared = null;
            if (!string.IsNullOrWhiteSpace(dto.Konkursdato) &&
                DateOnly.TryParseExact(dto.Konkursdato, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d))
            {
                declared = d;
            }

            return new BankruptcyResponse(IsBankrupt: dto.Konkurs, Declared: declared);
        }
        catch (BrregUnavailableException ex)
        {
            logger.LogWarning(ex, "Brreg bankruptcy lookup unavailable for {OrgNumber}", org.Value);
            throw;
        }
    }
}
