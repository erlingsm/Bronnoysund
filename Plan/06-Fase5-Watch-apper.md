# 06 — Fase 5: Watch-apper (watchOS + Wear OS) via companion-pattern

**Forutsetning:** Fase 3 ferdig (MAUI Mobile finnes som "backend" for watch).

## Mål

Lite grensesnitt på klokke der bruker kan si orgnr eller navn, og få svaret lest opp. Watch-appen er **thin client** — gjør ingen Brreg-kall selv. Telefon-appen er "backend".

## Hvorfor companion-pattern

| Fordel | Forklaring |
| --- | --- |
| Minst kode på klokken | Watch-appen er kun UI + voice + companion-message |
| Sparer batteri | Ingen nettverks-IO på klokken |
| Funker uten klokke-WiFi | Telefon kan ha forbindelse selv om klokken ikke har det |
| Felles cache | Telefonens HybridCache deles indirekte (klokken ser hva telefonen allerede har) |

## Protokoll (companion-message)

JSON-payload begge veier (samme protokoll på iOS/Android):

Watch → Phone:

```json
{ "version": 1, "action": "lookup", "value": "919300388" }
{ "version": 1, "action": "search", "value": "equinor" }
```

Phone → Watch:

```json
{ "version": 1, "result": "found", "organizationNumber": "919300388", "organizationName": "...", "companyType": "AS", "languageForm": "Bokmål" }
{ "version": 1, "result": "notFound", "message": "..." }
{ "version": 1, "result": "invalid", "message": "..." }
```

Definer protokollen i et eget delt schema-dokument (`docs/watch-protocol.md`) slik at både Swift- og Kotlin-koden refererer samme spec.

## watchOS-app (Swift + SwiftUI)

- Mappe: `/Kode/Watch/iOS/BrregWatch/`
- `ContentView` — knapp + tekst-felt + result-card
- `SpeechRecognizer` — Apple SFSpeechRecognizer
- `Companion` — wrapper rundt `WCSession.default.sendMessage(...)`
- TTS: AVSpeechSynthesizer

På iPhone-app (MauiMobile): legg til `WCSessionDelegate` som lytter etter messages, kaller `LookupCompanyHandler`, sender svaret tilbake.

## Wear OS-app (Kotlin + Compose)

- Mappe: `/Kode/Watch/Android/BrregWear/`
- Compose-UI tilsvarende watchOS
- `MessageClient.sendMessage(nodeId, path, payload)` til parent app
- TTS: Android TextToSpeech

På Android-app (MauiMobile): legg til `WearableListenerService` som mottar messages og kaller `LookupCompanyHandler`.

## DRY på watch — hva kan deles?

- **Protokoll-spec** deles via dokumentasjon
- **JSON-serialisering** av payload kan deles ved at både Swift og Kotlin har generert-kode fra samme schema (overveie i den fasen)
- Selve view-koden må være native — det er en bevisst trade-off

## UI-spesifikasjon (gjelder begge plattformer)

**Visning av selskapsdata:** Alle 4 MVP-felter vises (`organizationNumber`, `organizationName`, `companyType`, `languageForm`). På sikt utvides med flere felter etter hvert som scope vokser (Fase 4-utvidelser). Skjermen er **scrollbar** vertikalt.

**Visuell mal per data-rad:**

```text
┌─────────────────────────────┐
│ Organization Number         │  ← Label: tonet/sekundær farge, mindre skrift
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │  ← Diffus tynn linje (lav opacity)
│ 919300388                   │  ← Data: klar/primær farge, tydelig kropps-skrift
│                             │
│ Organization Name           │
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │
│ Equinor ASA                 │
│                             │
│ Company Type                │
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │
│ AS                          │
│                             │
│ Language Form               │
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │
│ Bokmål                      │
└─────────────────────────────┘
```

**Konkret:**

- Label-skrift: caption / footnote-størrelse, sekundær (tonet) farge
- Skille-linje: 0.5–1px, ~30 % opacity av primær tekstfarge — diffus, ikke dominerende
- Data-skrift: body / kroppstekst, primær farge — tydelig og lesbar
- Vertikal padding mellom rader: tilstrekkelig til at det er klart hvor én rad slutter og neste begynner

