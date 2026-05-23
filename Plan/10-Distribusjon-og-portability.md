# 10 — Distribusjon og portability (tverrgående)

Dette dokumentet binder sammen krav fra `Oppdrag/ReadMe` og brukerens A/B/C-presisering. Det refereres fra Fase 1, 2, 3 og 5.

## Prinsipper

| Prinsipp | Konsekvens |
| --- | --- |
| **Portable** | Web og Desktop skal kunne kjøres fra en mappe (også USB-stick) uten installer |
| **Lavest mulig avhengighet** | Standard: forventer `.NET 10 Runtime` installert; tilbyr `self-contained` som tilleggsbundle for null avhengigheter |
| **Sideload først, App Store senere** | iOS + Android leveres som sideload-bundles i demo-fasen; oppdateres til App Store-publisering når sertifikater og demoer er trygge |
| **Samme artefakt der det går** | Bruker `dotnet publish` med RID-spesifikke bundles for hver plattform, men *samme* solution |

## A) Web — tre kjøre-modus

Vi velger **Blazor Web App i Server-modus** (i stedet for ren WASM) for å støtte alle tre kjøre-modus med samme artefakt:

### A1) Lokalt på PC (Windows, .NET 10 Runtime installert)

1. Last ned ZIP `Brreg.BlazorWeb-win-x64.zip`
2. Pakk ut til hvor som helst (også USB-stick)
3. Dobbeltklikk `Brreg.BlazorWeb.exe` — Kestrel starter på `http://localhost:5000`
4. Standard nettleser åpnes automatisk via `BrowserLauncher` (kall `Process.Start` med URL ved startup)

### A2) Lokalt på Mac (.NET 10 Runtime installert)

1. Last ned ZIP `Brreg.BlazorWeb-osx-arm64.zip` (eller `osx-x64` for Intel Mac)
2. Pakk ut hvor som helst
3. Terminal: `./Brreg.BlazorWeb` (eller dobbeltklikk hvis vi pakker som `.command`-fil)
4. Samme: Kestrel + nettleser åpnes automatisk

### A3) Dedikert webserver med .NET 10 Runtime

1. Last ned ZIP `Brreg.BlazorWeb-linux-x64.zip` (eller relevant RID)
2. Pakk ut til `/var/www/brreg-lookup/`
3. Lag systemd-service eller bruk reverse-proxy bak nginx/Apache
4. Sett miljøvariabler (ASPNETCORE_URLS, ASPNETCORE_ENVIRONMENT, ev. evt. Kestrel-port)
5. README-fil i Web-bundle beskriver alle stegene + eksempel systemd-unit

### Bygge-kommandoer

```bash
# Per plattform (alle bruker samme prosjekt, framework-dependent som standard):
dotnet publish src/Brreg.BlazorWeb -c Release -r win-x64    --self-contained false -o dist/web/win-x64
dotnet publish src/Brreg.BlazorWeb -c Release -r osx-arm64  --self-contained false -o dist/web/osx-arm64
dotnet publish src/Brreg.BlazorWeb -c Release -r osx-x64    --self-contained false -o dist/web/osx-x64
dotnet publish src/Brreg.BlazorWeb -c Release -r linux-x64  --self-contained false -o dist/web/linux-x64

# Self-contained (større, men ingen Runtime-krav) — som ekstra valgfri bundle:
dotnet publish src/Brreg.BlazorWeb -c Release -r win-x64    --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o dist/web-sc/win-x64
# ... osv
```

Resultat: én katalog per RID, hver kan zip-pakkes og legges på USB-stick.

### Hvorfor Server-modus og ikke ren WASM?

- WASM krever en HTTP-server uansett (kan ikke åpnes som `file://` direkte)
- Server-modus gir oss en `.exe` som starter alt — én artefakt, dobbeltklikk-vennlig
- Cache (HybridCache) fungerer bedre i Server (in-memory per prosess vs in-tab i WASM)
- Vi mister "100% kjør i nettleser" — men vi vinner enkel portable-deploy

Hvis WASM-versjon er ønskelig senere (kjør i 100% browser), kan vi legge det til som en *ekstra* host (`Brreg.BlazorWasm`) som gjenbruker samme `Brreg.Components`-RCL.

