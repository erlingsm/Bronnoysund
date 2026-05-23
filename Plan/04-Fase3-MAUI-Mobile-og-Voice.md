# 04 — Fase 3: MAUI Mobile (iOS + Android) + Voice

**Forutsetning:** Fase 1 + 2 ferdig.

## Mål

Mobile apper på iOS og Android fra én kodebase, med **on-device** voice input ("si orgnr" eller "si firmanavn") og voice output ("les svaret"). Ingen cloud-avhengighet for tale.

## Nye prosjekter

- `src/Bronnoysund.Lookup.MauiMobile/` — `net10.0-ios;net10.0-android` multi-target
- `src/Bronnoysund.Lookup.Speech/` — `net10.0` delt prosjekt: porter + norsk tall-til-siffer-parser
- `src/Bronnoysund.Lookup.Speech.Maui/` — MAUI-implementasjon (CommunityToolkit.Maui.Media + Microsoft.Maui.Essentials)
- `src/Bronnoysund.Lookup.Speech.Web/` — Blazor JS-interop-implementasjon (Web Speech API)

## Ports & Adapters for Speech

Definert i `Bronnoysund.Lookup.Application/Ports/` (eller `Speech`-prosjektet hvis vi vil holde det isolert):

```csharp
public interface ISpeechToText
{
    /// <summary>Lytt på mikrofon og returner gjenkjent tekst (eller null hvis avbrutt/feilet).</summary>
    Task<string?> ListenAsync(string languageCode, CancellationToken ct);
    bool IsAvailable { get; }
}

public interface ITextToSpeech
{
    Task SpeakAsync(string text, string languageCode, CancellationToken ct);
    bool IsAvailable { get; }
}
```

Plattform-prosjekter registrerer riktig implementasjon i DI.

## Innebygd talegjenkjenning per plattform (alle on-device, alle gratis)

| Plattform | Underliggende API | Hvordan vi når den |
| --- | --- | --- |
| iOS | `SFSpeechRecognizer` (Apple Speech framework) | Via **CommunityToolkit.Maui.Media** (MIT, Microsoft-vedlikeholdt) |
| Android | `SpeechRecognizer` (Android system) | Via **CommunityToolkit.Maui.Media** |
| macOS Catalyst | `SFSpeechRecognizer` (samme som iOS) | Via **CommunityToolkit.Maui.Media** |
| Windows | `Windows.Media.SpeechRecognition` (WinRT) | Via **CommunityToolkit.Maui.Media** |
| Web (Blazor) | Web Speech API (`SpeechRecognition` i nettleser) | JS-interop fra `Bronnoysund.Lookup.Speech.Web` |
| watchOS (Fase 5) | `SFSpeechRecognizer` direkte | Native Swift |
| Wear OS (Fase 5) | `SpeechRecognizer` direkte | Native Kotlin |

**Konklusjon:** Vi har én pakke — `CommunityToolkit.Maui.Media` — som dekker MAUI på alle 4 plattformer (iOS/Android/Mac/Win). For Web bruker vi nettleserens Web Speech API direkte via JS-interop. For watch bruker native APIs (Fase 5). Ingen cloud, ingen ekstra kostnad.

## Innebygd talesyntese (TTS) per plattform

| Plattform | API | Hvordan vi når den |
| --- | --- | --- |
| iOS / macOS | `AVSpeechSynthesizer` | Via **Microsoft.Maui.Essentials.TextToSpeech** (innebygd i MAUI) |
| Android | `android.speech.tts.TextToSpeech` | Samme |
| Windows | `Windows.Media.SpeechSynthesis` | Samme |
| Web | Web Speech API `SpeechSynthesisUtterance` | JS-interop |
| Watch | Plattform-native | Native Swift/Kotlin |

Alle har norsk bokmål-stemmer ut av boksen.

## Norsk talegjenkjenning — språk-koder

- iOS / macOS: `nb-NO`
- Android: `nb-NO` (kan kreve at brukeren laster ned norsk pack i Google Speech Services første gang)
- Windows: `nb-NO` (kan kreve at norsk lokaliserings-pakke er installert)
- Web: `nb-NO` (avhenger av nettleser; Chrome/Edge har best støtte)

Vi bruker `nb-NO` som default. `nn-NO` (nynorsk) kan tilbys som setting i konfigurasjons-siden.

## Norsk tall-til-siffer — bygg selv (lett)

**Vurdering:** Det finnes ingen moden NuGet-pakke for norsk "tall-tekst-til-siffer". Humanizer har den motsatte veien (NumberToWords), men ikke parser. Heldigvis er behovet vårt enkelt — orgnr på 9 siffer leses typisk siffer-for-siffer.

**Strategi:**

1. Etter `ISpeechToText.ListenAsync()` får vi en streng som `"ni en ni tre null null tre åtte åtte"` eller `"9 1 9 3 0 0 3 8 8"`.
2. `NorskTallParser.TryParseDigits(string input, out string digits)` — splitt på whitespace, mapp hvert ord til sin sifferverdi via lookup-tabell, returner konkatenert streng.
3. Validér resultat mot `OrganizationNumber.TryCreate` — hvis OK, gjør lookup.
4. Hvis ikke gyldig orgnr, fall tilbake til **navn-søk** (Fase 4) med rå transcript.
5. Hvis verken orgnr-parse eller navn-søk gir treff, vis: *"Vi forstod ikke. Si tallene én for én, eller skriv inn manuelt."*

**Lookup-tabell:**

