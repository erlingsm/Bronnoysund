# 13 — Persistens og cache (SQLite + EF Core, fra Fase 1)

Tverrgående plan for lokal databaselagring brukt av MAUI Desktop (Fase 1), Blazor Web (Fase 2), MAUI Mobile (Fase 3) og senere watch-apper indirekte (Fase 5 — via telefon-companion).

## Mål

- Lokal SQLite-database for søkehistorikk, favoritter, persistent cache og brukerinnstillinger
- Komprimert cache (GZip) for å spare diskplass
- Konfigurerbar auto-cleanup av ikke-favoritt-historikk
- Konfigurerbar maks-størrelse på cache med LRU-eviction
- Konfigurasjons-side i UI der bruker kan oppdatere URI til eksterne tjenester

## Teknisk valg

- **SQLite** (Public Domain) — engine
- **Microsoft.Data.Sqlite** (Apache-2.0) — ADO.NET-provider
- **Microsoft.EntityFrameworkCore.Sqlite** (MIT) — ORM med code-first migrations
- **System.IO.Compression.GZipStream** (BCL, MIT) — komprimering
- Alle lisens-kompatible med vår AGPL-3.0 outbound

Begrunnelse i `09-Tredjepartskode-og-kreditering.md`.

## Nytt prosjekt

```text
src/Bronnoysund.Lookup.Infrastructure.Persistence/
├── BronnoysundDbContext.cs              — EF Core DbContext
├── Entities/
│   ├── LookupHistoryEntry.cs
│   ├── FavoriteCompany.cs
│   ├── CacheEntry.cs
│   ├── AppSetting.cs
│   └── RegisterEndpoint.cs
├── Repositories/
│   ├── LookupHistoryRepository.cs       — implements ILookupHistoryRepository
│   ├── FavoritesRepository.cs           — implements IFavoritesRepository
│   ├── SettingsRepository.cs            — implements ISettingsRepository
│   └── RegisterEndpointsRepository.cs   — implements IRegisterEndpointsRepository
├── Cache/
│   ├── SqliteCacheStore.cs              — IDistributedCache-impl med kompresjon
│   ├── GzipCacheSerializer.cs           — komprimerings-wrapper
│   └── CacheEvictionService.cs          — LRU + maks-størrelse + TTL-cleanup
├── Migrations/                           — EF Core code-first migrations
├── PlatformPath/
│   └── (interfaces — implementasjoner i hvert plattform-prosjekt)
└── ServiceCollectionExtensions.cs       — AddBronnoysundPersistence()
```

`Bronnoysund.Lookup.Application` får nye porter:

```csharp
public interface ILookupHistoryRepository { /* ... */ }
public interface IFavoritesRepository { /* ... */ }
public interface ISettingsRepository { /* ... */ }
public interface IRegisterEndpointsRepository { /* ... */ }
public interface IDatabasePathProvider { string GetDatabaseFilePath(); }
```

## Database-skjema (5 tabeller)

### LookupHistory

| Kolonne | Type | Notat |
| --- | --- | --- |
| Id | INTEGER PK AUTOINCREMENT | |
| OrgNumber | TEXT NULL | Normalisert 9-siffer (NULL hvis bare navn-søk) |
| SearchTerm | TEXT NOT NULL | Rå brukerinput |
| SearchedAt | TEXT NOT NULL | ISO 8601 timestamp |
| ResultGzip | BLOB NULL | GZip-komprimert CompanyResponse-JSON |
| IsFavorite | INTEGER NOT NULL DEFAULT 0 | Flagg satt når brukeren markerer favoritt |

Indekser: `SearchedAt`, `OrgNumber`, `IsFavorite`.

### Favorites

| Kolonne | Type | Notat |
| --- | --- | --- |
| OrgNumber | TEXT PK | Normalisert 9-siffer |
| Name | TEXT NOT NULL | Siste kjente navn |
| Note | TEXT NULL | Brukerens egen kommentar |
| AddedAt | TEXT NOT NULL | ISO 8601 |

### CacheEntries

