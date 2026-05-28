// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;
using Bronnoysund.Domain;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Provider for <c>/roller/enheter/{orgnr}/juridiskeroller</c> — legal roles a
/// subject organization holds in other organizations (deltaker, regnskapsfører,
/// revisor, etc.). Different from <see cref="IRolesProvider"/>, which lists the
/// person-roles inside the subject entity.
/// </summary>
public interface ILegalRolesProvider
{
    Task<LegalRolesLookupResult> GetLegalRolesAsync(OrganizationNumber org, CancellationToken ct);
}
