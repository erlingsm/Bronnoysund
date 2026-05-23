# 02 — Fase 1: MAUI Desktop (Mac + Windows)

**Forutsetning:** Fase 0 ferdig.

## Mål

En desktop-applikasjon som bygges fra én kodebase og distribueres til både macOS og Windows. Bruker `Brreg.Application`/`Brreg.Infrastructure` direkte (fat client) og presenterer via Blazor-komponenter inni MAUI Blazor Hybrid.

## Nye prosjekter

- `src/Brreg.Components/` — Razor Class Library (RCL) med delte `.razor`-komponenter
- `src/Brreg.ViewModels/` — `net10.0` klassebibliotek med `CommunityToolkit.Mvvm` ViewModels
- `src/Brreg.Resources/` — strenger (`.resx`), bilder
- `src/Brreg.MauiDesktop/` — `net10.0-maccatalyst;net10.0-windows10.0.19041.0` multi-target

## Komponenter (RCL)

- `OrgNumberInput.razor` — input + validering-feedback
- `CompanyCard.razor` — vis 4 engelsk-felt + språk-flagg
- `ErrorPanel.razor` — viser InvalidInput / NotFound / Unavailable
- `Layout/MainLayout.razor`

## ViewModel

- `CompanyLookupViewModel` med `OrgNumber`-input-property, `LookupCommand` (RelayCommand), `Result`-property som komponenten databinder mot.

## DI-oppsett i MAUI

```csharp
builder.Services
    .AddBrregApplication()
    .AddBrregInfrastructure(builder.Configuration)
    .AddTransient<CompanyLookupViewModel>();
builder.Services.AddMauiBlazorWebView();
```

## Verifikasjon

- App starter på Mac (Apple Silicon) og Windows (Parallels eller VM hvis Mac-only)
- Manuell test: skriv inn 919300388 → ser engelsk respons
- Ugyldig input → ser feilmelding
- Re-lookup samme orgnr → ser cache-hit i logg

## Plattform-spesifikt

Skal være minst mulig — kun nødvendig kode i `Platforms/MacCatalyst/` og `Platforms/Windows/` for app-bootstrap.

## Distribusjon (portable, se `10-Distribusjon-og-portability.md`)

- **Windows:** Unpackaged-bygg med `WindowsPackageType=None` → mappe med `.exe + DLLs` som ZIP-pakkes. Kjøres fra hvor som helst (også USB).
- **Mac:** Catalyst `.app`-bundle ZIP-pakket. Drag-drop til Applications *eller* kjør fra USB.
- README: `/docs/install/desktop.md` (krever .NET 10 Runtime; self-contained variant tilbys som ekstra ZIP).

## Persistens (SQLite + EF Core, fra denne fasen)

Hele persistens-arkitekturen introduseres i Fase 1 og arves av alle senere faser. Detaljert i `13-Persistens-og-cache.md`.

Konkret for Fase 1:

- Nytt prosjekt `src/Bronnoysund.Lookup.Infrastructure.Persistence/` med EF Core + SQLite
- 5 tabeller: `LookupHistory`, `Favorites`, `CacheEntries`, `AppSettings`, `RegisterEndpoints`
- Komprimert cache (GZip) — sparer ~50% diskplass
- Konfigurerbar auto-cleanup av ikke-favoritt-historikk (default 30 dager)
- Konfigurerbar maks cache-størrelse med LRU-eviction (default 50 MB)
- Konfigurasjons-side i UI (`Bronnoysund.Lookup.Components/Pages/Settings.razor`) der bruker kan endre Brreg base-URL, cache TTL, maks-størrelse, retention-dager
- Live-reload av settings via `IOptionsMonitor<T>` — ingen app-restart kreves
- `MauiDatabasePathProvider` plasserer DB-fila via `FileSystem.AppDataDirectory` (gjenbrukes uendret i Fase 3 Mobile)
- EF Core code-first migrations kjøres automatisk ved app-start

## Open questions

- Skal komponentbiblioteket basere seg på MudBlazor eller noe annet?
