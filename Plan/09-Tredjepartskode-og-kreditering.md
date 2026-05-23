# 09 — Tredjepartskode og kreditering

Dette dokumentet er kildelisten som speiles inn i README under "Inspirasjon & kildehenvisninger".

## Prosjekter vi har sett på (ikke kopiert kode fra)

### Frank.Libraries.Brreg

- URL: <https://www.nuget.org/packages/Frank.Libraries.Brreg/>
- Forfatter: Frank R. Haugen
- Lisens: MIT
- Hva vi tok med oss: tilnærmingen til en typed HttpClient og DTO-mapping. Vi reimplementerte i stedet for å ta avhengighet, for å holde full kontroll over koden.

### organisationsnummer/csharp

- URL: <https://github.com/organisationsnummer/csharp>
- Lisens: MIT
- Hva vi tok med oss: form på `IsValid`-API for orgnr-validator. Vår implementasjon er skrevet fra bunnen som DDD Value Object.

### storbukas/norsk-validator

- URL: <https://github.com/storbukas/norsk-validator>
- Lisens: MIT
- Hva vi tok med oss: referanse for MOD11-vektene (verifisert mot Brreg-spesifikasjon).

### SindreMA/Blazor-Brønnøysundregistrene

- URL: <https://github.com/SindreMA/Blazor-Br-nn-ysundregistrene>
- Lisens: ikke oppgitt — derfor lest kun overflate-arkitektur, ingen kode-detaljer
- Hva vi tok med oss: ingen direkte. Bekreftet at vi tar samme grunnvalg (Blazor for web-UI) som en annen .NET-utvikler allerede hadde valgt.

### oysandvik94/brreg-app

- URL: <https://github.com/oysandvik94/brreg-app>
- Lisens: Apache-2.0
- Hva vi tok med oss: ingen — React-frontend, ikke relevant for vår stack.

## NuGet-pakker vi bruker (cross-cutting concerns)

| Pakke | Lisens | Til hva |
| --- | --- | --- |
| Microsoft.Extensions.Http.Resilience | MIT | Polly v8 resilience pipeline |
| Microsoft.Extensions.Caching.Hybrid | MIT | HybridCache (L1 in-memory) |
| Microsoft.Extensions.Logging | MIT | Logging-abstraksjon |
| Serilog.AspNetCore + Sinks.Console + Sinks.File | Apache-2.0 | Strukturert logging til console og rolling file |
| FluentValidation | Apache-2.0 | Request-validering i Application |
| CommunityToolkit.Mvvm | MIT | MVVM med source generators (fra Fase 1) |
| MudBlazor | MIT | UI-komponentbibliotek i RCL (fra Fase 1) |
| xunit | Apache-2.0 | Testrammeverk |
| FluentAssertions | Apache-2.0 | Lesbar assertion-syntaks |
| NSubstitute | BSD-3-Clause | Mocking |
| WireMock.Net | Apache-2.0 | HTTP-mock for integrasjonstester |
| bUnit | MIT | Razor-komponent-tester (fra Fase 1) |
| Microsoft.EntityFrameworkCore | MIT | ORM for SQLite-persistens (fra Fase 1) |
| Microsoft.EntityFrameworkCore.Sqlite | MIT | SQLite-provider inkl. Microsoft.Data.Sqlite + native binær |
| Microsoft.EntityFrameworkCore.Design | MIT | Migrations-CLI (`dotnet ef`) |
| SQLite (engine) | Public Domain | Embedded database — bundlet via EF Core Sqlite-pakken |
| CommunityToolkit.Maui.Media | MIT | On-device speech-to-text på iOS/Android/Mac/Windows (fra Fase 3) |
| Microsoft.Maui.Essentials.TextToSpeech | MIT (innebygd i MAUI) | On-device text-to-speech på alle MAUI-plattformer |

Alle disse er etablerte, vedlikeholdte pakker med tillatelige lisenser kompatible med vår løsning.

**Lisens-kompatibilitet med AGPL-3.0:** MIT, Apache-2.0 og BSD-3-Clause er alle permissive og fullt kompatible med AGPL-3.0 som "outbound" lisens. Vi kan kombinere disse fritt.

## Data fra Brønnøysundregistrene

- Lisens: NLOD 2.0 (Norwegian License for Open Data)
- Krediteres i README: "Data fra Brønnøysundregistrene, lisensiert under NLOD 2.0."
- URL: <https://data.norge.no/nlod/>

## Bilder / ikoner

(Fylles inn når Fase 1+ legger til assets.)

## Lisens for vår kode (avgjort)

**Dobbel lisens:**

1. **AGPL-3.0-or-later** for ikke-kommersiell + åpen-kildekode-bruk
   - Alle endringer og avledede verk MÅ publiseres under AGPL-3.0
   - Nettverkstilbud (SaaS) krever også kildekode-tilgang
2. **Kommersiell tilleggslisens** for kommersiell bruk som ikke kan/vil følge AGPL
   - Kontakt Røa Systemutvikling AS for å inngå avtale
   - Detaljer i `COMMERCIAL-LICENSE.md`

Filer som skal opprettes i Fase 0:

- `LICENSE` (rot) — full AGPL-3.0-tekst (hentes fra <https://www.gnu.org/licenses/agpl-3.0.txt>)
- `COMMERCIAL-LICENSE.md` (rot) — kort beskrivelse + kontaktinfo til Røa Systemutvikling AS
- Header i hver kildefil: `// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial`
- README-seksjon "Lisens" som forklarer modellen i klartekst

**Hvorfor AGPL-3.0:** Sterkeste copyleft-lisens, OSI-godkjent, tvinger endringer tilbake også for nettverk-bruk (ikke bare distribusjon). Etablert dual-licensing-modell brukt av MongoDB (tidligere), Nextcloud, Mautic, Grafana Mimir.

**Trenger fra Røa Systemutvikling AS:** organisasjonsnummer, kontakt-e-post for kommersiell lisens, og web-URL (alle vises i COMMERCIAL-LICENSE.md og i About-siden i presentasjonen).