| Kolonne | Type | Notat |
| --- | --- | --- |
| Key | TEXT PK | F.eks. `org:919300388` |
| ValueGzip | BLOB NOT NULL | GZip-komprimert serialisert verdi |
| DecompressedSizeBytes | INTEGER NOT NULL | For maks-størrelse-beregning uten å dekomprimere |
| StoredAt | TEXT NOT NULL | |
| ExpiresAt | TEXT NOT NULL | TTL-utløp |
| LastAccessedAt | TEXT NOT NULL | Oppdateres ved hver read — brukes for LRU-eviction |

Indekser: `LastAccessedAt`, `ExpiresAt`.

### AppSettings

| Kolonne | Type | Notat |
| --- | --- | --- |
| Key | TEXT PK | F.eks. `cache.maxSizeMB`, `history.retentionDays` |
| Value | TEXT NOT NULL | Serialisert som string |
| DataType | TEXT NOT NULL | `string`, `int`, `bool`, `double` — for typesikker rehydrering |
| UpdatedAt | TEXT NOT NULL | |

### RegisterEndpoints

| Kolonne | Type | Notat |
| --- | --- | --- |
| Name | TEXT PK | `Brreg`, `Gjeldsregister`, `Regnskap`, osv. |
| BaseUrl | TEXT NOT NULL | Brukerens overstyring av `appsettings.json`-default |
| IsEnabled | INTEGER NOT NULL DEFAULT 1 | Slå av et register midlertidig |
| UpdatedAt | TEXT NOT NULL | |

## Komprimering (cache)

**Vurdering:**

- Brreg-respons er typisk 2 KB JSON
- GZip gir ~50% reduksjon → 1 KB per entry
- CPU-koste: <1 ms per entry på moderne hardware (negligible)
- For 10.000 cachede selskaper: 20 MB ukomprimert vs 10 MB komprimert
- På mobil med begrenset diskplass og batteri-bekymring: viktigere besparelse
- Kode er trivielt — `GZipStream` er innebygd

**Konklusjon: komprimer alt.**

Implementasjon:

```csharp
internal sealed class GzipCacheSerializer<T>(IHybridCacheSerializer<T> inner) : IHybridCacheSerializer<T>
{
    public T Deserialize(ReadOnlySequence<byte> source)
    {
        using var ms = new MemoryStream(source.ToArray());
        using var gz = new GZipStream(ms, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gz.CopyTo(output);
        return inner.Deserialize(new ReadOnlySequence<byte>(output.ToArray()));
    }

    public void Serialize(T value, IBufferWriter<byte> target)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            var writer = new ArrayBufferWriter<byte>();
            inner.Serialize(value, writer);
            gz.Write(writer.WrittenSpan);
        }
        target.Write(ms.ToArray());
    }
}
```

Registreres via:

```csharp
services.AddHybridCache().AddSerializer<GzipCacheSerializer<CompanyResponse>>();
```

## Auto-cleanup av søkehistorikk

- Standard retention: **30 dager** for ikke-favoritter (konfigurerbar via `AppSettings.history.retentionDays`)
- Favoritter beholdes for alltid
- Cleanup-job:
  - **WebApi / Blazor Server:** `IHostedService` som kjører hver natt kl. 03:00
  - **MAUI Desktop / Mobile:** kjøres `App.OnStart()` async fire-and-forget; også hvis app har vært åpen >24t kontinuerlig

Implementasjon:

```csharp
internal sealed class HistoryCleanupService(
    BronnoysundDbContext db,
    IOptionsMonitor<PersistenceOptions> opts,
    ILogger<HistoryCleanupService> log)
{
    public async Task CleanupAsync(CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow - TimeSpan.FromDays(opts.CurrentValue.History.RetentionDays);
        var deleted = await db.LookupHistory
            .Where(h => !h.IsFavorite && h.SearchedAt < threshold)
            .ExecuteDeleteAsync(ct);
        log.LogInformation("Slettet {Count} historikk-entries eldre enn {Threshold:O}", deleted, threshold);
    }
}
```

## Maks cache-størrelse + eviction

