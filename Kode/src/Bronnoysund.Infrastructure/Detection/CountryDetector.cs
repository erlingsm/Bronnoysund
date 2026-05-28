// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;

namespace Bronnoysund.Infrastructure.Detection;

/// <summary>
/// Default <see cref="ICountryDetector"/>. Today it only recognises the Norwegian
/// 9-digit MOD11 organization number; international identifier types (Finnish, Estonian,
/// Polish, ...) are added by their respective adapter packs when those providers come online.
/// Detection order matters: each new pattern must be discriminable from existing ones
/// (length, separator, or prefix) so the same raw input never matches two countries.
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

        return null;
    }
}
