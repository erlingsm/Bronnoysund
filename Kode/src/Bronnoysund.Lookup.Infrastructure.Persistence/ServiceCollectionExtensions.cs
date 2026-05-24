// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bronnoysund.Lookup.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registrerer BronnoysundDbContext + SQLite. Brukeren av extension-metoden må
    /// registrere IDatabasePathProvider på forhånd (typisk MauiDatabasePathProvider
    /// i MAUI, eller DefaultDatabasePathProvider i WebApi/Blazor Server).
    ///
    /// Skjema migreres automatisk ved første kjøring via EnsureCreated() — for Fase 1.
    /// Senere fases vil bruke proper code-first migrations.
    /// </summary>
    public static IServiceCollection AddBronnoysundPersistence(this IServiceCollection services)
    {
        services.AddDbContext<BronnoysundDbContext>((sp, opts) =>
        {
            var pathProvider = sp.GetRequiredService<IDatabasePathProvider>();
            opts.UseSqlite($"Data Source={pathProvider.GetDatabaseFilePath()}");
        });

        return services;
    }
}
