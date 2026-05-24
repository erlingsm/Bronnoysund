# Kjøre og debugge Bronnoysund.Lookup

Praktisk oppskrift per plattform: hvilket verktøy, hvordan starte, hvordan sette breakpoint, og hvordan inspisere data.

## Web-applikasjonen (Blazor Server)

**Verktøy:** JetBrains Rider (anbefalt på Mac) eller Visual Studio (Windows). **WebStorm passer ikke** — det er IDE for JavaScript/TypeScript-frontend, mens vår Web er C#/Blazor Server-side. Hele rendering skjer på server.

### Rider — kjøre og debugge

1. `File → Open` → `Kode/Bronnoysund.Lookup.Web.slnf` (bare Web-relaterte prosjekter lastes; raskere indeksering).
2. Toolbar dropdown: velg `Bronnoysund.Lookup.BlazorWeb: http`.
3. **Kjør:** grønn play-knapp eller `Ctrl+R` (Mac: `Ctrl+R`).
4. **Debug:** play-knapp med bug-ikon eller `Ctrl+D` (Mac: `Ctrl+D`). Browser åpner http://localhost:5199.

### Breakpoints

- C# i `Pages/Lookup.razor.cs`, `ViewModels/CompanyLookupViewModel.cs`, `Infrastructure/Brreg/BrregCompanyProvider.cs` — klikk i gutter (venstre marg) eller `F9`.
- Razor-markup i `Lookup.razor`: Rider støtter breakpoints i `@code`-blokk og i C#-uttrykk på `@()`-form.
- **Conditional breakpoint:** høyreklikk på breakpoint → `More` → angi expression (f.eks. `OrgNumber == "974760843"`).

### Debug-vinduer (Rider)

- **Variables** — lokale variabler i nåværende stack frame.
- **Watches** — uttrykk som evalueres ved hver step (f.eks. `ViewModel.IsBusy`).
- **Call Stack** — hopp opp/ned i kallkjeden.
- **Immediate Window** — kjør C# mens debugger er på pause.

### Inspeksjon av Blazor SignalR / DOM

Blazor Server kommuniserer via SignalR. For å se hva som faktisk renderes:
- I browseren (Chrome/Edge/Safari): `F12` → `Network`-fanen → filtrer på `WebSocket` → finn `_blazor`-tilkoblingen → `Messages` viser alle render-batches.
- DOM: `Elements`-fanen som vanlig.

### Hot reload

Endringer i `.razor`/`.cs` aktiveres uten restart hvis Rider kjører med "Hot Reload" på (toolbar-knapp). For Razor-markup-endringer: bare lagre. For C#-signaturer (ny metode): "Apply Code Changes"-knapp.

### Logger

`logs/bronnoysund-lookup-web-{dato}.log` — Serilog ruller daglig. Console viser samme.

---

## Desktop-appen (MAUI Blazor Hybrid på Mac Catalyst)

**Verktøy:** JetBrains Rider på Mac.

### Forutsetning

```bash
# Sjekk Xcode 26.4.1 er aktiv (ikke 26.5 — MAUI 10.0 har eksakt 26.4-krav):
xcodebuild -version

# Hvis 26.5: bytt til 26.4-kopi:
sudo xcode-select -s /Applications/Xcode-kopi.app/Contents/Developer
```

### Rider — kjøre

1. Åpne `Kode/Bronnoysund.Lookup.Desktop.slnf`.
2. Run-konfigurasjon (toolbar): `Bronnoysund.Lookup.MauiDesktop: net10.0-maccatalyst | maccatalyst-arm64`.
3. Grønn play-knapp eller `Ctrl+R`.

Hvis run-konfigurasjon mangler:
- `Run → Edit Configurations → + → .NET MAUI → Project: MauiDesktop → Framework: net10.0-maccatalyst → RID: maccatalyst-arm64`.

### Rider — debugge

C#-debugging i MauiProgram/MainPage/App fungerer som vanlig — sett breakpoint, kjør med Debug.

