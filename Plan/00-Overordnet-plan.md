# 00 — Overordnet plan: Brønnøysund Company Lookup

**Reponavn:** `Bronnoysund.Lookup` — namespaces matcher (`Bronnoysund.Lookup.Domain`, `.Application`, `.Infrastructure`, `.WebApi`, osv.).
**UI-bibliotek (fra Fase 1):** MudBlazor (MIT, Material Design).
**Mobile-distribusjon:** TestFlight (iOS, Apple Developer Program tilgjengelig) + Play Store Internal Testing (Android, Google Play Developer tilgjengelig).

## Bakgrunn

Hjemmeoppgave fra oppdragsgiver (`den opprinnelige oppgave-PDF-en`): bygg en .NET-applikasjon som tar et organisasjonsnummer, henter data fra Brønnøysund Enhetsregisteret, og returnerer et forenklet svar på engelsk. Brukeren utvider scope (`/Oppdrag/InnledendeInstruksjon.md`) til et større produkt med MAUI Desktop, Blazor Web, mobile apper, og watch-grensesnitt — alle bygget på en felles .NET 10 kodebase.

Den autoritative kortversjonen av planen ligger i den autoritative plan-filen i `~/.claude/plans/`. Denne mappen utdyper hver fase.

## Bærende valg

| Valg | Beslutning | Begrunnelse |
| --- | --- | --- |
| Backend-strategi | **Fat clients** i Fase 1-5; sentral backend som opsjonell Fase 6 | Bruker har ikke tid til server-oppsett nå, men arkitekturen er Ports & Adapters-basert så det er lett å bytte senere |
| Eksisterende kode | **Bygg selv**, krediter inspirasjonskilder | Lisens-risiko + hjemmeoppgaven krever demonstrasjon av kompetanse |
| Kode-deling | Maks mulig — Domain/Application/Infrastructure som ett `net10.0`-bibliotek brukt av alle plattformer | "Develop once, deploy everywhere" |
| Watch-arkitektur | Klokken er thin client, telefon-app er "backend" via WatchConnectivity (iOS) / Wearable Data Layer (Android) | Sparer batteri, gir minst kode på klokken |
| Stack | .NET 10 + MAUI Blazor Hybrid + Razor Class Library + ASP.NET Core (referanse-API) + Swift/Kotlin på watch | Microsoft anbefaling for kryss-platform 2025/26 |

## Faseoversikt

| # | Fase | Innhold | Plan-fil |
| --- | --- | --- | --- |
| 0 | **MVP** | Kjernebibliotek + Web API + tester. Tilfredsstiller MVP-leveransen | `01-Fase0-MVP.md` |
| 1 | MAUI Desktop | Mac+Win desktop-app som hoster Blazor-komponenter via BlazorWebView | `02-Fase1-MAUI-Desktop.md` |
| 2 | Blazor Web | Portable Server-modus-bundle som kjøres lokalt PC/Mac eller på webserver | `03-Fase2-Blazor-Web.md` |
| 3 | MAUI Mobile + Voice | iOS+Android-app, voice input/output | `04-Fase3-MAUI-Mobile-og-Voice.md` |
| 4 | Application-utvidelser | Navn-søk + drill-down + stub-ports for gjeld/regnskap/eierskap | `05-Fase4-Application-utvidelser.md` |
| 5 | Watch-apper | Native Swift (watchOS) + Kotlin (Wear OS) via companion-pattern | `06-Fase5-Watch-apper.md` |
| 6 | Sentral backend (opsjonell) | Promoter Web API til produksjon, legg til RemoteApi-adapter | `07-Fase6-Sentral-backend-opsjonell.md` |

## Tverrgående dokumenter

- `08-Patterns-og-arkitekturbegrunnelser.md` — for muntlig presentasjon
- `09-Tredjepartskode-og-kreditering.md` — kilder som krediteres i README
- `10-Distribusjon-og-portability.md` — Web/Desktop portable (USB-stick-vennlig), Mobile sideload, OS-minstekrav
- `11-README-strategi.md` — kart over alle README-filene som skal leveres
- `12-Presentasjon-statisk-HTML.md` — reveal.js-presentasjon under `/Dokumentasjon/Presentasjon/`
- `13-Persistens-og-cache.md` — SQLite + EF Core (fra Fase 1): historikk, favoritter, komprimert cache, auto-cleanup, settings-UI
- `14-IDE-og-verktoy-mapping.md` — `.slnf` solution-filter, IDE-mapping per prosjekt, hvordan åpne i Rider/VS/VS Code/Xcode/Android Studio
- `15-Register-aggregator.md` — parallelle oppslag mot flere offentlige registre (Brreg Roller, regnskap, eiere, underenheter, konkurs, gjeldsregisteret, person-drill-down)

## Vedlikehold

Når detaljer endres eller en fase fullføres, oppdater både fase-filen *og* `progress-brreg` i `~/.claude/projects/-Users-esm-UtviklingPrivat/memory/`.
