# BrregWatch — watchOS companion app

Thin watchOS client for the Bronnoysund lookup. Voice-driven org-number / name
search; result rendered on the watch and read aloud. Backend is the paired
iPhone running `Bronnoysund.MauiMobile` via WatchConnectivity.

## Layout

```
BrregWatch Watch App/
  BrregWatchApp.swift     SwiftUI App entry, owns Companion.
  ContentView.swift       Voice button, transcript, result, "Les opp".
  CompanyDataRow.swift    Label / divider / value row reused per field.
  SpeechRecognizer.swift  SFSpeechRecognizer wrapper (nb-NO).
  Companion.swift         WCSession wrapper, sends LookupRequest, returns LookupResponse.
  Protocol.swift          Codable DTOs matching Kode/docs/watch-protocol.md.
  Info.plist              Mic + Speech usage strings, WKApplication, companion bundle id.
project.yml               xcodegen spec — sources of truth for project structure.
```

The `.xcodeproj` is intentionally NOT committed. Generate it locally with
xcodegen.

## First build

```bash
brew install xcodegen
cd Kode/Watch/iOS/BrregWatch
xcodegen generate
open BrregWatch.xcodeproj
```

Pick the **BrregWatch Watch App** scheme + a watchOS simulator (e.g. Apple
Watch Series 10 (46mm) watchOS 26.1) and run.

## Pairing for first end-to-end test

1. Boot the iPhone simulator that hosts `Bronnoysund.MauiMobile`.
2. In Xcode Devices & Simulators, pair the watchOS simulator with the iPhone.
3. Run `Bronnoysund.MauiMobile` on the iPhone first so `WCSession` activates
   on the host. The watch scheme then reaches a live phone-host.

## Wire protocol

Single source of truth: [`Kode/docs/watch-protocol.md`](../../../docs/watch-protocol.md).
`Protocol.swift` mirrors the JSON shape — do not diverge.

## Distribution

Sideload via Xcode for the simulator is free. App Store distribution requires
an Apple Developer Program membership ($99/year) — out of scope for the v1
skeleton.

## Status (2026-05-29)

Skeleton in place. Phone-side `WCSessionDelegate` extension still pending —
the watch UI will surface "Telefonen er ikke tilgjengelig" until that lands.
