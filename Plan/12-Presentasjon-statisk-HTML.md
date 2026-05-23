# 12 — Presentasjon (statisk HTML)

Fra `Oppdrag/Presentasjon.md`:

> Under mappen "Dokumentasjon/Presentasjon", vil jeg at du skal opprette dokumentasjonen som statiske HTML-sider som vi kan bruke til presentasjonen.
> Vi skal være åpne om at vi bruker Claude og AI til oppgaven.
> Vi trenger noen sider til å presentere alle deler av applikasjonen.
> Vi trenger kildehenvisning.
> Vi må svare ut alle spørsmål som ligger i oppgaven.

Dette er et **flere-sides statisk dokumentasjons-nettsted**, ikke bare ett slide-deck. Slide-deck (reveal.js) er én av sidene, ikke hele leveransen.

## Format og verktøy

**Ren HTML + CSS, ingen byggesteg.**

Hvorfor:

- Maksimal portabilitet — dobbeltklikk `index.html`, eller hostes som GitHub Pages
- Ingen Node/npm/build-pipeline å vedlikeholde
- Matcher prosjektets "portable"-tema (samme prinsipp som Web/Desktop-bundlene)
- Lett å oppdatere uten å risikere build-feil

Verktøy:

- **Mermaid** (via CDN, også bundled offline) for arkitektur-diagrammer
- **Prism.js** for syntax-highlighting av kode-snippets
- **reveal.js** for én side med slide-deck-modus

Ingen frameworks (ingen Docusaurus / VitePress / MkDocs) — for å holde det null-avhengighet.

## Mappestruktur

```text
/Dokumentasjon/Presentasjon/
├── index.html                     — landing / oversikt + navigasjon
├── om-oppgaven.html               — den opprinnelige oppgaven + utvidet scope (med Q&A-tabell)
├── ai-bruk.html                   — åpenhet om Claude/AI + arbeidsmetode
├── arkitektur.html                — Clean Arch + Ports & Adapters m/ Mermaid-diagrammer
├── patterns.html                  — patterns-katalog m/ kode-snippets og litteratur-referanser
├── platforms/
│   ├── desktop.html               — MAUI Desktop (Mac+Win), portable-distribusjon
│   ├── web.html                   — Blazor Web (Server-modus), tre kjøre-modus
│   ├── mobile.html                — MAUI Mobile (iOS+Android), voice
│   └── watch.html                 — watchOS + Wear OS, companion-pattern
├── robusthet-caching-testing.html — direkte svar på de opprinnelige kvalitetskravene
├── kildehenvisninger.html         — alle krediteringer (speil av docs/credits.md)
├── faq.html                       — Q&A: alle spørsmål fra oppgaven + brukerens egne
├── slidedeck.html                 — reveal.js slide-versjon av hovedpunktene
├── assets/
│   ├── css/
│   │   ├── site.css               — felles styling, lys/mørk modus
│   │   └── print.css              — pent print for handouts
│   ├── img/
│   │   ├── logo.svg
│   │   ├── arkitektur-clean.svg
│   │   ├── arkitektur-ports.svg
│   │   ├── companion-pattern.svg
│   │   └── screenshots/           — skjermbilder av kjørende app per plattform
│   ├── js/
│   │   └── nav.js                 — felles topp-meny som inkluderes på alle sider
│   └── vendor/                    — bundled Mermaid + Prism + reveal.js (offline)
└── README.md                      — hvordan kjøre/vise presentasjonen
```

## Felles navigasjon (på toppen av hver side)

```text
Hjem | Oppgaven | AI-bruk | Arkitektur | Patterns | [Desktop ▼ Web ▼ Mobile ▼ Watch ▼] | Robusthet & test | Kilder | FAQ | ▶ Slide-deck
```

Implementeres som en `nav.js` som injiseres med samme innhold i alle sider (en liten `<script src="assets/js/nav.js">` i `<head>`). Holdes konsistent uten templating-engine.

## Side-for-side: hva besvares hvor?

### `index.html` — Landing

- Tittel + bakgrunn ("En venn av meg utfordret meg...")
- Veikart over de andre sidene
- Lenke til repo og til live demo

### `om-oppgaven.html` — Oppgaven

- Sammendrag av den opprinnelige oppgavebriefen (MVP-krav)
- Brukerens utvidede scope
- Stor sjekkliste-tabell: krav → hvilken side / kode-fil / test som dekker

### `ai-bruk.html` — Åpenhet om AI

- Hvordan Claude (Claude Code) brukes
- Min rolle: kravstiller, Code Review, justering, håndkoding
- Begrunnelse: kjent terreng, godt dokumentert, AI er produktiv
- Hva som er menneske-håndkodet vs AI-generert (gjennomgang per modul)

