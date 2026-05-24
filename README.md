# Bronnoysund.Lookup

> Slå opp norske selskaper via Enhetsregisteret. Desktop, web, mobil og smartwatch — fra én .NET 10-kodebase.

## Bakgrunn

En venn av meg utfordret meg til å lage en enkel integrasjon mot Brønnøysundregistrene fordi han var nysgjerrig på hvordan det gjøres; og dette er en måte å gjøre det på.

Underveis vokste prosjektet til en demonstrasjon av cross-platform .NET 10 (Blazor Web, MAUI Desktop, MAUI Mobile, watchOS/Wear OS) med Clean Architecture og Ports & Adapters. Arkitekturen er klargjort for parallelle oppslag mot flere offentlige registre.

## Åpenhet om AI-bruk

Dette prosjektet er bygget i tett samarbeid med Anthropics Claude (Claude Code som agent i Visual Studio Code).

Min rolle som menneske:

- **Innledende research** hos Brønnøysundregistrene (datasett, API-er, lisens, MOD11-spec for organisasjonsnummer) før AI ble involvert
- **Kravstiller** — arkitektur, valg av patterns, scope, leveransemodell og lisens (AGPL-3.0 + kommersiell)
- **Plan-review** — gikk gjennom hver plan-fil systematisk og kommenterte underveis
- **Code Reviewer** — all AI-generert kode gjennomgås før commit
- **Justering** — endrer retning underveis basert på funn
- **Håndkoder** der det er mer effektivt enn å brenne tokens

Brreg-integrasjon, Clean Architecture og MAUI/Blazor er **kjent terreng** som ligger godt dokumentert på GitHub, Microsoft Learn og StackOverflow. AI er produktivt på den type oppgaver. Min menneskelige rolle er domene-research, arkitekturvalg, kritisk gjennomgang og retnings-endringer — det AI ikke gjør alene.

## Status

| Fase | Status | Tester | Kjørbar |
| --- | --- | --- | --- |
| Fase 0 — Kjerne + WebApi (MVP) | ✅ Ferdig | 31 passerer | `dotnet run --project Kode/src/Bronnoysund.Lookup.WebApi` |
| Fase 2 — Blazor Web (Server) | ✅ Ferdig | — | `dotnet run --project Kode/src/Bronnoysund.Lookup.BlazorWeb` |
| Fase 1 — MAUI Desktop (Mac+Win) | 🟡 Kode ferdig, bygg krever Xcode 26.4 | — | Se [docs/install/desktop.md](docs/install/desktop.md) |
| Fase 3 — MAUI Mobile + Voice | 🟡 Grunnlag + Speech-prosjekter | 16 passerer | Se [docs/install/ios.md](docs/install/ios.md) |
| Fase 4 — Register-aggregator | 📋 Planlagt (Brreg-Roller, Regnskap, Eiere via parallelle oppslag) | — | — |
| Fase 5 — Watch (Swift + Kotlin) | 📋 Planlagt (kun parret telefon) | — | — |
| Fase 6 — Sentral backend (Kubernetes) | 📋 Planlagt (klar for AWS/Azure) | — | — |

**Totalt 47 tester passerer.** Hele løsningen er linter-ren og lover SPDX-headers i alle kildefiler.

## Veikart i repoet

```text
/Kode/                         — .NET 10 solution (10 src + 4 test-prosjekter)
  ├── Bronnoysund.Lookup.sln   — komplett solution
  ├── *.slnf                   — Solution Filter: Core, Web, Desktop, Mobile
  ├── src/                     — alle prosjekter
  └── tests/                   — alle test-prosjekter
/docs/                         — installasjons- og utvikler-dokumentasjon
```

## Kom i gang

| Jeg vil ... | Se |
| --- | --- |
| Kjøre Web-versjonen på Mac/PC | [docs/install/web-standalone.md](docs/install/web-standalone.md) |
| Sette opp Web på en webserver | [docs/install/web-server.md](docs/install/web-server.md) |
| Kjøre Desktop på Mac/Windows | [docs/install/desktop.md](docs/install/desktop.md) |
| Installere Android-appen | [docs/install/android.md](docs/install/android.md) |
| Installere iOS-appen (iPhone og iPad) via TestFlight | [docs/install/ios.md](docs/install/ios.md) |
| Forstå arkitekturen | [docs/architecture.md](docs/architecture.md) |
| Åpne i Rider / VS / VS Code | [docs/development/ide-overview.md](docs/development/ide-overview.md) |
| Se kildehenvisninger | [docs/credits.md](docs/credits.md) |

## Bygg og test lokalt

```bash
cd Kode
dotnet build
dotnet test
dotnet run --project src/Bronnoysund.Lookup.WebApi --urls http://localhost:5099
# I annen terminal:
curl http://localhost:5099/companies/974760843  # Statens vegvesen
```

Forventet respons:

```json
{
  "organizationNumber": "974760843",
  "organizationName": "STATENS VEGVESEN",
  "companyType": "ORGL",
  "languageForm": "Bokmål"
}
```

## Lisens

**Dobbel lisens:**

1. **AGPL-3.0-or-later** for ikke-kommersiell og åpen-kildekode-bruk — se [LICENSE](LICENSE). Alle endringer og avledede verk må publiseres under AGPL-3.0; nettverkstilbud (SaaS) krever også kildekode-tilgang.
2. **Kommersiell tilleggslisens** for kommersiell bruk som ikke kan/vil følge AGPL — se [COMMERCIAL-LICENSE.md](COMMERCIAL-LICENSE.md). Kontakt Røa Systemutvikling AS for å inngå avtale.

SPDX-identifier i hver kildefil: `// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial`

Data fra Brønnøysundregistrene er lisensiert under [NLOD 2.0](https://data.norge.no/nlod/).

## Kreditering

Inspirasjon (ikke kopiert kode — vi har bygget alt selv for kontroll over lisens og vedlikehold):

- [Frank.Libraries.Brreg](https://www.nuget.org/packages/Frank.Libraries.Brreg/) (MIT) — Frank R. Haugen
- [organisationsnummer/csharp](https://github.com/organisationsnummer/csharp) (MIT)
- [storbukas/norsk-validator](https://github.com/storbukas/norsk-validator) (MIT)
- [SindreMA/Blazor-Brønnøysundregistrene](https://github.com/SindreMA/Blazor-Br-nn-ysundregistrene)

Komplett liste i [docs/credits.md](docs/credits.md).

## Eier

Utviklet av **Røa Systemutvikling AS**.
