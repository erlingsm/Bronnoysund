# Installer iOS-appen på iPhone

iOS-appen distribueres via **TestFlight** så lenge vi er i demo-/testfase. Den blir publisert til App Store når den er klar for det.

## For sluttbrukere (deg som skal teste appen)

**Du trenger kun en vanlig Apple ID** — ingen Apple Developer Program-konto kreves.

1. Installer **TestFlight** fra App Store på iPhone (gratis app fra Apple)
2. Du får en e-post fra `post@roasystemutvikling.no` med en invitasjons-lenke
3. Åpne lenken på iPhone → TestFlight åpnes → trykk **Accept**
4. Trykk **Install** for å laste ned Bronnoysund Lookup-appen
5. Appen ligger nå i hjemskjermen og oppdateres automatisk når nye versjoner publiseres

**Gyldighet:** TestFlight-builds er gyldige i 90 dager. Du får automatisk varsel om å oppdatere før utløp.

## For utviklere (deg som skal bygge appen selv)

### Forutsetning

- Mac med Xcode 26.4 (se [docs/install/desktop.md](desktop.md) for Xcode-versjons-issue)
- .NET 10 SDK + MAUI workload (`dotnet workload install maui`)
- Apple Developer Program-konto (du har) — krever signing certificate i Keychain

### Bygg signert IPA

```bash
cd Kode
dotnet publish src/Bronnoysund.Lookup.MauiMobile \
    -c Release \
    -f net10.0-ios \
    -p:ArchiveOnBuild=true \
    -p:RuntimeIdentifier=ios-arm64 \
    -p:CodesignKey="Apple Distribution: Røa Systemutvikling AS (TEAM-ID)"
```

Erstatt `TEAM-ID` med din egen 10-tegns Team ID fra Apple Developer Portal.

Resultat: `src/Bronnoysund.Lookup.MauiMobile/bin/Release/net10.0-ios/ios-arm64/publish/Bronnoysund.Lookup.MauiMobile.ipa`

### Last opp til TestFlight

**Med Transporter (anbefalt — gratis fra Mac App Store):**

1. Åpne **Transporter**-appen
2. Drag-drop `Bronnoysund.Lookup.MauiMobile.ipa` inn i appen
3. Trykk **DELIVER**
4. Vent ~5–15 min på Apple-prosessering
5. Logg inn på <https://appstoreconnect.apple.com> → Bronnoysund Lookup → TestFlight
6. Legg til testere (max 10 000 eksterne) ved e-postadresse
7. Apple gjør en lett review (typisk ~24 timer for første versjon, deretter umiddelbart)

**Med xcrun (terminal):**

```bash
xcrun altool --upload-app \
    -f Bronnoysund.Lookup.MauiMobile.ipa \
    -t ios \
    --apiKey YOUR_KEY \
    --apiIssuer YOUR_ISSUER
```

### Test på fysisk iPhone uten TestFlight

For rask iterasjon under utvikling:

1. Koble iPhone til Mac via USB
2. I terminal:

   ```bash
   dotnet build src/Bronnoysund.Lookup.MauiMobile \
       -t:Run \
       -f net10.0-ios \
       -p:RuntimeIdentifier=ios-arm64 \
       -p:_DeviceName=":v2:udid=<DIN-UDID>"
   ```

   Finn UDID med `xcrun xctrace list devices`.

Appen installeres direkte. Krever at iPhone er i utviklermodus (Settings → Privacy & Security → Developer Mode).

## Veien videre — App Store-publisering

Når demo-fase er ferdig:

1. Lever endelig versjon via samme bygg/upload-flyt
2. I App Store Connect: lag App Store-listing (metadata, screenshots, beskrivelse, privacy-info)
3. Submit for review
4. Apple-review tar 24–48 timer for første versjon

App Store-listing er planlagt som senere milepæl.
