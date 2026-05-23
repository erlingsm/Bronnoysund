# 07 — Fase 6 (opsjonell): Sentral backend

**Forutsetning:** Fase 0-5 ferdig. Aktiveres når trafikkmønstre, sentral cache, eller sentral logging blir verdt kostnaden.

## Mål

Promotere `Brreg.WebApi` fra "referanse-frontend for tester" til ekte produksjon, og gi alle klienter et valg mellom **direkte Brreg** og **via vår API**.

## Endringer

### Nye prosjekter / utvidelser

- `src/Brreg.Infrastructure.RemoteApi/` — ny adapter som implementerer `ICompanyProvider` ved å kalle vår egen Web API i stedet for Brreg direkte
- `src/Brreg.WebApi/` — utvides med autentisering, rate-limiting, observability (OpenTelemetry)

### Klient-side bytte

Klientene velger via konfigurasjon:

```jsonc
// appsettings.json i MAUI/Blazor
{
  "Brreg": {
    "Mode": "Direct"   // eller "RemoteApi"
  },
  "RemoteApi": {
    "BaseUrl": "https://api.brreg-lookup.example/"
  }
}
```

DI-extension velger:

```csharp
if (mode == "RemoteApi")
    services.AddSingleton<ICompanyProvider, RemoteApiCompanyProvider>();
else
    services.AddSingleton<ICompanyProvider, BrregCompanyProvider>();
// CachingCompanyProvider-decorator wrapper begge
```

## Hva får vi?

- Sentral cache (Redis L2 i HybridCache)
- Sentralisert logging og observability
- Skjuler API-kontrakt mot Brreg fra klienter (vi kan bytte underliggende kilde)
- Bedre rate-limit-håndtering mot Brreg
- Mulighet for å lage premium-features (gjeldsregister, regnskap) som krever auth

## Hva mister vi?

- Apper trenger nettverk til vår server (i tillegg til Brreg)
- Kostnad for hosting
- Vi blir et SPoF

## Verifikasjon

- Vri en konfig-bit → app går fra Direct til RemoteApi uten kode-endring
- Existing tester fortsatt grønne
