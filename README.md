# Bronnoysund

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

## To driftsmoduser — én kodebase

Samme kompilerte app (Web, Desktop, Mobile) kan kjøre i to moduser, valgt via konfigurasjon:

- **Fat Client** (default — gratis, App Store/Google Play): Brreg-oppslag direkte fra enheten. Ingen brukerkonto, fungerer offline med cache. Dekker MVP. Leveres i flere innpakninger:
  - **MAUI Desktop** — native app for Mac + Windows (`.app` / `.exe`)
  - **MAUI Mobile** — native app for iOS + Android (inkl. iPad og Android-tablets)
  - **Blazor Web Server portable** — kjørbar `.exe`/binary for Mac/Win/Linux som starter Kestrel og åpner i nettleser (USB-stick-vennlig)
- **Thin Client** (Subscription via Azure-sky): rikere funksjonalitet som krever sentralisert tilgang — regnskap via Maskinporten, eierregister, andre land (UK, FR, DE, USA, ...), synkronisering av favoritter mellom enheter.

Bytte er én linje i `appsettings.json`: `"DataSource": { "Mode": "Direct" }` eller `"RemoteApi"`. Detaljer + sammenligningsmatrise i [docs/fat-vs-thin-client.md](docs/fat-vs-thin-client.md).

## Status

| Fase | Status | Tester | Kjørbar |
| --- | --- | --- | --- |
| Fase 0 — Kjerne + WebApi (MVP) | ✅ Ferdig | 31 passerer | `dotnet run --project Kode/src/Bronnoysund.WebApi` |
| Fase 2 — Blazor Web (Server) | ✅ Ferdig | — | `dotnet run --project Kode/src/Bronnoysund.BlazorWeb` |
| Fase 1 — MAUI Desktop (Mac+Win) | 🟡 Kode ferdig, bygg krever Xcode 26.4 | — | Se [docs/install/desktop.md](docs/install/desktop.md) |
| Fase 3 — MAUI Mobile + Voice | 🟡 Grunnlag + Speech-prosjekter | 16 passerer | Se [docs/install/ios.md](docs/install/ios.md) |
| Fase 4 — Register-aggregator | 📋 Planlagt (Brreg-Roller, Regnskap, Eiere via parallelle oppslag) | — | — |
| Fase 5 — Watch (Swift + Kotlin) | 📋 Planlagt (kun parret telefon) | — | — |
| Fase 6 — Sky (Azure Container Apps) | 🟢 Dockerfile + CI/CD klar; deploy via [docs/install/azure-deploy.md](docs/install/azure-deploy.md) | — | — |

**Totalt 47 tester passerer.** Hele løsningen er linter-ren og lover SPDX-headers i alle kildefiler.

## Veikart i repoet

```text
/Kode/                         — .NET 10 solution (10 src + 4 test-prosjekter)
  ├── Bronnoysund.sln   — komplett solution
  ├── *.slnf                   — Solution Filter: Core, Web, Desktop, Mobile
  ├── src/                     — alle prosjekter
  └── tests/                   — alle test-prosjekter
/docs/                         — installasjons- og utvikler-dokumentasjon
```

## Kom i gang

| Jeg vil ... | Se |
| --- | --- |
| Forstå Fat vs Thin Client (gratis vs subscription) | [docs/fat-vs-thin-client.md](docs/fat-vs-thin-client.md) |
| Kjøre Web-versjonen på Mac/PC | [docs/install/web-standalone.md](docs/install/web-standalone.md) |
| Sette opp Web på en webserver | [docs/install/web-server.md](docs/install/web-server.md) |
| Deploye til Azure Container Apps (Oslo) | [docs/install/azure-deploy.md](docs/install/azure-deploy.md) |
| Sette opp GitHub Actions CI/CD | [docs/development/cicd-github-actions.md](docs/development/cicd-github-actions.md) |
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
dotnet run --project src/Bronnoysund.WebApi --urls http://localhost:5099
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

## Brreg-dekning per i dag

Klienten mot Brreg er generert fra den offisielle OpenAPI 3-spec'en —
<https://data.brreg.no/enhetsregisteret/api/dokumentasjon/no/openapi.json> —
via Kiota. Nattlig CI-job (`.github/workflows/spec-drift.yml`) varsler
via GitHub-issue når Brreg endrer spec'en. Regenererer ved behov:

```bash
cd Kode/src/Bronnoysund.Infrastructure.Brreg.Generated
./regenerate.sh
```

**Åpne endepunkter (ingen autentisering):**

| Endepunkt | I koden? | I UI? |
|---|---|---|
| `/enheter/{orgnr}` | ✓ via `BrregCompanyProvider` | ✓ Lookup-side |
| `/enheter?navn=…` (paginert) | ✓ via `BrregCompanySearchProvider` | ✓ Lookup (navn-modus) |
| `/enheter/{orgnr}/roller` | ✓ via `BrregRolesProvider` | ✓ Lookup-resultat |
| `/underenheter?overordnetEnhet=…` | ✓ via `BrregSubUnitsProvider` | ✓ Lookup-resultat |
| `/organisasjonsformer` | ✓ via `BrregKodeverkProvider` | ✓ `/kodeverk/organisasjonsformer` |
| Øvrige 28 åpne endepunkter (Kommuner, Næringskoder, Oppdateringer, Frivillighet, Matrikkel, Downloads, Partiregisteret) | Klient generert (klar til bruk) | Ikke bygget — full spec i `Plan/54-Frontend-alle-aapne-endepunkter.md` |

**Lukkede endepunkter (krever Maskinporten):**

| Endepunkt / Register | Hva som mangler |
|---|---|
| `/autorisert-api/enheter/{orgnr}/roller` | Virksomhetssertifikat + Maskinporten-onboarding + scope `brreg:enhetsregister.read` |
| Regnskapsregisteret | Egen Maskinporten-scope `regnskapsregisteret:read` + egen Kiota-klient |
| Reelle rettighetshavere | Scope `brreg:reellerettighetshavere/read` + AML-rapporteringsstatus |
| Løsøreregisteret | Egen spec (ikke i hoved-OpenAPI) |
| Elektronisk mottak | Maskinporten + Altinn-autorisasjon |
| Gjeldsregisteret | IKKE Brreg — kommersiell avtale + 2-veis TLS via `gjeldsregisteret.com` |

**Hva må til for full Maskinporten-dekning:**

1. **Virksomhetssertifikat** fra Buypass (~3 000 NOK/år) eller Commfides
   (~4 500 NOK/år) — 1–2 ukers bestillingstid
2. **Digdir Selvbetjening-onboarding** av konsumenten `Bronnoysund` — noen dager
3. **Søk om scopes** — sekunder for åpne scopes, uker for AML-gated
4. **Lagre sertifikat i Azure Key Vault** + gi Container App's managed identity tilgang
5. **Implementer Maskinporten-token-provider + autorisert Kiota-klient** (~4 timer kode når premissene er på plass)

Aldri sjekk inn `.p12`, `.pfx`, `.keystore`, `.jks`, eller `AuthKey_*.p8` —
de er allerede i `.gitignore`. Lokal kopi forventes i `~/.bronnoysund/`.

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
