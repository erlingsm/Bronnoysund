# Tidsbruk

Løpende logg av tidsbruk per utviklings-iterasjon for `Bronnoysund.Lookup`. Brukes til estimering, tilbakerapportering, og som dokumentasjon overfor oppdragsgiver/sensor.

## Format

Hver iterasjon dokumenteres med:

- **Start / slutt:** filsystem-baserte tidsstempler der mulig (verifiserbart)
- **Elapsed:** vegg-tid mellom start og slutt
- **Aktiv AI-tid (estimat):** anslått tid hvor AI faktisk genererte output
- **Brukerens review-tid (estimat):** anslått tid hvor mennesket leste, reviewet og kommenterte
- **Artefakter:** hva som ble produsert (filer, ord, KB)
- **Input:** hva som trigget iterasjonen

Tider over 1 time avrundes til nærmeste 15 minutter; under 1 time til nærmeste 5 minutter. Estimater er markert eksplisitt.

---

## Iterasjon 1 — Spesifikasjons-fase v1 (2026-05-23)

### Tidsstempler (verifiserbart fra filsystem)

| Hendelse | Tidspunkt |
| --- | --- |
| Original PDF mottatt fra oppdragsgiver | 2026-05-21 10:27 |
| `Oppdrag/InnledendeInstruksjon.md` lagt inn av bruker | 2026-05-23 12:45:06 |
| `Oppdrag/Presentasjon.md` lagt inn av bruker | 2026-05-23 13:23:40 |
| `Oppdrag/ReadMe` lagt inn av bruker | 2026-05-23 13:38:37 |
| Første memory-fil skrevet av AI | 2026-05-23 15:15:52 |
| Siste plan-fil modifisert i denne iterasjonen | 2026-05-23 16:34:40 |

### Tidsbruk

| Måling | Verdi |
| --- | --- |
| **Total elapsed** (første brukerinstruksjon → siste plan-edit) | **~3 t 49 min** |
| Aktiv AI-skrivetid (estimat) | ~1,5–2 t |
| Brukerens review-/feedback-tid (estimat) | ~1–2 t |
| Faktisk implementasjonskode skrevet | **0 (ingen kode i denne iterasjonen — kun spesifikasjon)** |

### Artefakter produsert

| Type | Antall | Størrelse | Ord |
| --- | --- | --- | --- |
| Plan-filer i `/Plan/` | 14 | ~132 KB | ~13 548 |
| Memory-filer i `~/.claude/.../memory/` | 9 | ~44 KB | ~2 217 |
| Lint-konfig (`.markdownlint-cli2.jsonc`) | 1 | <1 KB | — |
| **Sum** | **24** | **~176 KB** | **~15 765** |

Vekstfaktor fra original 3-siders oppgave-PDF: ~10–13× i tekst-volum (gjenspeiler scope-utvidelse fra MVP til komplett produktstack med MAUI Desktop, Blazor Web, MAUI Mobile, watchOS, Wear OS, persistens, voice, distribusjon).

### Innhold levert

Spesifikasjonsfase fullført — 14 plan-filer dekker:

1. Overordnet plan + bærende valg
2. Fase 0 (MVP) — detaljert
3. Fase 1 (MAUI Desktop)
4. Fase 2 (Blazor Web — Server-modus, portable)
5. Fase 3 (MAUI Mobile + on-device voice)
6. Fase 4 (Application-utvidelser + stub-ports for andre registre)
7. Fase 5 (Watch-apper — companion-pattern)
8. Fase 6 (Sentral backend — opsjonell senere)
9. Patterns og arkitekturbegrunnelser (inkl. verifiserbare litteratur-henvisninger)
10. Tredjepartskode og kreditering (lisens-kompatibilitet)
11. Distribusjon og portability (Web/Desktop/Mobile)
12. README-strategi (mange README-filer mapped)
13. Presentasjon (statisk HTML, flere sider)
14. Persistens og cache (SQLite + EF Core, GZip, LRU, settings-UI)

Pluss 9 memory-filer som etablerer hand-off-protokoll, arbeidsflyt-konvensjoner og referanser.

### Inngangsmateriale (input)