### watchOS (Swift + SwiftUI)

`CompanyDataRow`-komponent gjenbrukt for hver felt:

```swift
struct CompanyDataRow: View {
    let label: String
    let value: String

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(label)
                .font(.caption2)
                .foregroundColor(.secondary)
            Divider()
                .opacity(0.3)
            Text(value)
                .font(.body)
                .foregroundColor(.primary)
        }
        .padding(.vertical, 4)
    }
}

struct CompanyDetailsView: View {
    let response: CompanyResponse
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 8) {
                CompanyDataRow(label: "Organization Number", value: response.organizationNumber)
                CompanyDataRow(label: "Organization Name", value: response.organizationName)
                CompanyDataRow(label: "Company Type", value: response.companyType)
                CompanyDataRow(label: "Language Form", value: response.languageForm)
            }
            .padding()
        }
    }
}
```

### Wear OS (Kotlin + Compose for Wear)

Bruker `ScalingLazyColumn` (Wear OS-standard for scrollbare lister med focus-skalering):

```kotlin
@Composable
fun CompanyDataRow(label: String, value: String) {
    Column(modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp)) {
        Text(
            text = label,
            style = MaterialTheme.typography.caption1,
            color = MaterialTheme.colors.onSurfaceVariant
        )
        Divider(
            modifier = Modifier.fillMaxWidth().padding(vertical = 2.dp),
            color = MaterialTheme.colors.onSurface.copy(alpha = 0.3f),
            thickness = 0.5.dp
        )
        Text(
            text = value,
            style = MaterialTheme.typography.body1,
            color = MaterialTheme.colors.onSurface
        )
    }
}

@Composable
fun CompanyDetailsScreen(response: CompanyResponse) {
    ScalingLazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp)
    ) {
        item { CompanyDataRow("Organization Number", response.organizationNumber) }
        item { CompanyDataRow("Organization Name", response.organizationName) }
        item { CompanyDataRow("Company Type", response.companyType) }
        item { CompanyDataRow("Language Form", response.languageForm) }
    }
}
```

### Designprinsipper på klokkeskjerm

- **Énhets-skjerm-fokus:** Bruker scroller for å se mer, ikke sveiper. Vertikal flyt.
- **Kontrast:** Følger systemets dark mode automatisk. Test både lys og mørk.
- **Lesbarhet på liten skjerm:** Aldri mindre enn 11pt body-tekst. Aldri små "blokk-tegn" som gjør lange labels uleselige.
- **TTS-knapp øverst eller nederst:** Stor, lett å treffe — "Les opp" trigger `AVSpeechSynthesizer` (iOS) / `TextToSpeech` (Android) med hele svaret formatert som naturlig setning.
- **Tilbake-navigasjon:** Apple Watch crown / Wear OS swipe-right — standard plattform-mønster.

## Standalone watchOS — ikke støttet

Watch-appene fungerer **kun med parret telefon**. Ingen HTTP-klient på klokken. Ingen offline-cache på klokken (telefonen er backenden).

Hvis telefonen ikke er nådbar via WatchConnectivity / Wearable Data Layer:

- Vis melding: *"Telefonen er ikke tilgjengelig. Åpne appen på telefonen din."*
- Tilby retry-knapp

Beslutningen begrunnes med: minimum kode på klokken, batteri-besparelse, og ingen duplisert auth/cache-logikk.

## Verifikasjon

- Watch-simulator på Mac (Xcode), Wear-emulator på Android Studio
- Sett opp telefon-simulator parret med klokke-simulator
- Si "ni en ni tre null null tre åtte åtte" på klokken → telefon utfører lookup → klokke viser alle 4 felter scrollbart → trykk "Les opp" → klokke leser
- Test scenario hvor telefon er av/utenfor rekkevidde → ser tydelig feilmelding
- Test både lys og mørk modus på klokken
- Test at scroll-flyt er smooth med 4 felter og senere med 8-10 felter
