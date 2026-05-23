# 14 — IDE- og verktøy-mapping

Brukeren har flere IDE-er tilgjengelig og vil kunne åpne hvert del-prosjekt i den IDE-en som er best egnet. Denne planen styrer solution-strukturen og dokumenterer hvilket verktøy som brukes til hva.

## Brukerens verktøypark

| Plattform | Tilgjengelige IDE-er |
| --- | --- |
| **Mac (M3)** | JetBrains Rider, Xcode, Visual Studio Code, alle JetBrains-produkter (IntelliJ IDEA, AppCode, etc.), Android Studio |
| **Windows** | Visual Studio (full edition), JetBrains Rider |

Claude-agent kjører i VS Code (Claude Code-extension) under spesifikasjon og koding. Brukeren bruker andre IDE-er for review, debugging, simulator-test og fin-tuning.

## Filosofi: én solution + filtrerte slnf

For å gjøre det enkelt å laste bare relevant del i hver IDE bruker vi **Solution Filter (`.slnf`)** — en standard .NET-mekanisme støttet av Rider, Visual Studio og VS Code.

**Filer som opprettes i Fase 0+:**

```text
/Kode/
├── Bronnoysund.Lookup.sln                    — komplett solution (alle .NET-prosjekter)
├── Bronnoysund.Lookup.Web.slnf               — kun WebApi + BlazorWeb + kjerne + tester
├── Bronnoysund.Lookup.Desktop.slnf           — kun MauiDesktop + kjerne + tester
├── Bronnoysund.Lookup.Mobile.slnf            — kun MauiMobile + kjerne + tester
├── Bronnoysund.Lookup.Core.slnf              — kun kjernebibliotek (Domain/Application/Infrastructure) + tester
├── src/
│   └── (alle .NET-prosjekter — se faseplaner)
├── tests/
│   └── (alle .NET-test-prosjekter)
└── Watch/
    ├── iOS/BronnoysundWatch.xcodeproj/       — Xcode-prosjekt (Swift + SwiftUI), står utenfor .sln
    └── Android/BronnoysundWear/               — Gradle-prosjekt (Kotlin + Compose), står utenfor .sln
```

**Hvorfor `.slnf` og ikke flere `.sln`:** Solution Filter peker inn i én underliggende `.sln` og er bare et utvalg av prosjekter som skal lastes. Endringer i kjernebibliotek er automatisk synlig i alle filtre. Versjons-styres som vanlige tekstfiler.

## Verktøy-matrise per prosjekt

| Prosjekt / del | Beste IDE | Alternativer | Begrunnelse |
| --- | --- | --- | --- |
| `Bronnoysund.Lookup.Domain` | Rider, VS Code | Visual Studio (Win) | Ren `net10.0` — alle IDE-er fungerer likt |
| `Bronnoysund.Lookup.Application` | Rider, VS Code | Visual Studio (Win) | Samme |
| `Bronnoysund.Lookup.Infrastructure` | Rider, VS Code | Visual Studio (Win) | Samme |
| `Bronnoysund.Lookup.Infrastructure.Persistence` | Rider, VS Code | Visual Studio (Win) | EF Core code-first migrations — Rider og VS har grafiske verktøy for migrations |
| `Bronnoysund.Lookup.WebApi` | Rider, VS Code, Visual Studio | — | ASP.NET Core — alle har god støtte. Hot reload fungerer overalt |
| `Bronnoysund.Lookup.BlazorWeb` | **Rider** (Mac+Win), Visual Studio (Win) | VS Code med C# DevKit | Blazor-debugging er best i Rider/VS; VS Code med C# DevKit har grei støtte men færre verktøy |
| `Bronnoysund.Lookup.Components` (RCL) | Rider, VS Code | Visual Studio | Razor-syntax støttes overalt; live preview er bedre i Rider/VS |
| `Bronnoysund.Lookup.ViewModels` | Alle | — | Ren C# |
| `Bronnoysund.Lookup.MauiDesktop` (Mac+Win) | **Rider** (Mac+Win), Visual Studio (Win) | — | MAUI på Mac: Rider er beste valg etter Visual Studio for Mac ble discontinued (slutten på 2024). Windows: Visual Studio er fortsatt en bra kandidat for MAUI Windows-target |
| `Bronnoysund.Lookup.MauiMobile` (iOS+Android) | **Rider på Mac** (for iOS-build), Visual Studio på Windows (kun Android-target) | — | iOS-bygg krever Mac uansett. Rider på Mac støtter både iOS- og Android-target. Visual Studio på Windows kan ikke bygge iOS uten "pair-to-Mac" — ofte friksjon |
| `Watch/iOS/BronnoysundWatch` | **Xcode** | AppCode (deprecated, ikke anbefalt) | Swift + SwiftUI + WatchKit krever Xcode |
| `Watch/Android/BronnoysundWear` | **Android Studio** | IntelliJ IDEA Ultimate (samme grunnplattform) | Kotlin + Compose for Wear + Wear OS-emulator |
| Tester (alle `.Tests`-prosjekter) | Alle .NET-IDE-er | — | xUnit-test-runners finnes overalt |