## B) Desktop — portable

### MAUI på Windows (unpackaged)

- Bygg med `WindowsPackageType=None` → gir `.exe + DLLs` som kan kjøres fra mappe (også USB)
- Kommando: `dotnet publish src/Brreg.MauiDesktop -c Release -f net10.0-windows10.0.19041.0 -r win-x64 -p:WindowsPackageType=None -p:PublishReadyToRun=true -o dist/desktop/win-x64`
- ZIP-pakke for distribusjon
- README forklarer at Windows kan vise SmartScreen-advarsel første gang (workaround: "More info" → "Run anyway")

### MAUI på Mac (.app bundle)

- Mac Catalyst: `dotnet publish src/Brreg.MauiDesktop -c Release -f net10.0-maccatalyst -p:CreatePackage=false -o dist/desktop/osx-arm64`
- Resultat: `Brreg.MauiDesktop.app`-bundle som kan dras til Applications-mappen *eller* kjøres fra hvilken som helst mappe (inkludert USB)
- README beskriver Gatekeeper-bypass første gang: høyreklikk → Åpne → bekreft
- Hvis vi har Apple Developer ID-sertifikat: signer med `codesign` så Gatekeeper godkjenner uten advarsel

### Trade-off: Desktop MAUI vs Web Server-bundle

For en "lite trafikk"-portable app er det rimelig argument for å kun levere Web Server-bundle og hoppe over MAUI Desktop helt:

- Samme UI (Blazor-komponenter)
- Enklere portable
- Færre artefakter å vedlikeholde

Vi velger likevel MAUI Desktop som del av planen fordi: hjemmeoppgaven (utvidet) krever det, og det demonstrerer ekte cross-platform native-bygg. Web Server-bundle blir et **ekstra** tilbud.

## C) Mobile — utenfor App Store + plan for App Store

### Android — sideload (demo)

- `dotnet publish src/Brreg.MauiMobile -c Release -f net10.0-android -p:AndroidPackageFormat=apk -o dist/mobile/android`
- Resultat: `Brreg.MauiMobile-Signed.apk` (signert med debug-keystore i demo, prod-keystore senere)
- README: kopier APK til telefon (USB/email/Drive), slå på "Install unknown apps" for kilde-appen, tap for å installere
- Krav til signing for sideload: debug-keystore er greit; prod-keystore er nødvendig for Play Store

### Android — Play Store (senere)

- Bygg AAB (Android App Bundle): `-p:AndroidPackageFormat=aab`
- Krav: Google Play Developer Account ($25 engangsavgift)
- Last opp i Play Console, fyll inn metadata + screenshots, gjennomgå
- Tidsbruk: ~1-7 dager review

### iOS — sideload (demo)

To muligheter:

**C1) Gratis Apple ID-cert (7-dagers utløp):**

- Krever Xcode på Mac + iPhone tilkoblet
- Åpne MAUI iOS-prosjektet i Visual Studio for Mac / kjør via `dotnet build -f net10.0-ios`
- Deploy via Xcode til tilkoblet enhet
- App utløper hver 7. dag — må reinstallere via Xcode
- Greit for demo hvis fysisk tilstede

**C2) Apple Developer Program ($99/år):**

- Ad-hoc distribution: registrer enhetenes UDID i Apple Developer Console (maks 100)
- Bygg signert IPA: `dotnet publish ... -p:ArchiveOnBuild=true`
- Distribuer IPA via Diawi, Firebase App Distribution eller TestFlight (TestFlight er enklest)
- TestFlight: app gyldig i 90 dager, brukere bekrefter ved invitasjon

**Anbefaling for demo:** TestFlight hvis $99 Developer Program er innen scope; ellers gratis Xcode-deploy hvis bruker har Mac + iPhone fysisk.

### iOS — App Store (senere)

- Krever Apple Developer Program ($99/år)
- App Store Connect — fyll inn metadata, screenshots, privacy-info, in-app purchases (ingen for oss)
- Apple Review (~24-48 timer typisk for første versjon)

### Watch-apper (Fase 5) — distribusjon

- **watchOS:** Følger med iOS-appen — installeres som "Watch App" når iPhone-appen installeres på en parret klokke. Samme distribusjonskanal (sideload eller TestFlight).
- **Wear OS:** Egen APK eller del av telefonens APK. Sideload via ADB eller Play Store (Wear OS-tab).