### `arkitektur.html` — Arkitektur

- Mermaid-diagram av Clean Architecture-lagene
- Mermaid-diagram av Ports & Adapters (porter med pilene)
- Hvorfor disse valgene — kort tekst per lag
- Kobling til neste side (patterns)

### `patterns.html` — Patterns

- Tabell-katalog: pattern → hvor i koden → kilde-litteratur
- Kode-snippets for de viktigste (Strategy, Decorator, Value Object, Polly-pipeline, HybridCache cache-aside)
- Hver pattern peker til konkret fil i `/src/`

### `platforms/desktop.html` — MAUI Desktop

- Hva: portable Mac+Win app
- Hvordan: MAUI Blazor Hybrid hosting RCL
- Distribusjon: ZIP, unpackaged Win, .app for Mac, USB-vennlig
- Screenshots
- Lenke til install-README

### `platforms/web.html` — Blazor Web

- Hva: portable Web Server-bundle som kjøres lokalt PC/Mac eller på server
- Tre kjøre-modus (A1/A2/A3)
- Hvorfor Server-modus, ikke WASM
- Screenshots
- Lenke til install-README (standalone + server)

### `platforms/mobile.html` — MAUI Mobile

- iOS + Android, MAUI multi-target
- Voice input (norsk siffer-til-tall, navn-søk)
- Sideload + TestFlight + Play Store Internal Testing
- OS-minstekrav iOS 15+, Android 7+
- Screenshots

### `platforms/watch.html` — Watch

- Companion-pattern (klokken = thin client, telefon = backend)
- Mermaid-sekvensdiagram av flyten
- watchOS Swift + Wear OS Kotlin
- Hvorfor native, ikke MAUI (MAUI støtter ikke watchOS)

### `robusthet-caching-testing.html` — opprinnelige kvalitetskrav

- Robusthet: Polly v8 pipeline (Retry, CircuitBreaker, Timeout, Bulkhead)
- Caching: HybridCache som decorator, 24t TTL, hvorfor 24t
- Testing: pyramide (Unit på Domain/Application, Integration via WireMock.Net, bUnit på komponenter)
- Kode-snippets fra hver

### `kildehenvisninger.html` — Kilder

- Frank.Libraries.Brreg, organisationsnummer/csharp, SindreMA, storbukas/norsk-validator (inspirasjon — krediteres, ikke kopiert)
- NuGet-pakker brukt (cross-cutting concerns)
- Litteratur: Martin, Cockburn, Evans, Fowler, GoF, Nygard
- Brreg NLOD 2.0-lisens

### `faq.html` — Q&A

Alle spørsmål fra oppgaven, eksplisitt besvart:

| Spørsmål (kilde) | Svar / hvor |
| --- | --- |
| Hvordan løst oppgaven? (opprinnelig oppgave) | Se `arkitektur.html` |
| Hvordan strukturert koden? (opprinnelig oppgave) | `arkitektur.html` + `patterns.html` |
| Hvordan tenkt rundt robusthet? (opprinnelig oppgave) | `robusthet-caching-testing.html` |
| Hvordan tenkt rundt caching? (opprinnelig oppgave) | `robusthet-caching-testing.html` |
| Hvordan tenkt rundt testing? (opprinnelig oppgave) | `robusthet-caching-testing.html` |
| Hvilke patterns? (bruker) | `patterns.html` |
| Hvilke arkitektur-regler? (bruker) | `arkitektur.html` |
| Sjekket eksisterende kode? (bruker) | `kildehenvisninger.html` + `ai-bruk.html` |
| Hvordan dyp-dykk i Brreg-API? (bruker) | `om-oppgaven.html` + `arkitektur.html` (Infrastructure-seksjon) |
| MAUI/Blazor/iOS/Android/Watch? (bruker) | `platforms/*.html` |
| Voice input + readback? (bruker) | `platforms/mobile.html` + `platforms/watch.html` |
| Validering (MOD11)? (bruker) | `patterns.html` (Value Object-seksjon med kode-snippet) |
| Fremtidige registre (gjeld/regnskap/eierskap)? (bruker) | `arkitektur.html` (stub-ports-seksjon) |

### `slidedeck.html` — Selve presentasjonen

- reveal.js (bundled i `assets/vendor/`)
- ~18 slides som koker ned hovedpoengene fra docs-sidene
- Kjøres lokalt via dobbeltklikk eller fullskjerm i nettleser
- Slide-disposisjon i appendiks under

## Slide-disposisjon (slidedeck.html)

