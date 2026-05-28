// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;

namespace Bronnoysund.Infrastructure.Detection;

/// <summary>
/// Default <see cref="ICountryDetector"/>. Recognises the Norwegian 9-digit MOD11
/// organization number and the Finnish 7-digit + dash + check digit Y-tunnus; further
/// international identifier types are added here as their adapter packs come online.
/// Detection order matters: each new pattern must be discriminable from existing ones
/// (length, separator, or prefix) so the same raw input never matches two countries.
/// Today's two are unambiguous — NO is 9 plain digits, FI is 8 digits with a dash in
/// position 8 (and the dash is required to disambiguate from anything else that's exactly
/// 8 digits, e.g. Estonian registrikood).
/// </summary>
internal sealed class CountryDetector : ICountryDetector
{
    public CompanyIdentifier? Detect(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return null;
        }

        if (OrganizationNumber.TryCreate(rawInput, out var organizationNumber, out _))
        {
            return organizationNumber;
        }

        // Require an explicit dash for Finnish IDs so we don't claim every random 8-digit
        // string. PRH itself accepts the dashless form on the wire, but as a user-input
        // signal the dash is the discriminator.
        if (rawInput.Contains('-') &&
            FinnishBusinessId.TryCreate(rawInput, out var finnishBusinessId, out _))
        {
            return finnishBusinessId;
        }

        return null;
    }
}