- Hjemmeoppgave-PDF fra oppdragsgiver (3 sider) — original MVP-spec
- Brukerens utvidede instruksjon (`InnledendeInstruksjon.md`) — produktstack-scope
- Brukerens ReadMe-spec (`ReadMe`) — portability + README-pakke + distribusjon
- Brukerens Presentasjons-spec (`Presentasjon.md`) — statisk HTML, AI-åpenhet
- Brukerens kommentarer underveis (lisens, persistens, voice, watch-UI, kreditering, etc.)

### Status ved iterasjons-slutt

- **Kode skrevet:** 0
- **Spesifikasjon:** komplett v1, godkjent som plan, venter på siste plan-review
- **Bestemt:** alle bærende valg (backend-strategi, plattform-stack, lisens, persistens-arkitektur, voice-strategi, watch-arkitektur)
- **Utestående:** brukerens review og eventuelle «Erling sin kommentar:»-tilbakemeldinger før kode-arbeid starter

### Reflekjon

- Plan-iterasjoner og linter-runder tok mer tid enn forventet — flere skriv-om-passer fordi krav kom inn i etapper
- Brukerens valg om dobbel lisens (AGPL-3.0 + kommersiell) krevde grundig vurdering og dokumentasjon
- Watch-arkitektur og persistens-scope ble større enn opprinnelig planlagt, men er nå godt strukturert for senere implementasjon
- Investeringen i memory-/hand-off-struktur skal lønne seg fra iterasjon 2 og utover — ny agent kan ta over uten kontekstap

---

## Iterasjon 2 — Fase 0 ferdig + Fase 2 WIP (2026-05-23, kveld)

### Tidsstempler (verifiserbart fra filsystem)

| Hendelse | Tidspunkt |
| --- | --- |
| Brukeren ga klarsignal "Nå er vi klare" | 2026-05-23, ~17:00 |
| Første kode-fil (`OrganizationNumber.cs`) skrevet | 2026-05-23 17:30 (estimat) |
| Fase 0 verifisert end-to-end mot Brreg | 2026-05-23 21:24 |
| Fase 0 git-commit (`c3d00ce`) | 2026-05-23 ~21:25 |
| Fase 2 grunnstruktur commit (`4d98bac`) | 2026-05-23 23:30 |
| Brukeren ba om pause | 2026-05-23 23:25 |

### Tidsbruk

| Måling | Verdi |
| --- | --- |
| **Total elapsed** (klarsignal → pause) | **~6 t 30 min** |
| Aktiv AI-skrivetid (estimat) | ~3,5 t |
| Brukerens reaksjons-/avbrudds-tid (estimat) | ~3 t (inkluderer plan-iterasjoner, MAUI-workload-installasjon, nettverks-brudd, etc.) |
| Faktisk implementasjonskode skrevet | **~25 .cs/.razor-filer + 3 csproj-modifikasjoner** |

### Artefakter produsert i iterasjon 2

| Type | Antall | Detalj |
| --- | --- | --- |
| .NET-prosjekter | 7 (Fase 0) + 3 (Fase 2) = 10 | Domain, Application, Infrastructure, WebApi, ViewModels, Components, BlazorWeb + 3 test-prosjekter |
| C#/Razor-filer skrevet | ~25 | inkl. tester |
| Tester | 31 (alle passerer) | 21 Domain + 5 Application + 5 Infrastructure |
| Nye plan-filer | 1 (plan 15 register-aggregator) | + oppdateringer i 00, 01, 02, 03, 04, 09, 10 |
| Nye dokumentasjons-filer | 1 (`Leseliste.md`) | + Tidsbruk.md-oppdatering (denne) |
| Memory-oppdateringer | progress_brreg.md helt rewritten | + 1 ny (`feedback_tidsbruk_logging.md`) |
| Git-commits | 2 | `c3d00ce` Fase 0, `4d98bac` Fase 2 WIP |

### Innhold levert

- **Fase 0 = 100 % ferdig og verifisert**:
  - Domain (`OrganizationNumber` Value Object med MOD11, `Company` entity, `LanguageForm` enum)
  - Application (LookupCompany use case + handler, alle 8 aggregator-porter etablert som interfaces, `CompanyResponse` DTO, `CompanyLookupResult` diskriminert union, FluentValidation)
  - Infrastructure (Brreg typed HttpClient + Polly resilience + HybridCache + `CachedLookup`-wrapper for polymorf serialisering + CoreOnlyAggregator + 7 NotAvailableYet-stubs)
  - WebApi (Minimal API + Serilog + appsettings)
  - **31 tester passerer** (21 Domain MOD11/normalisering + 5 Application handler + 5 Infrastructure WireMock.Net)
  - **End-to-end verifisert**: WebApi starter, curl mot 919300388 → 200 JSON, cache-hit på tur 2