**Blazor-komponenter (Razor) inne i BlazorWebView:**
- C# i `@code`-blokk og i C#-filer (ViewModels, services): sett breakpoint i Rider direkte.
- JavaScript/CSS i WebView: bruk **Safari Web Inspector**:
  1. Safari: `Preferences → Advanced → Show Develop menu`.
  2. Med MAUI-appen kjørende: `Develop → <din Mac> → Bronnoysund Lookup → index.html` — full DevTools mot WebView.

### Manuelt fra kommandolinje

```bash
cd Kode

# Bygg + kjør Debug (snabbeste utviklings-loop):
dotnet build src/Bronnoysund.Lookup.MauiDesktop -f net10.0-maccatalyst -p:RuntimeIdentifier=maccatalyst-arm64
open "src/Bronnoysund.Lookup.MauiDesktop/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Bronnoysund Lookup.app"

# Eller Release-bundle for distribusjon (~436 MB; trimming av):
dotnet publish src/Bronnoysund.Lookup.MauiDesktop -c Release -f net10.0-maccatalyst -p:RuntimeIdentifier=maccatalyst-arm64 -p:CreatePackage=false
```

### SQLite-fila for Desktop

```bash
~/Library/Containers/no.roasystemutvikling.bronnoysundlookup.desktop/Data/Library/bronnoysund.db
```

