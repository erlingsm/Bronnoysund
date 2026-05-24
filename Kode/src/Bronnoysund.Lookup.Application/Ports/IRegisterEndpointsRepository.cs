// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Ports;

public sealed record RegisterEndpointItem(string Name, string BaseUrl, bool IsEnabled, DateTimeOffset UpdatedAt);

/// <summary>
/// Configurable URLs per registry. The user can change the Brreg URL or turn off a registry
/// without updating the app.
/// </summary>
public interface IRegisterEndpointsRepository
{
    Task<RegisterEndpointItem?> GetAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<RegisterEndpointItem>> ListAsync(CancellationToken ct);
    Task UpsertAsync(string name, string baseUrl, bool isEnabled, CancellationToken ct);
    Task RemoveAsync(string name, CancellationToken ct);
}