## Eldre OS-versjoner — vurdering

| Plattform | MAUI 10 minimum | Vår valgte minimum | % av aktive enheter | Rasjonale |
| --- | --- | --- | --- | --- |
| iOS | 15.0 | **15.0** | ~95% (iPhone 6s og nyere, fra 2015) | MAUI 10 setter gulvet; ikke verdt å forsøke å gå lavere |
| Android | API 21 (5.0) | **API 24 (7.0)** | ~95% av aktive enheter | API 21 er minimum, men 24 gir bedre TLS-default og moderne API-er; vi mister kun ~1% brukere |
| macOS Catalyst | 15.0 | **15.0** | Alle Mac fra 2017+ | Catalyst-default for MAUI 10 |
| Windows | 10.0.17763 (1809) | **10.0.19041 (2004)** | ~99% av Windows 10/11 | Standard MAUI-target |
| watchOS | 9.0+ | **9.0** | Apple Watch Series 4+ (fra 2018) | Native Swift, vi bestemmer selv |
| Wear OS | API 30 (Wear OS 3) | **API 30** | De fleste moderne Wear-klokker | Native Kotlin |

**Konklusjon:** "Eldre OS-versjoner" er begrenset av MAUI 10 sine minimumskrav. iOS 15+, Android 7+ er praktisk gulv som gir ~95%+ rekkevidde.

## Eye candy — pragmatisk omfang

Uten å miste tid på animasjoner som ikke verdi-bidrar:

- **Tema:** Bruk `MudBlazor` eller `Radzen.Blazor` i RCL — gir polert default-look uten store CSS-investeringer
- **Animasjoner:** CSS transitions (fade-in på cards, smooth loading-spinner)
- **Ikoner:** Material Symbols / Heroicons (gratis SVG)
- **Mørk modus:** Følger systemets preference via CSS `prefers-color-scheme`
- **Mobil:** Touch-targets ≥44px, swipe-friendly liste
- **Watch:** Minimalistic — stor tekst, ett primært element per skjerm

Trade-off med "støtte eldre OS": de eldre versjonene støtter ikke alle moderne CSS-features (f.eks. `container queries`). Hold deg til CSS Grid + Flexbox + `prefers-color-scheme` som har bredest støtte. Test på simulator med iOS 15 og Android 7.

## Sammendrag av distribusjons-artefakter (per release)

```text
dist/
├── web/
│   ├── win-x64/         (zip) — krever .NET 10 Runtime
│   ├── osx-arm64/       (zip)
│   ├── osx-x64/         (zip)
│   └── linux-x64/       (zip)
├── web-sc/              (self-contained varianter, opsjonelt)
├── desktop/
│   ├── win-x64/         (zip) — MAUI unpackaged
│   ├── osx-arm64/       (.app i .zip)
│   └── osx-x64/         (.app i .zip)
└── mobile/
    ├── android/         (.apk + .aab)
    └── ios/             (.ipa når sertifikat finnes)
```

Bygge-script i `scripts/build-release.sh` og `scripts/build-release.ps1` produserer hele settet.

## Avklart (status post-review)

| Spørsmål | Avgjørelse |
| --- | --- |
| Apple Developer Program | ✅ Brukeren har det → **TestFlight** for iOS-demo. Sluttbrukere trenger bare en vanlig Apple ID, ikke Developer-konto |
| Google Play Developer Account | ✅ Brukeren har det → **Play Internal Testing** for Android-demo. Sluttbrukere trenger bare Google-konto |
| Mac code signing | ✅ Apple Developer Program inkluderer Developer ID-sertifikat — Mac-bundles signeres ved bygg |
| UI-bibliotek | ✅ **MudBlazor** (MIT, Material Design) i RCL |
| Speech: on-device vs cloud | ✅ **On-device** via `CommunityToolkit.Maui.Media` (MAUI) + Web Speech API (Blazor). Ingen cloud-avhengighet |
| Norsk tall-til-siffer | ✅ Egen lett parser i `Bronnoysund.Lookup.Speech/Parsing/NorskTallParser.cs` (~30 linjer) |
