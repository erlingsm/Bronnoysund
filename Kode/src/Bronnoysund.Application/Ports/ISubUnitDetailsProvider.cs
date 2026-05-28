// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;
using Bronnoysund.Domain;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Provider for direct sub-unit lookup against <c>/underenheter/{orgnr}</c>. Distinct
/// from <see cref="ISubUnitsProvider"/> (which lists subunits under a parent) — this
/// one returns the full details of a single underenhet identified by its own org number.
/// </summary>
public interface ISubUnitDetailsProvider
{
    Task<SubUnitLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct);
}
