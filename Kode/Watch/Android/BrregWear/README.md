# BrregWear — Wear OS companion app

Thin Wear OS client for the Bronnoysund lookup. Voice-driven org-number / name
search; result rendered on the watch and read aloud via Android TextToSpeech.
Backend is the paired Android phone running `Bronnoysund.MauiMobile` via
Wearable Data Layer.

## Layout

```
app/
  build.gradle.kts                            App module.
  src/main/
    AndroidManifest.xml                       Wear feature, mic permission, MainActivity.
    java/com/bronnoysund/wear/
      MainActivity.kt                         Compose host, owns Companion + SpeechCapture + TTS.
      LookupScreen.kt                         ScalingLazyColumn UI.
      Companion.kt                            MessageClient.sendMessage wrapper.
      SpeechCapture.kt                        Android SpeechRecognizer wrapper.
      Protocol.kt                             kotlinx.serialization DTOs.
      ui/CompanyDataRow.kt                    Label / divider / value row.
    res/values/strings.xml                    nb-NO labels.
build.gradle.kts                              Project plugins.
settings.gradle.kts                           Single :app module.
```

The Android Studio `.idea/`, `build/`, and Gradle wrapper are intentionally NOT
committed — install Android Studio, open the folder, and let it generate them.

## First build

1. Install Android Studio (Hedgehog or newer recommended). Wear OS SDK 30+
   needs to be installed via SDK Manager.
2. From Android Studio, **Open** `Kode/Watch/Android/BrregWear/` (root with
   `settings.gradle.kts`). Let Gradle sync.
3. Pick a Wear OS emulator (Wear OS 4 / API 33+).
4. Run `app`.

## Pairing for first end-to-end test

1. In Android Studio Device Manager, pair the Wear OS emulator with an Android
   phone emulator that hosts `Bronnoysund.MauiMobile`.
2. Run the phone app first so its `WearableListenerService` is registered.
3. Run BrregWear on the watch emulator; press **Snakk**, say the org number.

## Wire protocol

Single source of truth: [`Kode/docs/watch-protocol.md`](../../../docs/watch-protocol.md).
`Protocol.kt` mirrors the JSON shape — do not diverge.

## Status (2026-05-29)

Skeleton in place but **not yet built** — Android Studio is not installed on
this machine. Verify Gradle sync once it is.
