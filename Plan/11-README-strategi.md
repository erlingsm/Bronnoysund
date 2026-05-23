# 11 — README-strategi

Fra `Oppdrag/ReadMe`: vi trenger mange README-filer. Dette dokumentet kartlegger hva som skal hvor.

## Oversikt

| # | Filsti i repo | Målgruppe | Innhold |
| --- | --- | --- | --- |
| 1 | `/README.md` | Alle | Hovedside med veikart, bakgrunn, lenker til alle andre README-er |
| 2 | `/src/Brreg.WebApi/README.md` | Utvikler | Hvordan kjøre Web API lokalt, eksempel-curl, env-vars |
| 3 | `/docs/install/web-standalone.md` | Sluttbruker | Last ned ZIP, pakk ut, dobbeltklikk — på PC/Mac |
| 4 | `/docs/install/web-server.md` | Sysadmin | Sette opp på Linux/Windows-server m/ .NET 10, systemd, nginx reverse proxy |
| 5 | `/docs/install/desktop.md` | Sluttbruker | Last ned, pakk ut, kjør — portable MAUI på PC/Mac |
| 6 | `/docs/install/android.md` | Sluttbruker | Sideload APK, "ukjente kilder", Play Store-veikart |
| 7 | `/docs/install/ios.md` | Sluttbruker / demo-publikum | Sideload via Xcode eller TestFlight, App Store-veikart |
| 8 | `/docs/install/watch-apple.md` | Sluttbruker | watchOS — installerer automatisk når iPhone-app installeres |
| 9 | `/docs/install/watch-wear.md` | Sluttbruker | Wear OS — installerer fra parret telefon, eller ADB |
| 10 | `/docs/development.md` | Utvikler | Sette opp dev-env, kjøre tester, kode-stil |
| 11 | `/docs/architecture.md` | Utvikler / interessent | Sammendrag av Clean Arch + Ports & Adapters med diagrammer |
| 12 | `/docs/credits.md` | Alle | Kreditering av inspirasjon, lisens-overoversikt |
| 13 | `/docs/development/ide-overview.md` | Utvikler | IDE-mapping per prosjekt (jf. `Plan/14-IDE-og-verktoy-mapping.md`) |
| 14 | `/docs/development/ide-rider.md` | Utvikler | Åpne `.slnf` i JetBrains Rider på Mac/Win |
| 15 | `/docs/development/ide-visual-studio.md` | Utvikler (Windows) | Åpne `.sln` i Visual Studio |
| 16 | `/docs/development/ide-vscode.md` | Utvikler | C# Dev Kit-oppsett, launch.json |
| 17 | `/docs/development/ide-xcode.md` | Utvikler (Mac) | Watch-prosjekt i Xcode (Fase 5) |
| 18 | `/docs/development/ide-android-studio.md` | Utvikler | Wear-prosjekt i Android Studio (Fase 5) |
| 19 | `/docs/development/simulators-setup.md` | Utvikler | iOS-simulator + Android-emulator + Wear-emulator (Fase 3+) |
| 20 | `/docs/development/maui-workloads.md` | Utvikler | `dotnet workload install maui` per plattform (Fase 1) |

## Hovedsidens (`/README.md`) disposisjon

```markdown
# Brønnøysund Company Lookup

> Slå opp norske selskaper via Enhetsregisteret. Desktop, web, mobil og smartwatch — fra én .NET 10-kodebase.

## Bakgrunn

En venn av meg utfordret meg til å lage en enkel integrasjon mot Brønnøysundregistrene
fordi han var nysgjerrig på hvordan det gjøres; og dette er en måte å gjøre det på.

Underveis vokste prosjektet til en demonstrasjon av cross-platform .NET 10
(MAUI Desktop, Blazor Web, iOS/Android, watchOS/Wear OS) med Clean Architecture
og Ports & Adapters.

## Åpenhet om AI-bruk

Dette prosjektet er bygget i tett samarbeid med Anthropics Claude (Claude Code).
Jeg som menneske har vært kravstiller, gjort Code Review og justert underveis.
Jeg har håndkodet der det var mer effektivt enn å brenne tokens.

Begrunnelse: alt vi bygger her er kjent terreng som ligger godt dokumentert
på GitHub og StackOverflow. AI er produktivt på den typen oppgaver.
For mer spesialiserte deler tar jeg over selv.

## Veikart i repoet

- `/src/` — kildekode
  - `Brreg.Domain`, `Brreg.Application`, `Brreg.Infrastructure` — kjernebibliotek
  - `Brreg.WebApi` — referanse-API for Brreg-oppslag
  - `Brreg.Components`, `Brreg.ViewModels` — delte Blazor/MVVM-presentasjonslag
  - `Brreg.MauiDesktop`, `Brreg.MauiMobile`, `Brreg.BlazorWeb` — plattform-hosts
  - `Watch/iOS/`, `Watch/Android/` — native Swift/Kotlin watch-apper
- `/tests/` — xUnit-tester
- `/docs/` — README-er per plattform + arkitektur
- `/Oppdrag/` — opprinnelig hjemmeoppgave
- `/Plan/` — planer per fase
- `/Dokumentasjon/Presentasjon/` — statiske HTML-sider for presentasjonen

## Kom i gang

| Jeg vil ... | Se |
| --- | --- |
| ... bygge og kjøre koden lokalt | [docs/development.md](docs/development.md) |
| ... installere Web-versjonen på PC/Mac | [docs/install/web-standalone.md](docs/install/web-standalone.md) |
| ... installere Web-versjonen på en server | [docs/install/web-server.md](docs/install/web-server.md) |
| ... installere Desktop-versjonen | [docs/install/desktop.md](docs/install/desktop.md) |
| ... installere Android-appen | [docs/install/android.md](docs/install/android.md) |
| ... installere iOS-appen | [docs/install/ios.md](docs/install/ios.md) |
| ... bruke smartwatch-versjonen | [docs/install/watch-apple.md](docs/install/watch-apple.md) |
| ... forstå arkitekturen | [docs/architecture.md](docs/architecture.md) |
| ... se kildehenvisninger | [docs/credits.md](docs/credits.md) |

## Lisens

MIT. Data fra Brønnøysundregistrene er lisensiert under NLOD 2.0.
```

## Når skrives hva?

| Plan-fase | README-filer som leveres |
| --- | --- |
| Fase 0 | `/README.md`, `/src/Brreg.WebApi/README.md`, `/docs/development.md`, `/docs/architecture.md` (skisse), `/docs/credits.md` |
| Fase 1 | `/docs/install/desktop.md` |
| Fase 2 | `/docs/install/web-standalone.md`, `/docs/install/web-server.md` |
| Fase 3 | `/docs/install/android.md`, `/docs/install/ios.md` |
| Fase 5 | `/docs/install/watch-apple.md`, `/docs/install/watch-wear.md` |

Hver fases plan skal eksplisitt liste README-leveransene i sin DoD.
