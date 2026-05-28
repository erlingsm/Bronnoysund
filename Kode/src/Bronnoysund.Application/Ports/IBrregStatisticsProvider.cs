// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Aggregate metrics from Brreg's open registers — counts, totals, etc. Currently
/// covers <c>/enhetsregisteret/api/roller/totalbestand</c> (total number of
/// person-roles across all entities); will grow as we expose more bulk endpoints.
/// </summary>
public interface IBrregStatisticsProvider
{
    Task<RolesTotalCountResult> GetRolesTotalCountAsync(CancellationToken ct);
}