- Standard maks: **50 MB** (konfigurerbar via `AppSettings.cache.maxSizeMB`)
- Strategi: **LRU** (Least Recently Used) — slettes entries med lavest `LastAccessedAt` til totalsum < 90% av maks
- Trigger:
  - **Skrive-trigger:** ved hver cache-write som overskrider maks, kjør eviction
  - **Periodisk:** samme cleanup-service som historikk; kjør også TTL-utløpte (`ExpiresAt < now`)

Implementasjon:

```csharp
internal sealed class CacheEvictionService(
    BronnoysundDbContext db,
    IOptionsMonitor<PersistenceOptions> opts)
{
    public async Task EvictIfNeededAsync(CancellationToken ct)
    {
        // 1. Fjern utløpte entries
        await db.CacheEntries
            .Where(c => c.ExpiresAt < DateTimeOffset.UtcNow)
            .ExecuteDeleteAsync(ct);

        // 2. Sjekk total størrelse, fjern LRU til 90% av maks
        var maxBytes = opts.CurrentValue.Cache.MaxSizeMB * 1024L * 1024L;
        var target = (long)(maxBytes * 0.9);

        var totalSize = await db.CacheEntries.SumAsync(c => (long)c.DecompressedSizeBytes, ct);
        if (totalSize <= maxBytes) return;

        // Hent oldest first, slett til vi er under target
        var entriesByOldest = await db.CacheEntries
            .OrderBy(c => c.LastAccessedAt)
            .Select(c => new { c.Key, c.DecompressedSizeBytes })
            .ToListAsync(ct);

        long runningTotal = totalSize;
        var toDelete = new List<string>();
        foreach (var entry in entriesByOldest)
        {
            if (runningTotal <= target) break;
            toDelete.Add(entry.Key);
            runningTotal -= entry.DecompressedSizeBytes;
        }

        await db.CacheEntries
            .Where(c => toDelete.Contains(c.Key))
            .ExecuteDeleteAsync(ct);
    }
}
```

## Konfigurasjons-side (UI)

Razor-komponent i `Bronnoysund.Lookup.Components/Pages/Settings.razor` — brukes både i MAUI Desktop, MAUI Mobile og Blazor Web.

**Funksjonalitet:**

- Vis alle nåværende settings + hjelpetekst per felt
- Lar bruker endre:
  - Brreg base-URL (default `https://data.brreg.no/enhetsregisteret/api`)
  - Cache TTL i timer (default 24)
  - Maks cache-størrelse i MB (default 50)
  - Historikk retention-dager (default 30)
  - Senere: URL til gjeldsregister, regnskapsregister, etc. (via `RegisterEndpoints`-tabell)
- "Tilbakestill til defaults"-knapp
- "Eksporter innstillinger" (JSON-fil)
- "Importer innstillinger" (JSON-fil) — for å flytte mellom enheter

**Live-reload via `IOptionsMonitor<T>`:**

Vi bruker en custom `IConfigurationSource` som leser fra `AppSettings`-tabellen og merger over `appsettings.json`-defaults:

```csharp
public static IConfigurationBuilder AddSqliteSettings(
    this IConfigurationBuilder builder, string connectionString)
{
    builder.Add(new SqliteSettingsConfigurationSource(connectionString));
    return builder;
}
```

Når bruker endrer en setting og lagrer, kalles `IConfigurationRoot.Reload()` → `IOptionsMonitor<T>.OnChange` trigger → alle injiserte `IOptionsMonitor<BrregOptions>` får ny verdi automatisk. Ingen restart kreves.

## Cross-platform: database-path

Implementer `IDatabasePathProvider` per host-prosjekt:

| Host | Implementasjon | Resulterende sti |
| --- | --- | --- |
| MAUI (alle plattformer) | `MauiDatabasePathProvider` bruker `FileSystem.AppDataDirectory` | iOS app-sandbox / Android `/data/data/<pkg>/files/` / Mac/Win `~/AppData/Local/` |
| Blazor Web (Server) | `ServerDatabasePathProvider` bruker `SpecialFolder.LocalApplicationData` | `~/AppData/Local/Bronnoysund.Lookup/bronnoysund.db` |
| WebApi (standalone) | Samme som Server | Samme |

