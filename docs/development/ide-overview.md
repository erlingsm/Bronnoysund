# IDE-mapping og åpning av prosjektet

Bronnoysund.Lookup-solution kan åpnes i flere IDE-er. Bruk **Solution Filter-filer (`.slnf`)** for å laste bare det du trenger.

## Tilgjengelige Solution Filters

| Fil | Åpner | Anbefalt IDE |
| --- | --- | --- |
| `Kode/Bronnoysund.Lookup.sln` | Alt (10 prosjekter + tester) | Rider |
| `Kode/Bronnoysund.Lookup.Core.slnf` | Domain + Application + Infrastructure + WebApi + tester | VS Code / Rider |
| `Kode/Bronnoysund.Lookup.Web.slnf` | Core + Components + ViewModels + BlazorWeb + Speech | Rider / VS Code |
| `Kode/Bronnoysund.Lookup.Desktop.slnf` | Core + Components + ViewModels + MauiDesktop | Rider på Mac, Visual Studio på Windows |
| `Kode/Bronnoysund.Lookup.Mobile.slnf` | Core + Components + ViewModels + Speech + MauiMobile | Rider på Mac |

## JetBrains Rider (Mac + Windows) — anbefalt for de fleste

```bash
# Mac
open -a Rider Kode/Bronnoysund.Lookup.Web.slnf

# eller
Rider Kode/Bronnoysund.Lookup.Web.slnf
```

Hot reload fungerer for Blazor-komponenter. Debug-konfigurasjoner finnes automatisk basert på prosjekt-typer.

## Visual Studio (Windows) — beste valg for MAUI på Win

1. `File → Open → Project/Solution...`
2. Velg `Kode/Bronnoysund.Lookup.sln` eller en `.slnf`
3. Set startup project (høyreklikk → "Set as Startup Project")
4. F5 for å kjøre

## Visual Studio Code (Mac + Windows) — letteste alternativ

**Forutsetninger:**

- Extension: **C# Dev Kit** (Microsoft)
- Extension: **.NET MAUI** (Microsoft) — hvis du jobber med MAUI-prosjekter

**Åpne:**

```bash
code Kode/
```

C# Dev Kit oppdager `.sln` automatisk og viser Solution Explorer. Velg `.slnf` via Solution Explorer-menyen for å filtrere.

Hot reload og debugging via launch-konfigurasjoner i `.vscode/launch.json` (genereres første gang).

## Xcode (Mac) — kun for watch-utvikling (Fase 5)

Når watchOS-appen er bygget (Fase 5):

```bash
open Watch/iOS/BronnoysundWatch.xcodeproj
```

## Android Studio (Mac + Windows) — kun for Wear OS-utvikling (Fase 5)

Når Wear-appen er bygget (Fase 5):

```bash
open -a "Android Studio" Watch/Android/BronnoysundWear/
```

## Anbefalinger per oppgave

| Oppgave | Beste verktøy |
| --- | --- |
| Editere delt kjernebibliotek (Domain/Application) | Rider eller VS Code |
| Utvikle Blazor Web UI | Rider eller VS Code med C# Dev Kit |
| Debugge WebApi | Rider eller Visual Studio |
| Kjøre tester | Rider, VS Code, eller `dotnet test` i terminal |
| MAUI Desktop på Mac | Rider |
| MAUI Mobile iOS-build på Mac | Rider |
| MAUI Mobile Android-build på Win | Visual Studio eller Rider |
| watchOS Swift-utvikling | Xcode (kommer i Fase 5) |
| Wear OS Kotlin-utvikling | Android Studio (kommer i Fase 5) |
| Git og PR | VS Code eller terminal |
| SQLite-inspeksjon | DB Browser for SQLite (gratis app) eller Rider DataGrip-plugin |

## Bygg fra terminal (ingen IDE nødvendig)

```bash
cd Kode
dotnet build                    # bygger alt
dotnet test                     # kjører alle tester
dotnet run --project src/Bronnoysund.Lookup.WebApi
```

## Linter

Markdown-filer i `Plan/` og `Dokumentasjon/` skal være lint-rene:

```bash
npx markdownlint-cli2 "Plan/*.md" "Dokumentasjon/*.md"
```

Forventet output: `Summary: 0 error(s)`.
