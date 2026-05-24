# Installer Android-appen

Vi tilbyr to distribusjons-veier:

- **Play Store Internal Testing** (anbefalt for testere — automatiske oppdateringer)
- **APK-sideload** (for de som ikke vil bruke Play Store)

## A) Play Store Internal Testing (anbefalt)

**Du trenger kun en Google-konto** — ingen Google Play Developer-konto kreves.

1. Du får en e-post fra `post@roasystemutvikling.no` med en opt-in-lenke
2. Åpne lenken på Android-telefonen i nettleser → logg inn med Google-kontoen
3. Trykk **Become a tester**
4. Trykk lenken til **Download it on Google Play**
5. Play Store åpnes → trykk **Install**
6. Appen oppdateres automatisk når vi publiserer nye versjoner

## B) APK-sideload

Hvis du ikke vil bruke Play Store:

1. Last ned `Bronnoysund.Lookup.MauiMobile.apk` fra [Releases](https://github.com/erlingsm/Bronnoysund.Lookup/releases) (når publisert)
2. På Android-telefonen: åpne **Settings → Apps → Special app access → Install unknown apps** → velg nettleseren du brukte til å laste ned (f.eks. Chrome) og slå på **Allow from this source**
3. Åpne den nedlastede APK-en → trykk **Install**
4. Trykk **Open**

**Oppdateringer:** Last ned ny APK manuelt og installer på samme måte. Ingen automatiske oppdateringer.

## For utviklere

### Forutsetning

- .NET 10 SDK + MAUI workload
- **Android SDK** med Platform 35+ og Build-tools 35+
- Android Studio (anbefalt — installerer SDK + emulatorer)

Hvis du har bare command-line tools (`brew install --cask android-commandlinetools`):

```bash
# Installer nødvendige SDK-komponenter
sdkmanager "platforms;android-35" "build-tools;35.0.0" "platform-tools"
```

Sett miljøvariabel (Mac):

```bash
export ANDROID_HOME=/opt/homebrew/share/android-commandlinetools
export PATH=$PATH:$ANDROID_HOME/platform-tools
```

### Bygg signert APK (debug-keystore, for sideload)

```bash
cd Kode
dotnet publish src/Bronnoysund.Lookup.MauiMobile \
    -c Release \
    -f net10.0-android \
    -p:AndroidPackageFormat=apk \
    -p:RuntimeIdentifier=android-arm64
```

Resultat: `bin/Release/net10.0-android/android-arm64/publish/no.roasystemutvikling.bronnoysundlookup.mobile-Signed.apk`

### Bygg AAB for Play Store

```bash
dotnet publish src/Bronnoysund.Lookup.MauiMobile \
    -c Release \
    -f net10.0-android \
    -p:AndroidPackageFormat=aab
```

Last opp resulterende `.aab` i Google Play Console → Internal Testing.

### Test på fysisk telefon eller emulator

```bash
# List enheter
adb devices

# Bygg + installer + kjør
dotnet build src/Bronnoysund.Lookup.MauiMobile \
    -t:Run \
    -f net10.0-android
```

For å starte emulator:

```bash
emulator -list-avds
emulator -avd <navn>
```

(Hvis du ikke har AVD: opprett i Android Studio Device Manager.)
