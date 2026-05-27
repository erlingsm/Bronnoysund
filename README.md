# Bronnoysund

> Look up Norwegian companies in the Brønnøysund Register Centre across web,
> mobile, tablet, and desktop — from a single .NET 10 codebase. Clean
> Architecture with a Kiota-generated client against Brreg's official
> OpenAPI 3 specification, deployed to Azure Container Apps with
> OIDC-gated CI/CD.

## Background

This is the **broader product family** for Brreg-lookup: the MVP scope
(a single web page that resolves an organisation number) is solved in
the sister project [Bronnoysund.MVP](https://github.com/erlingsm/Bronnoysund.MVP).
This repository carries everything beyond it — desktop, mobile, tablet
adaptations, on-device voice input, persistence, a multi-registry
aggregator, and the Kiota-generated client that covers all 36
Enhetsregisteret + Frivillighetsregisteret + Partiregisteret endpoints.

Same Domain, Application, and Razor Components projects feed every
surface; only the host shell and the platform-specific adapters differ.

## What runs where

| Surface | Built with | Status | Notes |
|---|---|---|---|
| **Web (BlazorWeb)** | Blazor Server + MudBlazor + Razor Component Library shared with MAUI hosts | Provisioned as Azure Container App `bronnoysund-web` | Voice via Web Speech API. Tablet-class layouts via `IDeviceLayout`. |
| **WebApi (JSON endpoints)** | ASP.NET Core Minimal API | Live: <https://bronnoysund-webapi.greensky-97f7b0db.norwayeast.azurecontainerapps.io/health> | Standalone JSON surface for headless consumers. |
| **Desktop — Mac Catalyst** | MAUI + Blazor Hybrid (same Razor RCL) | Builds locally on macOS | Voice via `CommunityToolkit.Maui.Media`. Distribution skeleton for Mac App Store. |
| **Desktop — Windows** | MAUI + Blazor Hybrid | Build path defined; not yet verified locally | MSIX skeleton in place; Microsoft Partner Center account pending. |
| **Mobile — iOS (iPhone + iPad)** | MAUI + Blazor Hybrid | Build path defined; needs simulator/device run | Voice + tablet master-detail share the same code as Desktop and Web. |
| **Mobile — Android (phone + tablet + foldable)** | MAUI + Blazor Hybrid | Build path defined; needs emulator/device run | Same. |
| **Smartwatch — Apple Watch / Wear OS** | Native Swift + Kotlin (companion-pattern) | Planned, not started | Phone-companion projection; out of MAUI scope by design. |

The Razor Components project (`Bronnoysund.Components`) is the single
source of truth for pages — `Lookup`, `KodeverkOrganisasjonsformerPage`,
`Settings`, plus the shared `VoiceInputButton` control. All hosts mount
the same components; layout adaptations are driven by an injected
`IDeviceLayout` that resolves to Phone / Tablet / Desktop per host.

## Demo on the web

A guided walk-through of the deployed Web app once it's live. About
five minutes.

1. **Org-number happy path** — open the deployed URL, enter `974760843`
   and press Enter. The aggregated card slides in: name (Riksrevisjonen),
   org form (`ORGL`), language form (Bokmål), plus four collapsible
   sections — Contact (address, web, phone, email), Activity (industry,
   employees, sector), Lifecycle (founding date, registry-presence
   flags), and Roles (board, signature authority).

2. **Sub-units** — for an entity with branches (e.g. `971032081`
   Statens vegvesen), a "Sub-units" panel lists each registered
   underenhet with org-number and name.

3. **Bankruptcy badge** — for any bankrupt entity the lookup shows a
   red banner above the detail card with the declaration date.

4. **Name search with pagination** — switch the radio to **Name**,
   enter `Statens vegvesen`. A paginated table appears with hits +
   total-count caption. Page navigator + page-size dropdown (10 / 25
   / 50 / 100). The search results stay visible after drill-down so
   you can pick another result.

5. **Master-detail on tablet** — open the same URL on a tablet-sized
   viewport. The page rearranges: search results stay on the left,
   the selected company card on the right (5/7 column split). On
   phone, the same screen falls back to a stacked view automatically
   via `IDeviceLayout`.

6. **Deep-link** — paste
   `?name=Statens+vegvesen&select=971032081` after the URL. The page
   wakes up with the search pre-run and Statens vegvesen pre-selected.
   Used for sharing specific result rows and for deterministic
   screenshots.

7. **Voice input** — click the microphone icon next to the input
   (only visible if your browser has the Web Speech API — Chrome and
   Edge desktop, mobile Safari). Say "ni en ni tre null null tre åtte
   åtte". The `NorskTallParser` converts the spoken digits to
   `919300388` and runs the lookup. Say a company name instead
   ("Statens vegvesen") and it switches to name-search mode
   automatically.

8. **Read aloud** — click the speaker icon on the result card. The
   browser reads name + org-number (digit-by-digit) + company form +
   industry in the selected UI language.

9. **Browse — Organisasjonsformer** — click the hamburger menu → Browse
   → Organisation forms. A live table of Brreg's organisation-form
   kodeverk (AS, ASA, ORGL, FLI, ENK, ...), with a filter box and an
   Active / Retired badge per code. Proof-of-pattern for the
   remaining 28 open Brreg endpoints; each one is a copy of this
   page with a different adapter call.

10. **Language switcher** — Settings page → Language dropdown:
    English / Norsk (Bokmål) / Nynorsk. Every label, validation
    message and section title is localised against three `.resx`
    files.

11. **JSON API direct** — in a terminal:

    ```bash
    curl -s https://bronnoysund-webapi.greensky-97f7b0db.norwayeast.azurecontainerapps.io/companies/974760843 | python3 -m json.tool
    curl -s "https://bronnoysund-webapi.greensky-97f7b0db.norwayeast.azurecontainerapps.io/companies/974760843/aggregated" | python3 -m json.tool
    curl -s "https://bronnoysund-webapi.greensky-97f7b0db.norwayeast.azurecontainerapps.io/companies?name=Riksrevisjonen&size=5" | python3 -m json.tool
    ```

A curated table of inputs that hit every code path is in [Demo
inputs](#demo-inputs) further down.

## Per-surface functionality matrix

| Capability | Web | Desktop (Mac / Win) | Mobile (iOS / Android) | Tablet (iPad / Android tablet) |
|---|---|---|---|---|
| Org-number lookup with aggregated detail | ✓ | ✓ | ✓ | ✓ |
| Paginated name search → drill-down | ✓ | ✓ | ✓ | ✓ |
| Master-detail layout (search hits ↔ detail side-by-side) | ✓ (≥ 768 px viewport) | ✓ (always) | — (stack) | ✓ |
| Voice input (mic button → orgnr or name) | ✓ Web Speech API | ✓ CommunityToolkit.Maui.Media | ✓ Same | ✓ Same |
| Read-aloud (speaker icon on detail card) | ✓ SpeechSynthesis | ✓ MAUI TextToSpeech | ✓ Same | ✓ Same |
| Browse — Organisasjonsformer kodeverk | ✓ | ✓ | ✓ | ✓ |
| Deep-link `?orgnr=` / `?name=&select=` | ✓ | n/a (no URL bar) | n/a | n/a |
| Local SQLite cache + history + favourites + settings | n/a (HybridCache in-memory) | ✓ `~/.../Bronnoysund/bronnoysund.db` | ✓ Same path convention | ✓ Same |
| Three UI languages (en / nb-NO / nn-NO) | ✓ | ✓ | ✓ | ✓ |
| Offline lookup (last-cached) | — | ✓ | ✓ | ✓ |
| Apple Pencil / S-Pen handwriting on org-number field | n/a | n/a | n/a | ✓ (iPad / Android) — defined in Plan 17, not yet wired |

## Code navigation

```text
/Kode/
├── Bronnoysund.sln                                  Top-level solution
├── Bronnoysund.{Core,Web,Desktop,Mobile}.slnf       Solution filters per workflow
├── src/
│   ├── Bronnoysund.Domain/                          OrganizationNumber value object, MOD11, Company, LanguageForm
│   ├── Bronnoysund.Application/                     Use-case handlers + ports + CompanyLookupResult discriminated union
│   ├── Bronnoysund.Infrastructure/                  Adapters over the Kiota client + Polly resilience + HybridCache
│   ├── Bronnoysund.Infrastructure.Brreg.Generated/  Kiota-generated client from Brreg's OpenAPI 3 spec — DO NOT EDIT
│   ├── Bronnoysund.Infrastructure.Persistence/      SQLite + EF Core + GZip-compressed cache + repositories
│   ├── Bronnoysund.ViewModels/                      MVVM view-models + localised .resx (en, nb-NO, nn-NO)
│   ├── Bronnoysund.Components/                      Razor Component Library — pages + VoiceInputButton (shared by all UI hosts)
│   ├── Bronnoysund.Speech/                          Speech ports (ISpeechToText, ITextToSpeech) + NorskTallParser
│   ├── Bronnoysund.Speech.Web/                      Browser Web Speech API adapter (JS-interop)
│   ├── Bronnoysund.BlazorWeb/                       Blazor Server UI host
│   ├── Bronnoysund.WebApi/                          ASP.NET Core Minimal API (JSON only)
│   ├── Bronnoysund.MauiMobile/                      MAUI Blazor Hybrid app for iOS + Android (phone + tablet)
│   └── Bronnoysund.MauiDesktop/                     MAUI Blazor Hybrid app for Mac Catalyst + Windows
└── charts/bronnoysund/                              Helm chart for self-hosted K8s deployments (alternative to Azure CA)

/.github/workflows/                                  CI + per-platform CD pipelines (Azure / Mac / Windows / iOS / Android)
/deploy/                                             Local-manual deploy scripts mirroring each CD pipeline
```

The dependency arrows point inward — Domain knows nothing; Application
depends only on Domain; Infrastructure plugs adapters into Application's
ports; each host (WebApi, BlazorWeb, MauiMobile, MauiDesktop) wires up
its preferred Infrastructure adapters at startup.

## Brreg coverage

The client against Brønnøysundregistrene is **generated by
[Kiota](https://learn.microsoft.com/openapi/kiota/overview) from Brreg's
official OpenAPI 3 specification**. A nightly CI workflow re-fetches
the spec and opens a GitHub issue if it has drifted from the committed
`kiota-lock.json`. The whole spec — 36 endpoints, ~200 schemas — is
available in code; UI exposes a deliberately growing subset.

### Open endpoints (no authentication)

| Endpoint | In code (adapter) | In UI |
|---|---|---|
| `/enheter/{orgnr}` | ✓ `BrregCompanyProvider` | ✓ Lookup page |
| `/enheter?navn=…` (paginated) | ✓ `BrregCompanySearchProvider` | ✓ Lookup page, name mode |
| `/enheter/{orgnr}/roller` | ✓ `BrregRolesProvider` | ✓ Lookup detail panel |
| `/underenheter?overordnetEnhet=…` | ✓ `BrregSubUnitsProvider` | ✓ Lookup detail panel |
| `/organisasjonsformer` | ✓ `BrregKodeverkProvider` | ✓ Browse → Organisation forms |
| 28 other open endpoints (Kommuner, Næringskoder, Oppdateringer, Frivillighet, Matrikkel, Downloads, Partiregisteret) | Client generated (callable) | Not yet built — same one-page pattern per endpoint |

### Closed endpoints (Maskinporten-gated)

| Endpoint / Registry | What unlocks it |
|---|---|
| `/autorisert-api/enheter/{orgnr}/roller` (with personnummer + address) | Maskinporten consumer + scope `brreg:enhetsregister.read` |
| Regnskapsregisteret — annual reports | Maskinporten scope `regnskapsregisteret:read` + a separate Kiota client for that registry |
| Reelle rettighetshavere — beneficial ownership | Maskinporten scope + AML-reporting status (banks, lawyers, real-estate agents, etc.) |
| Løsøreregisteret — liens on vehicles, equipment | Separate Brreg spec (not in the main OpenAPI document) |
| Elektronisk mottak — filings | Maskinporten + Altinn authorisation |
| Gjeldsregisteret — personal debt | Not a Brreg API — commercial agreement + mutual-TLS via `gjeldsregisteret.com` |

**What it takes to unlock Maskinporten-gated registries** (one-time
admin work, then ~4 hours of code):

1. Order an enterprise certificate from Buypass (~3 000 NOK/year) or
   Commfides (~4 500 NOK/year). 1–2 week lead time.
2. Onboard the consumer `Bronnoysund` in Digdir Selvbetjening
   (<https://selvbetjening.digdir.no>). Days.
3. Apply for the scopes you need. Seconds for self-service scopes,
   weeks for AML-gated ones.
4. Store the certificate in Azure Key Vault and grant the Container
   App's managed identity read access.
5. Implement the Maskinporten token provider + a second authenticated
   Kiota client (mirrors the existing one with a different base URL
   and a `BaseBearerTokenAuthenticationProvider`).

Never commit `.p12`, `.pfx`, `.keystore`, `.jks`, or `AuthKey_*.p8` —
all are excluded by `.gitignore` already. Keep local copies in
`~/.bronnoysund/` outside the repo.

## API

The WebApi host exposes the JSON surface in case other clients (Postman,
shell scripts, anything not built on this codebase) want to consume the
same data:

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/companies/{orgnr}` | Core lookup — one entity |
| `GET` | `/companies/{orgnr}/aggregated` | Parallel fan-out: entity + roles + sub-units + bankruptcy in one response |
| `GET` | `/companies?name={q}&size={N}&page={P}` | Paginated name search |
| `GET` | `/health` | Liveness probe |

### Demo inputs

Curated test inputs covering every meaningful code path:

| Input | Expected outcome |
|---|---|
| `919300388` | Equinor ASA — large public company, multiple sub-units |
| `974760843` | Riksrevisjonen — public-sector entity, Bokmål målform |
| `971032081` | Statens vegvesen — `ORGL` form, registered roles |
| `933722821` | Røa Systemutvikling AS — small private AS |
| `12345` | Validation error — must be exactly 9 digits |
| `abc123def` | Validation error — digits only |
| `712345678` | Validation error — must start with 8 or 9 |
| `800000050` | Validation error — MOD11 control digit fails |
| `899999991` | Format-valid but not in Brreg — returns `not_found` |

Name-search inputs that exercise pagination, drill-down, and UTF-8
encoding: `Statens vegvesen`, `Equinor`, `Røa`, `Universitetet`.

## Run locally

```bash
cd Kode

# Web UI + JSON API on http://localhost:5199
dotnet run --project src/Bronnoysund.BlazorWeb

# Standalone JSON API (no UI) on http://localhost:5000
dotnet run --project src/Bronnoysund.WebApi --urls http://localhost:5000

# MAUI Desktop (Mac Catalyst — macOS only)
dotnet run --project src/Bronnoysund.MauiDesktop -f net10.0-maccatalyst

# MAUI Mobile (iOS simulator — macOS only)
dotnet build src/Bronnoysund.MauiMobile -f net10.0-ios -t:Run
```

## Deploy

The Web UI (`bronnoysund-web`) and the JSON API (`bronnoysund-webapi`)
are deployed independently to Azure Container Apps in region
`norwayeast`. Each has its own GitHub Actions CD pipeline that fires on
push to `master` (paths-filtered) and on manual `workflow_dispatch`.

Authentication uses OpenID Connect with a Federated Credential on an
Azure AD App Registration — no client secret stored in GitHub.

Per-platform skeleton pipelines also exist for the four
store-distributed targets:

- `cd-mac-desktop.yml` — Mac App Store via Apple Developer Program
- `cd-windows-desktop.yml` — Microsoft Store / MSIX via Partner Center
  (disabled-by-default — no account yet)
- `cd-ios-mobile.yml` — App Store / TestFlight via Apple Developer
- `cd-android-mobile.yml` — Google Play via Play Console

Each skeleton produces the signed artefact (`.app`, `.msix`, `.ipa`,
`.aab`); the store-upload step is stubbed with the exact commands
needed once the signing material is in place. See
[deploy/README.md](deploy/README.md) for credentials matrix and local
manual deploy scripts.

## Credits

### Primary sources

Brønnøysundregistrene maintains the authoritative resources this
project integrates with:

- **Umbrella developer docs**: <https://brreg.github.io/docs/> — canonical hub for the whole Brreg ecosystem (8 registries + Maskinporten + Altinn integration patterns)
- **GitHub org**: <https://github.com/orgs/brreg/repositories> — Brreg's own open-source clients and tooling
- **Enhetsregisteret API documentation**: <https://data.brreg.no/enhetsregisteret/api/dokumentasjon/no/index.html>
- **Enhetsregisteret OpenAPI 3 specification** (the contract this project's Kiota client is generated from): <https://data.brreg.no/enhetsregisteret/api/dokumentasjon/no/openapi.json>

Data from Brønnøysundregistrene is licensed under [NLOD 2.0](https://data.norge.no/nlod/).

### Secondary inspirations

Third-party C# libraries reviewed for patterns (no code copied — own
implementation per the original assignment):

- [Frank.Libraries.Brreg](https://github.com/frankhenrichdamgaard/Frank.Libraries) — Brreg lookup patterns
- [organisationsnummer/csharp](https://github.com/organisationsnummer/csharp) — MOD11 reference
- [SindreMA](https://github.com/SindreMA) — Brreg endpoint exploration

### Sister project

[Bronnoysund.MVP](https://github.com/erlingsm/Bronnoysund.MVP) — the
minimum-viable assignment scope (single Blazor page, two Brreg
endpoints, hand-coded HTTP client). This project (the broader product
family) reuses the same architectural primitives at larger scope.

## License

Dual license:

- **AGPL-3.0-or-later** for open-source and non-commercial use — see [LICENSE](LICENSE).
- **Commercial license** for use cases that cannot accept AGPL — see [COMMERCIAL-LICENSE.md](COMMERCIAL-LICENSE.md). Contact Røa Systemutvikling AS.

SPDX identifier in every source file: `// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial`

## Owner

Developed by **Røa Systemutvikling AS**.