```csharp
internal static class NorskTallParser
{
    private static readonly IReadOnlyDictionary<string, char> WordToDigit =
        new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase)
        {
            ["null"] = '0', ["zero"] = '0',
            ["en"] = '1', ["ett"] = '1', ["one"] = '1',
            ["to"] = '2', ["two"] = '2',
            ["tre"] = '3', ["three"] = '3',
            ["fire"] = '4', ["four"] = '4',
            ["fem"] = '5', ["five"] = '5',
            ["seks"] = '6', ["six"] = '6',
            ["sju"] = '7', ["syv"] = '7', ["seven"] = '7',
            ["åtte"] = '8', ["otte"] = '8', ["eight"] = '8',
            ["ni"] = '9', ["nine"] = '9'
        };

    public static bool TryParseDigits(string input, out string digits)
    {
        var sb = new StringBuilder(9);
        foreach (var token in input.Split(' ', '\t', '\n', '-', ',', '.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (char.IsDigit(token[0]) && token.All(char.IsDigit))
                sb.Append(token);
            else if (WordToDigit.TryGetValue(token, out var d))
                sb.Append(d);
            else
            {
                digits = string.Empty;
                return false;
            }
        }
        digits = sb.ToString();
        return digits.Length > 0;
    }
}
```

Plassert i `Bronnoysund.Lookup.Speech/Parsing/NorskTallParser.cs`. Testet i `Bronnoysund.Lookup.Speech.Tests`. Engelske ord (`one`, `nine`) tas med fordi noen brukere kan bytte mellom språk eller dictionaries kan mistolke.

## UI-utvidelser

- `VoiceInputButton.razor` (i RCL) — mikrofon-knapp; spør om tillatelser; viser interim transcript mens brukeren snakker
- `CompanyCard.razor` — "Les svaret"-knapp som kaller `ITextToSpeech.SpeakAsync`
- "Si organisasjonsnummer eller bedriftsnavn"-prompt over input-feltet

## Tillatelser (manifestendringer)

- **iOS Info.plist:** `NSSpeechRecognitionUsageDescription`, `NSMicrophoneUsageDescription` (begge med norsk-vennlig forklaringstekst)
- **Android AndroidManifest.xml:** `<uses-permission android:name="android.permission.RECORD_AUDIO" />`
- **Windows Package.appxmanifest:** `microphone` capability
- **macOS Info.plist:** `NSMicrophoneUsageDescription`, `NSSpeechRecognitionUsageDescription`

`INTERNET`-permission er **ikke** påkrevd for vår speech (på Android er det implisitt for de fleste apper uansett, men siden vi bruker on-device speech er det ikke nødvendig for talegjenkjenningen). Brreg-kall trenger selvsagt nett.

## OS-minstekrav (se `10-Distribusjon-og-portability.md` for hele matrisen)

- **iOS:** 15.0 (gulv satt av MAUI 10) — ~95% av aktive iPhones
- **Android:** API 24 / 7.0 Nougat — ~95% av aktive enheter

## Eye candy — pragmatisk

- UI-bibliotek i RCL: MudBlazor (Material Design, MIT) — bekreftet valg
- CSS transitions, Material Symbols/Heroicons SVG, `prefers-color-scheme` for mørk modus
- Pulserende mikrofon-knapp under lytting (CSS keyframes)
- Test alltid på iOS 15-simulator og Android 7-emulator for å bekrefte at det funker på gulvet

## Distribusjon — sluttbrukere trenger IKKE Developer-konto

Vi har Apple Developer Program og Google Play Developer Account. Sluttbrukere trenger bare en vanlig Apple ID / Google-konto.

**iOS — TestFlight (anbefalt for demo, ingen App Store-review nødvendig):**

1. Vi bygger signert IPA via `dotnet publish -p:ArchiveOnBuild=true`
2. Last opp til App Store Connect → TestFlight
3. Inviter testere på e-post (kan ha opp til 10.000 eksterne testere)
4. Tester installerer **TestFlight** fra App Store (gratis app, krever bare Apple ID)
5. Åpner invitasjon-link → installerer vår app inne i TestFlight
6. Appen er gyldig i 90 dager før den må re-publiseres
7. Apple gjør en lett review (typisk ~24 timer for første versjon, deretter raskt)

Dokumenteres i `/docs/install/ios.md`.

**Android — Play Store Internal Testing (anbefalt for demo):**

1. Vi bygger signert AAB via `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=aab`
2. Last opp til Play Console → Internal Testing track
3. Legg til testere ved e-post (Google-konto)
4. Tester får en opt-in-link → installerer vår app fra Play Store
5. Ingen review-tid for Internal Testing; appen er live umiddelbart
6. Senere overgang til Closed/Open Testing → Production er bare en track-promotering

Dokumenteres i `/docs/install/android.md`.

**Fallback: ren APK-sideload** (hvis Play Store-flyt har problemer):

- Bygg APK via `-p:AndroidPackageFormat=apk`
- Distribuer via Drive/email
- Bruker slår på "Install unknown apps" for kilde-appen, tap APK

For watch-apper (Fase 5) gjelder samme regler: watchOS-app følger med iOS-app gjennom TestFlight; Wear OS-app distribueres via Play Store Wear-tab eller sideload.

## Verifikasjon

- Bygger og kjører på iOS Simulator og Android Emulator
- Manuell test: tap mikrofon → si "ni en ni tre null null tre åtte åtte" → ser 919300388 → resultat → trykk Les → app leser opp på norsk
- Også test: si "Equinor" → faller til navn-søk → ser liste → drill-down
- Faller tilbake til tekst-input hvis mic ikke tilgjengelig eller tillatelse nektes
- TestFlight-flow testet ende-til-ende med minst én ekstern tester før Demo
- Play Internal Testing-flow testet tilsvarende