## Hvordan åpne i hver IDE — kort oppskrift

### JetBrains Rider (Mac + Windows)

**Åpne hele løsningen:**

1. `File → Open...` → velg `/Kode/Bronnoysund.Lookup.sln`
2. Rider restore-er pakker automatisk
3. Velg run-konfigurasjon i toolbar (f.eks. `Bronnoysund.Lookup.WebApi`) → grønn play-knapp

**Åpne kun en del (anbefalt for store solutions):**

1. `File → Open...` → velg en `.slnf`, f.eks. `Bronnoysund.Lookup.Web.slnf`
2. Bare relevante prosjekter lastes; raskere indeksering

**Debugging:**

- Sett breakpoint i kode → trykk play-knapp med "bug"-ikon (eller `Ctrl+D` / `Cmd+D`)
- Rider støtter conditional breakpoints, watch expressions, og inspect-vindu

**MAUI-spesifikt:**

- Rider på Mac viser MAUI workload-status og kan installere ved behov
- iOS-simulator-valg ligger i dropdown ved play-knappen
- Android-emulator må startes fra Android Studio eller `avdmanager`, men kjøres mot fra Rider

### Visual Studio (Windows)

**Åpne hele løsningen:**

1. `File → Open → Project/Solution...` → velg `Bronnoysund.Lookup.sln`
2. NuGet restore skjer automatisk

**Velg startup-prosjekt:**

1. Høyreklikk på prosjektet i Solution Explorer (f.eks. `Bronnoysund.Lookup.WebApi`)
2. `Set as Startup Project`
3. F5 for å kjøre med debugger

**Begrensninger på Windows:**

- iOS-bygging krever pairing til Mac via "Pair to Mac" (Visual Studio Tools → iOS) — kan være friksjon. Rider på Mac er enklere når iOS-bygg trengs.

### Visual Studio Code (Mac + Windows)

**Forutsetninger:**

- Installer ekstensjon: **C# Dev Kit** (Microsoft)
- Installer ekstensjon: **.NET MAUI** (Microsoft) — hvis MAUI-prosjekter skal redigeres

**Åpne:**

1. `File → Open Folder...` → velg `/Kode/`
2. C# Dev Kit oppdager `.sln` automatisk og viser Solution Explorer i sidepanelet
3. `Run and Debug`-tab (Ctrl+Shift+D / Cmd+Shift+D) → velg konfigurasjon

**For BlazorWeb-debugging:**

- `launch.json` genereres automatisk av C# Dev Kit
- F5 starter Kestrel + browser-debugging

**Begrensninger:**

- MAUI-bygging i VS Code fungerer, men hot-reload og XAML-designer er bedre i Rider eller Visual Studio
- iOS-simulator-styring er bedre i Rider/Xcode

### Xcode (Mac)

**Brukes for:** `Watch/iOS/BronnoysundWatch.xcodeproj` (Swift + SwiftUI watch-app)

**Åpne:**

1. `File → Open...` → velg `Watch/iOS/BronnoysundWatch.xcodeproj` (eller `.xcworkspace` om vi senere legger til SPM-pakker)
2. Velg scheme: `BronnoysundWatch` (eller spesifikk target hvis flere)
3. Velg simulator: Apple Watch-modeller i dropdown
4. `Cmd+R` for å bygge og kjøre

**Debug:**

- Breakpoints med klikk i gutter
- Variables-view, Console
- Lab. Watch Connectivity-debugging: må ha **både** iPhone-simulator og Watch-simulator parret (Xcode → Window → Devices and Simulators)

**Signering for fysisk Watch/iPhone:**

- Xcode → Settings → Accounts → Apple ID med Developer Program-konto registrert
- Project Settings → Signing & Capabilities → velg Team

### Android Studio (Mac + Windows)

**Brukes for:** `Watch/Android/BronnoysundWear/` (Kotlin + Compose Wear OS-app)

**Åpne:**

1. `File → Open...` → velg `Watch/Android/BronnoysundWear/` (mappa med `build.gradle.kts`)
2. Gradle sync (~1–2 min første gang)
3. Velg run-konfigurasjon: `app` (eller spesifikk modul)
4. Velg Wear OS-emulator: `Tools → Device Manager → Create Device → Wear`
5. Grønn play-knapp

**Debug:**

- Logcat-vinduet viser app-logger
- Breakpoints + Debug Watches fungerer som forventet

**Wear-emulator pairing med phone-emulator:**

- Android Studio Device Manager → opprett en phone-emulator
- I Wear-emulatoren: Settings → "Pair with phone emulator" via ADB-bridge
- Eller: `adb -s <wear-emulator> forward tcp:5601 tcp:5601` for Wear OS companion-debugging

