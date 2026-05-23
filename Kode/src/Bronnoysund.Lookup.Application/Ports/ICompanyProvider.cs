// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Domain;

namespace Bronnoysund.Lookup.Application.Ports;

/// <summary>
/// Henter kjernedata for en virksomhet (Brreg Enhetsregisteret).
/// Adapter-implementasjon i Infrastructure-laget.
/// </summary>
public interface ICompanyProvider
{
    Task<CompanyLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct);
}