Åpne med [DB Browser for SQLite](https://sqlitebrowser.org/) eller Rider DataGrip-plugin for å inspisere tabellene (`LookupHistory`, `Favorites`, `CacheEntries`, `AppSettings`, `RegisterEndpoints`).

---

## iOS-appen (MAUI på iPhone/iPad)

**Verktøy:** JetBrains Rider på Mac. Xcode brukes implisitt for simulator + signering, men du kan utvikle hovedsakelig i Rider.

### Forutsetninger

| Avhengighet | Status |
|---|---|
| Xcode 26.4.1 | ✅ Installert som `Xcode-kopi.app` |
| iOS Simulator | ✅ Følger med Xcode |
| Apple Developer Program-konto | Påkrevd for fysisk enhet / TestFlight, ikke for simulator |
| MAUI workload | `dotnet workload list` skal vise `maui` |

### Rider — kjøre i simulator

1. Åpne `Kode/Bronnoysund.Lookup.Mobile.slnf`.
2. Run-konfigurasjon: `Bronnoysund.Lookup.MauiMobile: net10.0-ios`.
3. Device-dropdown: velg en iOS-simulator (iPhone 15 Pro eller lignende).
4. Grønn play-knapp eller `Ctrl+R`. Første gang tar 2–3 min (kompilering + simulator-boot).

### Debugging

Breakpoints i C# fungerer i simulator. WebView-debugging via Safari (samme oppskrift som Desktop, men velg simulatorens vindu under `Develop → Simulator`).

### Fysisk iPhone

1. Koble iPhone til Mac via USB.
2. Xcode (én gang): `Settings → Accounts` → legg til Apple ID med Developer Program.
3. Xcode: åpne `Window → Devices and Simulators` → marker iPhone → trust hvis prompt.
4. I Rider: Device-dropdown viser nå iPhone-en din.

For å distribuere til testere: bygg signert IPA via Xcode (åpne `*.xcodeproj` som .NET genererer under build), upload til **App Store Connect → TestFlight**.

### Manuelt fra kommandolinje

```bash
cd Kode

# List tilgjengelige simulatorer:
xcrun simctl list devices

# Bygg og deploy til en spesifikk simulator (eksempel UDID):
dotnet build src/Bronnoysund.Lookup.MauiMobile -f net10.0-ios \
    -p:RuntimeIdentifier=iossimulator-arm64 \
    /t:Run /p:_DeviceName=:v2:udid=<UDID>
```

### SQLite-fila i iOS-simulator

```bash
~/Library/Developer/CoreSimulator/Devices/<UDID>/data/Containers/Data/Application/<APP-UUID>/Library/bronnoysund.db
```

Finn `<APP-UUID>` ved å lete: `find ~/Library/Developer/CoreSimulator -name "bronnoysund.db"`.

---

## Android-appen (MAUI på Android-emulator)

**Verktøy:** JetBrains Rider eller Android Studio. Rider er enklere hvis du allerede har C#-fokus; Android Studio gir bedre Android-spesifikke verktøy (Layout Inspector, Profiler).

### Forutsetninger

| Avhengighet | Status |
|---|---|
| Android command-line tools | ✅ Installert (`/opt/homebrew/share/android-commandlinetools`) |
| Android SDK platforms 35+ | ⚠️ **Mangler** — kun command-line tools per nå |
| Android Emulator | ⚠️ Installeres som del av fullt SDK |
| Java OpenJDK 26 | ✅ Installert |

**Installer manglende SDK-komponenter:**

```bash
sdkmanager --install "platforms;android-35" "build-tools;35.0.0" "emulator" "system-images;android-35;google_apis;arm64-v8a"
sdkmanager --licenses   # godkjenn alle
```

Alternativt: åpne Android Studio → `Tools → SDK Manager` → marker `Android 15 (API 35)` + `Android Emulator` + Intel/ARM system image → Apply.

### Opprett Android-emulator

I Android Studio: `Tools → Device Manager → Create Device → Phone-fanen → Pixel 7 → Android 15 (API 35) → Finish`. Start med play-knappen.

Fra terminal (uten Android Studio):

```bash
avdmanager create avd -n Pixel7_API35 -k "system-images;android-35;google_apis;arm64-v8a" -d pixel_7
emulator -avd Pixel7_API35 &
```

### Rider — kjøre

1. Åpne `Kode/Bronnoysund.Lookup.Mobile.slnf`.
2. Med emulator kjørende: device-dropdown viser den.
3. Run-konfigurasjon: `Bronnoysund.Lookup.MauiMobile: net10.0-android`.
4. Grønn play-knapp eller `Ctrl+R`.

### Android Studio — kjøre via terminal

Android Studio åpner ikke MAUI-prosjekter direkte (Kotlin/Gradle-orientert). Brukes for emulator + verktøy. Selve appen bygges via Rider eller `dotnet`:

```bash
cd Kode
dotnet build src/Bronnoysund.Lookup.MauiMobile -f net10.0-android /t:Run
```

### Debugging

- **C#-breakpoints:** Rider, samme som Desktop/iOS.
- **Logcat (Android logger):** Android Studio → `View → Tool Windows → Logcat` → filtrer på `com.companyname.bronnoysundlookup.mobile`. Eller terminal: `adb logcat | grep -i bronnoysund`.
- **WebView DevTools:** Chrome → `chrome://inspect/#devices` → finn `Bronnoysund Lookup` → `inspect`.

### SQLite-fila i Android-emulator

```bash
adb shell run-as com.companyname.bronnoysundlookup.mobile cat files/bronnoysund.db > local.db
# eller hent hele app-mappa:
adb shell run-as com.companyname.bronnoysundlookup.mobile ls files/
```

---

## Tester (alle plattformer)

```bash
cd Kode
dotnet test                              # alle 72 tester
dotnet test Bronnoysund.Lookup.Core.slnf # bare kjerne + WebApi-tester
```

I Rider: `Tests`-vinduet (`Run → Unit Tests → All Tests`).

I VS Code med C# Dev Kit: `Testing`-fane i sidebar viser test-treet.

---

## Felles oppskrift: hot reload mens du utvikler

| Komponent | Hot reload |
|---|---|
| Razor-markup (`.razor`) | Lagre fila — endring vises umiddelbart i kjørende app (Web + Hybrid). |
| C# i ViewModel/service | Lagre fila — Rider/VS bytter ut metode-body live. Signatur-endringer krever `Apply Code Changes`-knapp. |
| CSS / wwwroot | Lagre fila — browser refresher (Web), eller `Cmd+Shift+R` i MAUI WebView. |
| MAUI XAML | Lagre — hot reload fungerer på Mac Catalyst i Rider. |
| EF Core entitet-endring | Skjema-endring krever ny migration (`dotnet ef migrations add ...`); ikke hot-reloadable. |