- **Fase 2 = 70 % ferdig**:
  - Components RCL med MudBlazor 9.4.0 (Pages/Lookup.razor — full UI med MudTextField, MudButton, MudPaper, MudList)
  - ViewModels med CommunityToolkit.Mvvm (CompanyLookupViewModel som partial + ObservableProperty + RelayCommand)
  - BlazorWeb (Server-modus) med MudBlazor providers, Serilog, DI for Application/Infrastructure/ViewModel
  - **Build grønt, server starter, men "/"-route gir 404** — krever feilsøking av neste agent

### Inngangsmateriale i iterasjon 2

- Brukerens "Nå er vi klare!"-instruks
- Verktøy-installasjon utført av bruker (MAUI workload, Android SDK, gh CLI, GitHub SSH/HTTPS)
- 3 nye brukerkrav midt-iterasjon: design-spec-plassholder, lese-liste, Fase 6 backend oppgradert
- 1 nytt arkitekturkrav midt-iterasjon: register-aggregator (parallelle oppslag, plan 15)
- Brukerens hand-off-krav: oppdater memory + progress fortløpende

### Status ved iterasjons-slutt

- **Kode skrevet:** ~25 filer, 7 nye prosjekter, 2 commits
- **Tester:** 31 passerer, 0 feiler
- **WebApi:** kjørbar og verifisert
- **BlazorWeb:** kompilerer og starter, men UI ikke synlig (404 — blocker)
- **Tid igjen:** kontekst nær fullt utnyttet, brukeren ba om pause
- **Utestående blocker** for "se Web kjøre på Mac": Routes.razor/AdditionalAssemblies må fikses
- **Utestående for "Desktop på Mac"**: MAUI Desktop-prosjekt ikke laget
- **Utestående for "iOS-app m/ TestFlight-instruks"**: MAUI Mobile-prosjekt ikke laget

### Refleksjon

- Plan-iterasjoner og research tok mer enn forventet i starten — det betalte seg da koding gikk raskt fordi alle valg var avklart
- HybridCache-polymorfi var en uventet teknisk blocker som måtte løses (CachedLookup-wrapper)
- Strenge analyzer-regler (CA1707, CA1848, CA1305, etc.) ga friksjon — disabled i Directory.Build.props
- Blazor RCL + Routes.razor + AdditionalAssemblies er fortsatt fitkre å få til på første forsøk — typisk arbeid for ny sesjon
- Ny agent har komplett handoff via memory + plan-pakke + 2 git-commits

---

---

## Iterasjon 3 — Blazor 404-fiks + Fase 1 + Fase 3 + docs + GitHub-push (2026-05-24)

### Tidsstempler

| Hendelse | Tidspunkt |
| --- | --- |
| Sesjon startet (god morgen) | 2026-05-24 ~09:00 |
| Blazor 404 fikset (commit `3f0ede9`) | 2026-05-24 09:16 |
| MAUI Desktop kode commit (`eae058b`) | 2026-05-24 ~09:30 |
| Speech + MAUI Mobile commit (`7970513`) | 2026-05-24 ~09:55 |
| README + docs + slnf commit (`6a2e42b`) | 2026-05-24 ~10:10 |
| GitHub-repo opprettet + push | 2026-05-24 ~10:15 |

### Tidsbruk

| Måling | Verdi |
| --- | --- |
| **Total elapsed** | **~1 t 15 min** |
| Aktiv AI-skrivetid (estimat) | ~50 min |
| Brukerens reaksjons-tid (estimat) | ~25 min |
| Faktisk implementasjonskode + dokumentasjon | **~25 nye filer + 5 commits** |

### Artefakter produsert i iterasjon 3