1. Tittel
2. Bakgrunn (vennens utfordring)
3. Åpenhet om AI
4. Oppgaven (MVP kort)
5. Demo (link / QR til kjørende app)
6. Arkitektur — Clean Architecture-diagram
7. Arkitektur — Ports & Adapters-diagram
8. Robusthet — Polly v8
9. Caching — HybridCache
10. Testing — pyramide
11. Cross-platform — RCL + Blazor Hybrid + Domain/Application overalt
12. Watch — companion-pattern
13. Patterns og litteratur
14. Hvorfor reimplementere (Frank.Libraries.Brreg finnes)
15. Kildehenvisninger
16. Veien videre — Fase 1-6
17. Svar på oppgaven — eksplisitt sjekkliste
18. Spørsmål?

Hver slide har "speaker notes" (reveal.js `<aside class="notes">`) som ikke vises i fullskjerm, brukes som talepunkter.

## Når lages den?

| Fase | Hva legges til |
| --- | --- |
| Fase 0 (nå) | Skeleton — `index.html`, `om-oppgaven.html`, `ai-bruk.html`, `arkitektur.html`, `patterns.html`, `robusthet-caching-testing.html`, `kildehenvisninger.html`, `faq.html`. Slide-deck-skissen. Screenshots utelates inntil videre. |
| Fase 1 | `platforms/desktop.html` ferdigstilles + screenshots fra MAUI Desktop |
| Fase 2 | `platforms/web.html` ferdigstilles + screenshots fra Blazor Web (alle tre kjøre-modus) |
| Fase 3 | `platforms/mobile.html` ferdigstilles + screenshots + voice-demo (kort video?) |
| Fase 5 | `platforms/watch.html` ferdigstilles + screenshots fra simulator |
| Sist | Endelig polering av slidedeck.html + ev. opptak/screencast av demo |

## Åpen om AI — kjernebudskap (`ai-bruk.html`)

> Dette prosjektet er bygget i tett samarbeid med Anthropics Claude (via Claude Code).
>
> Min rolle som menneske:
>
> - **Innledende research** — jeg gjorde grundig research hos Brønnøysundregistrene (datasett, API-er, lisens, MOD11-spec for organisasjonsnummer) før AI ble satt på oppgaven. Det grunnlaget styrte hva som ble bygget og hvilke begrensninger som ble bakt inn fra start.
> - **Kravstiller** — definerte arkitektur, valg av patterns, scope, leveransemodell og lisens (AGPL-3.0 + kommersiell). AI fikk konkrete instruksjoner, ikke åpne mandater.
> - **Plan-review** — gikk gjennom hver plan-fil systematisk og kommenterte underveis. Mine kommentarer ble eksplisitt markert med prefikset "Erling sin kommentar:" og ble adressert i nye iterasjoner før vi skrev kode.
> - **Code Reviewer** — alt AI-generert kode skal gjennomgås av meg før commit. Ingen blind merge.
> - **Justering** — endrer retning underveis basert på funn. Flere plan-iterasjoner ble revidert etter mine kommentarer (f.eks. Web-deploy-modell, lisens-modell, persistens-scope, watch-arkitektur).
> - **Håndkoder** — der det er mer effektivt enn å brenne tokens (typisk korte fikser, format-justeringer, presise små endringer).
>
> Begrunnelse: Brreg-integrasjon, Clean Architecture og MAUI/Blazor er **kjent terreng** som ligger godt dokumentert på GitHub, Microsoft Learn og StackOverflow. AI er svært produktivt på den type oppgaver. Det jeg som menneske bidrar med — domene-research, arkitektur-valg, kritisk gjennomgang og retnings-endringer — er det AI ikke gjør alene.
>
> Det vi vinner: hastighet på det rutinemessige, slik at jeg kan bruke tid på domene-forståelse, arkitektur-valg, kvalitetssikring og at løsningen faktisk demonstrerer det som etterspørres.

## DoD for Fase 0-skelettet av presentasjonen

- [ ] Mappestruktur opprettet under `/Dokumentasjon/Presentasjon/`
- [ ] `index.html` med navigasjon fungerer i nettleser (dobbeltklikk)
- [ ] Alle ikke-plattform-sidene har minst en førsteversjon med tekst
- [ ] `kildehenvisninger.html` er komplett (speilet fra `docs/credits.md`)
- [ ] `faq.html` har komplett Q&A-tabell (selv om enkelte svar peker til "kommer i Fase X")
- [ ] `slidedeck.html` har 18 slides med talepunkter
- [ ] CSS er rent, fungerer i lys og mørk modus
- [ ] Funker offline (alle CDN-avhengigheter også bundled i `assets/vendor/`)
