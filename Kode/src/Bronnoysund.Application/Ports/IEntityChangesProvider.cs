// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Domain;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Provider for the combined "endringslogg" view per entity. Internally fans out
/// to <c>/oppdateringer/enheter</c>, <c>/oppdateringer/underenheter</c>, and
/// <c>/oppdateringer/roller</c> in parallel with per-feed error isolation.
/// </summary>
public interface IEntityChangesProvider
{
    Task<EntityChangesResponse> GetChangesAsync(OrganizationNumber org, int pageSize, CancellationToken ct);
}