Microsoft.Data.Sqlite bundler riktig native SQLite-binær for hver RID automatisk via NuGet.

## Migrations-strategi

- EF Core code-first migrations: `dotnet ef migrations add InitialCreate` etc.
- Ved app-start: kjør `db.Database.MigrateAsync()` — applies pending migrations automatisk
- Migrations checkes inn i `src/Bronnoysund.Lookup.Infrastructure.Persistence/Migrations/`
- Aldri redigér eksisterende migration etter merge — lag en ny

## DI-oppsett (`AddBronnoysundPersistence`)

```csharp
public static IServiceCollection AddBronnoysundPersistence(
    this IServiceCollection services,
    IConfiguration config)
{
    services.AddOptions<PersistenceOptions>().Bind(config.GetSection("Persistence")).ValidateOnStart();

    services.AddDbContext<BronnoysundDbContext>((sp, opts) =>
    {
        var pathProvider = sp.GetRequiredService<IDatabasePathProvider>();
        opts.UseSqlite($"Data Source={pathProvider.GetDatabaseFilePath()}");
    });

    services.AddScoped<ILookupHistoryRepository, LookupHistoryRepository>();
    services.AddScoped<IFavoritesRepository, FavoritesRepository>();
    services.AddScoped<ISettingsRepository, SettingsRepository>();
    services.AddScoped<IRegisterEndpointsRepository, RegisterEndpointsRepository>();

    services.AddSingleton<CacheEvictionService>();
    services.AddHostedService<PersistenceMaintenanceHostedService>(); // kjører cleanup + eviction periodisk

    return services;
}
```

Hver host-prosjekt registrerer sin egen `IDatabasePathProvider` før de kaller `AddBronnoysundPersistence`.

## Tester

I `tests/Bronnoysund.Lookup.Infrastructure.Persistence.Tests/`:

- `BronnoysundDbContextTests` — bruker EF Core InMemory eller SQLite in-memory (`:memory:`)
- `LookupHistoryRepositoryTests` — add/query/delete-roundtrips
- `FavoritesRepositoryTests` — markering, fjerning
- `CacheEvictionServiceTests` — verifiser LRU + maks-størrelse
- `HistoryCleanupServiceTests` — verifiser at favoritter overlever, ikke-favoritter slettes
- `GzipCacheSerializerTests` — round-trip + komprimering-ratio
- `SqliteSettingsConfigurationProviderTests` — settings-endring → `IOptionsMonitor.OnChange` trigger

## Når lages hva (per fase)

| Fase | Hva legges til |
| --- | --- |
| Fase 0 (MVP) | Ingen persistens — HybridCache L1-only, holder oppgavekravet |
| Fase 1 (MAUI Desktop) | Hele `Infrastructure.Persistence`-prosjektet + 5 tabeller + Settings-page i UI + cleanup-service. Komprimering aktivt fra start. |
| Fase 2 (Blazor Web) | Bruker samme persistens-prosjekt; egen `ServerDatabasePathProvider` |
| Fase 3 (MAUI Mobile) | Arver persistens automatisk via delt prosjekt; `MauiDatabasePathProvider` finnes allerede fra Fase 1 |
| Fase 4 (Application-utvidelser) | `RegisterEndpoints`-tabellen tas i bruk når flere registre legges til |
| Fase 5 (Watch) | Watch har ingen egen DB — bruker telefonens via companion-link |
| Fase 6 (Sentral backend, opsjonell) | Kan migreres til SQL Server / Postgres via EF Core provider-swap, eller beholde SQLite serverside |

## Open questions

- Skal eksport/import av settings være kryptert (med passord) hvis vi senere lagrer noe sensitivt? Per nå: nei, alt er ikke-sensitivt.
- Skal vi ha en "Tøm alt"-knapp (wipe DB) som backup-løsning ved problemer? Sannsynligvis ja.
- Skal historikk-snapshot (`ResultGzip`) lagre full Brreg-respons eller bare `CompanyResponse`-DTO? Anbefaling: bare `CompanyResponse` for å minimere størrelse — full respons kan re-hentes hvis ønskelig.
