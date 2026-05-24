// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Ports;

public sealed record RegisterEndpointItem(string Name, string BaseUrl, bool IsEnabled, DateTimeOffset UpdatedAt);

/// <summary>
/// Konfigurerbare URL-er per register. Brukeren kan endre Brreg-URL eller slå av et register
/// uten å oppdatere appen.
/// </summary>
public interface IRegisterEndpointsRepository
{
    Task<RegisterEndpointItem?> GetAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<RegisterEndpointItem>> ListAsync(CancellationToken ct);
    Task UpsertAsync(string name, string baseUrl, bool isEnabled, CancellationToken ct);
    Task RemoveAsync(string name, CancellationToken ct);
}