| Type | Detalj |
| --- | --- |
| Bug-fiks | Blazor 404 (AddAdditionalAssemblies på MapRazorComponents) |
| Nye prosjekter | Speech (delt port-prosjekt) + MauiDesktop (Mac Catalyst + Windows) + MauiMobile (iOS + Android) + Speech.Tests |
| Nye tester | 16 NorskTallParser-tester (alle passerer) — totalt 47 tester nå |
| Solution Filter-filer | 4 stk: Core, Web, Desktop, Mobile |
| Dokumentasjon | README.md (rot) + COMMERCIAL-LICENSE.md + LICENSE (AGPL-3.0 fra gnu.org) + 5 install-guider + ide-overview |
| GitHub | Repo opprettet på <https://github.com/erlingsm/Bronnoysund.Lookup> og pushet |

### Status ved iterasjons-slutt

- **Total kode-base:** 10 .NET-prosjekter + 4 test-prosjekter
- **Tester:** 47/47 passerer (Domain 21 + Application 5 + Infrastructure 5 + Speech 16)
- **Fungerer i nettleser:** Blazor Web Server (port 5199) — verifisert HTTP 200 + MudBlazor-rendering
- **WebApi:** Verifisert mot ekte Brreg + cache-hit
- **MAUI Desktop + Mobile:** Kode komplett, bygg blokkert av kjent Microsoft-issue (MAUI 10 krever Xcode 26.4 eksakt)
- **GitHub:** Repository offentlig, README/LICENSE/COMMERCIAL-LICENSE leveres
- **iOS-app for testers:** Krever Xcode 26.4-fix før vi kan bygge IPA og laste til TestFlight

### Inngangsmateriale i iterasjon 3

- Hand-off fra iterasjon 2 (progress_brreg.md med detaljert blocker-beskrivelse)
- Brukerens "god morgen, les memory og fortsett"

### Status ved iterasjons-slutt — gjenstående hovedoppgaver

1. **MAUI-build venter på Xcode 26.4-installasjon** (brukerens action) eller MAUI-pakke-oppdatering
2. **Persistens (SQLite + EF Core)** — kan introduseres når MAUI bygger
3. **Plan 7 oppgradering** + Dockerfile + Helm for Fase 6 backend
4. **Presentasjons-skeleton** (statisk HTML i `/Dokumentasjon/Presentasjon/`)
5. **Verifikasjon i nettleser visuelt** — brukeren kan åpne <http://localhost:5199> og bekrefte at det funker

### Refleksjon

- Hand-off via `progress_brreg.md` fungerte presist — ny agent fant blocker, bestemte løsning, fikset den på 5 minutter
- Solution filter (.slnf) er fin måte å la ulike IDE-er laste forskjellige deler — Rider får alt, VS Code kan ha bare core
- Markdown-lint kopiert til alle docs-mapper holder kvaliteten konsekvent
- GitHub-push var smooth (gh CLI HTTPS-auth var pre-konfigurert)
- Xcode-versjons-issue er en kjent Microsoft-pain — vil løses ved oppdatering

---

## Iterasjon 4+ — Senere faser

(Plassholder. Hver senere fase får sin egen seksjon med samme struktur.)

---

## Akkumulert tidsbruk

| Iterasjon | Elapsed | AI-tid (est.) | Bruker-tid (est.) | Status |
| --- | --- | --- | --- | --- |
| 1: Spesifikasjon v1 | ~3 t 49 min | ~1,5–2 t | ~1–2 t | ✅ Fullført |
| 2: Fase 0 + Fase 2 WIP | ~6 t 30 min | ~3,5 t | ~3 t | ✅ Fase 0 ferdig, Fase 2 70 %, pauset |
| 3: Blazor-fiks + Fase 1/3 kode + docs + push | ~1 t 15 min | ~50 min | ~25 min | ✅ Web kjørbar, MAUI venter på Xcode |
| **Sum så langt** | **~11 t 34 min** | **~6 t AI** | **~5 t bruker** | — |

## Metode-notat (transparens)

Tidsstempler i «verifiserbart fra filsystem»-tabellen er hentet via `stat -f "%Sm" -t "%Y-%m-%d %H:%M:%S"` på faktiske filer i prosjektet og kan etterprøves. Estimater (aktiv AI-tid, bruker-tid) er anslag basert på antall iterasjoner, observert kompleksitet og typisk arbeidstempo — disse er ikke målte verdier og er markert som «estimat». Ved fremtidige iterasjoner kan vi forbedre estimatene ved å logge sesjon-start og sesjon-slutt eksplisitt.
