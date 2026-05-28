// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;
using Bronnoysund.Domain;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Provider for <c>/frivillighetsregisteret/api/frivillige-organisasjoner/{orgnr}</c>.
/// Used by the Lookup page to render a "frivillig organisasjon"-badge when relevant.
/// </summary>
public interface IVoluntaryOrganizationProvider
{
    Task<VoluntaryOrganizationLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct);
}
