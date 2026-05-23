# 03 — Fase 2: Blazor Web (portable Server-modus)

**Forutsetning:** Fase 1 ferdig (RCL eksisterer).

## Mål

Web-versjon som er funksjonelt identisk med MAUI Desktop, kan kjøres:

- **A1)** Lokalt på Windows (dobbeltklikk `.exe`)
- **A2)** Lokalt på Mac (kjør binær)
- **A3)** På dedikert webserver (med .NET 10 Runtime)

Gjenbruker `Brreg.Components` 1:1 fra Fase 1.

## Valg: Blazor Web App i Server-modus (ikke WASM)

Begrunnelse i `10-Distribusjon-og-portability.md`. Kortversjon: Server-modus gir én `.exe` som starter Kestrel og åpner nettleser — én enkel portable artefakt for alle tre kjøre-modus.

WASM-variant kan legges til senere som ekstra host hvis vi vil ha 100% browser-only.

## Nye prosjekter

- `src/Brreg.BlazorWeb/` — `net10.0` Blazor Web App (Server-modus, InteractiveServer rendering)
  - Egen liten utility `OpenBrowser.cs` som kaller `Process.Start` mot `http://localhost:<port>` ved startup

## DI

Samme `AddBrregApplication()` + `AddBrregInfrastructure()` som MAUI Desktop.

- HybridCache: L1 in-memory (per server-prosess)
- Serilog: Console + rolling file (passer både local og server)

## Distribusjon (se `10-Distribusjon-og-portability.md` for detaljer)

Bygges som framework-dependent per RID:

```bash
dotnet publish src/Brreg.BlazorWeb -c Release -r win-x64   --self-contained false -o dist/web/win-x64
dotnet publish src/Brreg.BlazorWeb -c Release -r osx-arm64 --self-contained false -o dist/web/osx-arm64
dotnet publish src/Brreg.BlazorWeb -c Release -r osx-x64   --self-contained false -o dist/web/osx-x64
dotnet publish src/Brreg.BlazorWeb -c Release -r linux-x64 --self-contained false -o dist/web/linux-x64
```

Hver mappe ZIP-pakkes. Krever .NET 10 Runtime installert. Self-contained varianter (`--self-contained true -p:PublishSingleFile=true`) tilbys som ekstra ZIP for null avhengighet.

## READMEer

- `/docs/install/web-standalone.md` — last ned, pakk ut, dobbeltklikk (PC/Mac)
- `/docs/install/web-server.md` — sett opp som systemd-service, eksempel nginx reverse-proxy

## Verifikasjon

- Lokal: `dotnet run --project src/Brreg.BlazorWeb` — nettleser åpnes automatisk
- Manuell test: 919300388, 12345, 999999999
- ZIP testes på en fersk PC og Mac uten Visual Studio (kun .NET 10 Runtime)
- Server-deploy testes på en Linux-VM med kun .NET 10 Runtime installert
