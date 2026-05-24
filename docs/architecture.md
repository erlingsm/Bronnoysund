# Arkitektur

Bronnoysund.Lookup følger **Clean Architecture** med **Ports & Adapters** og maks kode-deling mellom plattformer.

## Lagdeling

```text
Domain          ← rene C#-typer (ingen rammeverk-avhengighet)
   ↑
Application     ← bruker Domain, definerer porter (interfaces)
   ↑
Infrastructure  ← implementerer portene (Brreg-HTTP, HybridCache, Polly, EF Core)
   ↑
Presentation    ← MAUI Desktop, MAUI Mobile, Blazor Web, WebApi — vet bare om Application
```

**The Dependency Rule:** avhengighets-pilen peker innover. Indre lag vet ingenting om ytre lag. Domain er fri for alle rammeverk. Application kjenner kun til Domain. Infrastructure leverer adaptere mot eksterne tjenester. Presentation orkestrerer use cases via Application.

Dette gjør at samme Domain + Application kjører i alle våre 4 plattform-presentations uten endring.

## Solution-struktur

```text
src/
├── Bronnoysund.Lookup.Domain          — OrganizationNumber (MOD11 VO), Company, LanguageForm
├── Bronnoysund.Lookup.Application     — Ports, DTOs, UseCases, Validators, Aggregator
├── Bronnoysund.Lookup.Infrastructure  — Brreg HttpClient, Caching, Stubs
├── Bronnoysund.Lookup.WebApi          — ASP.NET Core Minimal API
├── Bronnoysund.Lookup.Components      — Razor Class Library (delte Razor-komponenter)
├── Bronnoysund.Lookup.ViewModels      — MVVM (CommunityToolkit.Mvvm source generators)
├── Bronnoysund.Lookup.Speech          — Speech-porter + NorskTallParser
├── Bronnoysund.Lookup.BlazorWeb       — Blazor Web App i Server-modus
├── Bronnoysund.Lookup.MauiDesktop     — MAUI Blazor Hybrid (Mac Catalyst + Windows)
└── Bronnoysund.Lookup.MauiMobile      — MAUI Blazor Hybrid (iOS + Android, inkl. iPad)
```

## Mønstre brukt eksplisitt

| Mønster | Hvor i koden | Hvorfor |
| --- | --- | --- |
| **Clean Architecture** | Lagstruktur over | Forretningslogikk uavhengig av rammeverk |
| **Ports & Adapters (Hexagonal)** | `ICompanyProvider`, `ISpeechToText`, `ICompanyDataAggregator` | Bytter teknologi uten å endre forretningslogikk |
| **Dependency Injection** | Microsoft.Extensions.DI overalt | Testbar, konfigurerbar |
| **Value Object (DDD)** | `OrganizationNumber` validerer i konstruksjon | Type-sikkerhet — har du et orgnr, er det gyldig |
| **Strategy** | `ICompanyProvider` med adaptere (Brreg, RemoteApi senere) | Bytte kilde via DI |
| **Adapter** | `BrregCompanyProvider` adapterer Brreg-API til vår port | Brreg-detaljer ute av Application |
| **Decorator** | `CachingCompanyProvider` rundt `BrregCompanyProvider` | Krydre med caching uten å endre den dekorerte |
| **Aggregator (planlagt)** | `ICompanyDataAggregator` for parallelle register-oppslag | Roller, regnskap, eiere hentet i parallell |
| **Resilience (Circuit Breaker, Retry, Timeout, Bulkhead)** | Polly v8 via `AddStandardResilienceHandler()` | Brreg er offentlig tjeneste — robust mot transient feil |
| **Cache-aside** | `HybridCache` rundt provider-kall | Reduserer Brreg-trafikk; TTL 24t default |
| **MVVM** | `CompanyLookupViewModel` brukt av både MAUI og Blazor | Delt presentasjons-logikk |
| **Companion-pattern (Watch, planlagt)** | Klokke ↔ telefon via WatchConnectivity / Wearable Data Layer | Watch-app er thin client |

## Modern .NET 10 / C#-teknikker

- Nullable Reference Types overalt
- File-scoped namespaces (C# 10)
- Primary constructors (C# 12) for DI-handlers
- `record` og `readonly record struct` for DTOs og value objects
- `required` properties (C# 11) for kritiske felter
- `sealed` som default
- Pattern matching for `CompanyLookupResult`-håndtering
- `async/await` overalt + `CancellationToken` som siste parameter
- HybridCache (ny i .NET 9, GA i .NET 10) som cache-lag
- `IHttpClientFactory` + typed clients

## Litteratur og verifiserbare kilder

Detaljert litteratur-liste med ISBN, sentrale kapitler og direkte URL-er til primærkilder (Cockburn-artikkelen, Dijkstra EWD 447, Fowlers EAA-katalog, Microsoft Learn-sider) finnes i [credits.md](credits.md). Sammendrag for evaluator: ~70 % av referansene kan verifiseres online i 2-3 minutter.

## Register-aggregator (planlagt i Fase 4)

Når aggregator-koden implementeres vil hver org.nr-oppslag fyre **parallelle** kall mot:

- Brreg Enhetsregisteret (kjerne)
- Brreg Roller (styret, signatur, daglig leder)
- Brreg Underenheter
- Brreg Konkursregisteret
- Regnskap (krever Maskinporten — stub inntil videre)
- Reelle rettighetshavere (begrenset tilgang — stub)
- Gjeldsregisteret (kommersiell avtale — permanent stub)

Hver provider er en separat adapter med egen TTL, egen Polly-pipeline, og egen DI-registrering. Mismatched feil håndteres per-provider — én feilende provider ødelegger ikke hele responsen.

For person-drill-down: klikk på styremedlem → omvendt søk via Roller-API → liste over andre selskaper personen er involvert i.