## Simulator-oppsett (én gang per maskin)

### iOS-simulator (Mac)

- Følger med Xcode. Installer Xcode fra App Store eller developer.apple.com
- Åpne én gang: `Xcode → Open Developer Tool → Simulator`
- Hent flere iOS-versjoner: `Xcode → Settings → Platforms → Get`

### Android-emulator (Mac + Windows)

- Følger med Android Studio
- `Tools → Device Manager → Create Device` → velg telefon-modell + Android-versjon (anbefalt: API 34+)
- For Wear: `Create Device → Wear OS-tab`

### MAUI workloads (én gang per Mac/Windows)

```bash
dotnet workload install maui
# Verifiser:
dotnet workload list
```

Krever ofte sudo på Mac første gang.

## Plattform-spesifikke notater

### Mac M3 (Apple Silicon, arm64)

- Alle .NET 10-prosjekter målretter `osx-arm64` for native ytelse
- MAUI Catalyst: `net10.0-maccatalyst` — kjør og debug i Rider med native arm64-binær
- iOS-simulator: arm64-simulator-bilder (raskt på M3 — nær native hastighet)
- Android-emulator: x86_64-bilder anbefales fortsatt for breddetest (de fleste Android-enheter er arm, men Google sin "system images" arm64-er fungerer også på M3)
- Xcode 15+ kreves for moderne SDK-er

### Windows

- For iOS-build: enten Rider på Mac eller "Pair to Mac" fra Visual Studio (krever en Mac i nettverket)
- For Android-build: Visual Studio eller Rider støtter dette direkte uten Mac
- Windows-spesifikk MAUI: `net10.0-windows10.0.19041.0` target

## Hvilke IDE-er gjør hva i en typisk arbeidsflyt

| Oppgave | Anbefalt verktøy på Mac | Anbefalt verktøy på Windows |
| --- | --- | --- |
| Endre delt kjernebibliotek (Domain/Application) | Rider eller VS Code | Visual Studio eller Rider |
| Utvikle Blazor Web UI (Razor + CSS) | Rider | Visual Studio |
| Debugge WebApi-endepunkt | Rider | Visual Studio |
| Kjøre alle .NET-tester | Rider eller VS Code | Visual Studio eller Rider |
| MAUI Desktop på Mac (Mac Catalyst) | Rider | (krever Mac — gjøres på Mac) |
| MAUI Desktop på Windows | (krever Win — gjøres på Win) | Visual Studio |
| MAUI iOS-app bygging + simulator | Rider på Mac | Pair-to-Mac fra VS, men friksjon — ofte enklere å gjøre på Mac |
| MAUI Android-app bygging + emulator | Rider | Visual Studio eller Rider |
| watchOS-app i Swift (Apple Watch) | Xcode | (krever Mac) |
| Wear OS-app i Kotlin (Wear-emulator) | Android Studio | Android Studio |
| Database-inspeksjon (SQLite-fila) | Rider DataGrip-plugin eller DB Browser for SQLite | DB Browser for SQLite |
| Git og PR | VS Code eller terminal | VS Code eller terminal |

## README-leveranser (når lages hva)

Følgende `/docs/development/`-filer lages og lever sammen med kode-leveransene:

| Fil | Når lages | Beskriver |
| --- | --- | --- |
| `/docs/development/ide-overview.md` | Fase 0 | Denne plan-filens tabeller i sluttbruker-vennlig form |
| `/docs/development/ide-rider.md` | Fase 0 | Hvordan åpne hver `.slnf` i Rider på Mac/Win |
| `/docs/development/ide-visual-studio.md` | Fase 0 | Hvordan åpne `.sln` i Visual Studio (Win) |
| `/docs/development/ide-vscode.md` | Fase 0 | C# Dev Kit-oppsett + launch.json-eksempler |
| `/docs/development/ide-xcode.md` | Fase 5 | Watch-prosjektets oppsett i Xcode |
| `/docs/development/ide-android-studio.md` | Fase 5 | Wear-prosjektets oppsett i Android Studio |
| `/docs/development/simulators-setup.md` | Fase 3 | iOS-simulator + Android-emulator + Wear-emulator |
| `/docs/development/maui-workloads.md` | Fase 1 | `dotnet workload install maui` per plattform |

`Plan/11-README-strategi.md` oppdateres med disse filene.

## Verifikasjon

Når kode-leveransen er klar (Fase 0+), kan brukeren bekrefte at:

- `Bronnoysund.Lookup.sln` åpner uten feil i Rider på Mac
- `Bronnoysund.Lookup.Web.slnf` åpner i VS Code med kun Web-relaterte prosjekter
- `dotnet build` kjører fra terminal uten å åpne IDE
- Hvert fase-prosjekt har tilhørende `/docs/development/ide-*.md`-fil med konkrete steg
