// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bronnoysund.Infrastructure.Persistence.Repositories;

internal sealed class RegisterEndpointsRepository(BronnoysundDbContext db) : IRegisterEndpointsRepository
{
    public async Task<RegisterEndpointItem?> GetAsync(string name, CancellationToken ct)
    {
        var e = await db.RegisterEndpoints.AsNoTracking().FirstOrDefaultAsync(x => x.Name == name, ct);
        return e is null ? null : new RegisterEndpointItem(e.Name, e.BaseUrl, e.IsEnabled, e.UpdatedAt);
    }

    public async Task<IReadOnlyList<RegisterEndpointItem>> ListAsync(CancellationToken ct)
    {
        var rows = await db.RegisterEndpoints.AsNoTracking().OrderBy(e => e.Name).ToListAsync(ct);
        return rows.ConvertAll(e => new RegisterEndpointItem(e.Name, e.BaseUrl, e.IsEnabled, e.UpdatedAt));
    }

    public async Task UpsertAsync(string name, string baseUrl, bool isEnabled, CancellationToken ct)
    {
        var existing = await db.RegisterEndpoints.FindAsync([name], ct);
        if (existing is null)
        {
            db.RegisterEndpoints.Add(new RegisterEndpoint
            {
                Name = name,
                BaseUrl = baseUrl,
                IsEnabled = isEnabled,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existing.BaseUrl = baseUrl;
            existing.IsEnabled = isEnabled;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string name, CancellationToken ct)
    {
        await db.RegisterEndpoints.Where(e => e.Name == name).ExecuteDeleteAsync(ct);
    }
}
