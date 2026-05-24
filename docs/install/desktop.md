# Kjør Desktop-versjonen på Mac eller Windows

Desktop-versjonen er en native MAUI Blazor Hybrid-app som bruker samme UI-komponenter som web.

## ⚠️ Kjent issue (mai 2026): Xcode 26.4 kreves på Mac

Microsoft.MacCatalyst.Sdk 26.4.10259 (følger med .NET MAUI 10) krever **eksakt** Xcode 26.4. Hvis du har Xcode 26.5 (eller nyere) får du:

```text
error : This version of .NET for MacCatalyst (26.4.10259) requires Xcode 26.4.
        The current version of Xcode is 26.5.
```

**Løsning A — installer Xcode 26.4 ved siden av:**

1. Last ned Xcode 26.4 fra <https://developer.apple.com/download/all/>
2. Pakk ut til f.eks. `/Applications/Xcode_26.4.app`
3. Bytt aktiv Xcode:

   ```bash
   sudo xcode-select -s /Applications/Xcode_26.4.app/Contents/Developer
   ```

4. Verifiser:

   ```bash
   xcodebuild -version
   ```

5. Bygg som beskrevet under.

**Løsning B — vent på MAUI-pakke som støtter Xcode 26.5.** Microsoft pleier å publisere oppdatert Xcode-kompatibilitet innen få uker etter Apple-release.

## Forutsetning

- .NET 10 SDK med MAUI workload:

  ```bash
  dotnet workload install maui
  ```

- Xcode 26.4 (se over) — kun for Mac-build

## Bygg og kjør på Mac (M3 / Apple Silicon)

```bash
cd Kode
dotnet build src/Bronnoysund.Lookup.MauiDesktop \
    -f net10.0-maccatalyst \
    -p:RuntimeIdentifier=maccatalyst-arm64

# Kjør:
open src/Bronnoysund.Lookup.MauiDesktop/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Bronnoysund\ Lookup.app
```

## Bygg og kjør på Windows

```powershell
cd Kode
dotnet build src/Bronnoysund.Lookup.MauiDesktop `
    -f net10.0-windows10.0.19041.0 `
    -r win-x64

# Kjør:
start src\Bronnoysund.Lookup.MauiDesktop\bin\Debug\net10.0-windows10.0.19041.0\win10-x64\Bronnoysund.Lookup.MauiDesktop.exe
```

## Bygg portable distribusjon (Mac, drag-drop)

```bash
dotnet publish src/Bronnoysund.Lookup.MauiDesktop \
    -c Release \
    -f net10.0-maccatalyst \
    -p:CreatePackage=false \
    -o dist/desktop/osx-arm64
```

Resultatet er `dist/desktop/osx-arm64/Bronnoysund Lookup.app` som kan kopieres til Applications-mappen eller kjøres fra USB-stick.

**Første gang du åpner:** Gatekeeper kan blokkere fordi appen ikke er signert med ditt Apple Developer ID. Workaround: høyreklikk → Åpne → bekreft.

For signert distribusjon: se [docs/development/signing.md](../development/signing.md) (kommer i en senere release).

## Bygg portable distribusjon (Windows, unpackaged)

```powershell
dotnet publish src/Bronnoysund.Lookup.MauiDesktop `
    -c Release `
    -f net10.0-windows10.0.19041.0 `
    -r win-x64 `
    -p:WindowsPackageType=None `
    -p:PublishReadyToRun=true `
    -o dist/desktop/win-x64
```

Resultatet er en mappe `dist/desktop/win-x64/` med `Bronnoysund.Lookup.MauiDesktop.exe` + alle DLL-er. Kopier hele mappen til USB / dele-server / annen PC — kjør `.exe` direkte uten installasjon.

**SmartScreen-advarsel første gang:** "More info" → "Run anyway".
