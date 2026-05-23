# 01 — Fase 0: MVP

**Mål:** Tilfredsstille hele den opprinnelige oppgaven med en ryddig .NET 10-løsning som senere fases bygger oppå. Demonstrerer Clean Architecture, Dependency Injection, Separation of Concerns og løs kobling fra første commit.

## Leveranseliste fra opprinnelig oppgave

| Krav | Hvor i løsningen |
| --- | --- |
| Input + validering (9 siffer, kun tall, starter på 8/9, MOD11) | `Bronnoysund.Lookup.Domain.ValueObjects.OrganizationNumber` + `Bronnoysund.Lookup.Application.Validators.OrganizationNumberValidator` |
| Output på engelsk (`organizationNumber`, `organizationName`, `companyType`, `languageForm`) | `Bronnoysund.Lookup.Application.Dtos.CompanyResponse` (record) |
| Feilhåndtering (404, timeout, uventet) | Polly v8 i Infrastructure + `CompanyLookupResult`-diskriminator i Application |
| Kodeorganisering | Clean Architecture: Domain ← Application ← Infrastructure ← Presentation |
| Logging | `Microsoft.Extensions.Logging` + Serilog (console + rolling file) |
| Caching m/ TTL | `Microsoft.Extensions.Caching.Hybrid` (24t default) som decorator-pattern |
| Unit tests | `Bronnoysund.Lookup.Domain.Tests` + `Bronnoysund.Lookup.Application.Tests` |
| Integration test | `Bronnoysund.Lookup.Infrastructure.Tests` med WireMock.Net |
| README | `README.md` på repo-root: kjøre lokalt, kjøre tester, viktige valg, kreditering |

## Solution-struktur (Fase 0)

```text
/Kode/
├── Bronnoysund.Lookup.sln
├── Directory.Build.props        — felles MSBuild-egenskaper for alle prosjekter
├── src/
│   ├── Bronnoysund.Lookup.Domain/
│   ├── Bronnoysund.Lookup.Application/
│   ├── Bronnoysund.Lookup.Infrastructure/
│   └── Bronnoysund.Lookup.WebApi/
└── tests/
    ├── Bronnoysund.Lookup.Domain.Tests/
    ├── Bronnoysund.Lookup.Application.Tests/
    └── Bronnoysund.Lookup.Infrastructure.Tests/
```

`Directory.Build.props` (rot) standardiserer for alle prosjekter:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

## Konkrete bygge-steg

1. `dotnet new sln -n Bronnoysund.Lookup` i `/Kode/`
2. `dotnet new classlib -f net10.0 -o src/Bronnoysund.Lookup.Domain` (og legg til solution)
3. Tilsvarende for `Bronnoysund.Lookup.Application` og `Bronnoysund.Lookup.Infrastructure`
4. `dotnet new webapi -minimal -f net10.0 -o src/Bronnoysund.Lookup.WebApi`
5. `dotnet new xunit -f net10.0 -o tests/Bronnoysund.Lookup.Domain.Tests` (og videre for de to andre test-prosjektene)
6. Sett opp prosjektreferanser (`dotnet add reference`):

- Application → Domain
- Infrastructure → Application
- WebApi → Application, Infrastructure
- Tester → tilsvarende kildelag + nødvendige avhengigheter

1. Legg til NuGet-pakker (`dotnet add package`):

- Infrastructure: `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.Caching.Hybrid`, `Serilog.Extensions.Logging`
- Application: `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`
- WebApi: `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
- Tester: `FluentAssertions`, `NSubstitute`, `WireMock.Net`, `Microsoft.AspNetCore.Mvc.Testing` (for WebApi-integration ved behov)

1. Lag `Directory.Build.props` med felles settings (NRT, implicit usings, treat-warnings-as-errors)
2. Implementer i rekkefølge: Domain → Application → Infrastructure → WebApi → tester
3. Skriv `README.md`, `LICENSE` (AGPL-3.0), `COMMERCIAL-LICENSE.md` (Røa Systemutvikling AS), `.gitignore` (dotnet-template), `.editorconfig`
4. `git init` + første commit

## Tverrgående kvalitetskrav (gjelder all kode i Fase 0)

- **SPDX-header øverst i hver kildefil:** `// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial`
- **Nullable Reference Types** aktivt i alle prosjekter (settes i `Directory.Build.props`)
- **`sealed` som default** på alle klasser med mindre arv er et bevisst designvalg
- **`internal` som default** for klasser som ikke trenger å eksponeres på tvers av prosjekter
- **`CancellationToken`** som siste parameter på alle async-metoder
- **`async/await` overalt** for IO — ingen `.Result` eller `.Wait()`
- **DI for alt** — ingen `new` på avhengigheter inne i forretningskoden, ingen statiske singletoner
- **`IOptions<T>`** for all konfigurasjon (Brreg-base-URL, TTL, timeout, user-agent)
- **Primary constructors (C# 12)** der det passer for kortere DI-syntax
- **`record` / `readonly record struct`** for DTOs og Value Objects (innebygd verdi-likhet)
- **Pattern matching** for håndtering av `CompanyLookupResult`-varianter i WebApi-laget

Se `08-Patterns-og-arkitekturbegrunnelser.md` for utdypende begrunnelse av disse valgene.

## Kjerne-klasser

### Domain (`Bronnoysund.Lookup.Domain`)

- `ValueObjects/OrganizationNumber` — `readonly record struct`. Privat ctor, statisk `TryCreate(string raw, out OrganizationNumber value, out string? error)` og `Create(string raw)` som kaster ved ugyldig. Validering: 9 siffer, kun tall, starter på 8/9, MOD11 (vekter 3,2,7,6,5,4,3,2 fra venstre). Normalisert form (kun siffer, ingen mellomrom).
- `Entities/Company` — `sealed record`. `OrganizationNumber`, `Name`, `OrganizationFormCode`, `LanguageForm` (enum: `Bokmål`, `Nynorsk`, `Unknown`).
- **Avhengigheter:** ingen. Bare .NET BCL.

### Application (`Bronnoysund.Lookup.Application`)

- `Ports/ICompanyProvider` — `Task<CompanyLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct)`
- `Ports/ICompanyDataAggregator` + `Ports/IRolesProvider`, `IAnnualReportProvider`, `IBeneficialOwnerProvider`, `IDebtRegisterProvider`, `IBankruptcyProvider`, `ISubUnitsProvider`, `IPersonRolesProvider` — porter for fremtidig register-aggregering (`15-Register-aggregator.md`). I Fase 0 etableres kun interfaces + `CoreOnlyAggregator` som delegerer til `ICompanyProvider`. Stub-impl `NotAvailableYet*Provider` i Infrastructure for de andre.
- `UseCases/LookupCompany/LookupCompanyQuery` — `record(string OrgNumberInput)`
- `UseCases/LookupCompany/LookupCompanyHandler` — primary constructor injiserer `ICompanyProvider`. Validerer input → `OrganizationNumber.TryCreate` → kaller port → mapper `Company` → `CompanyResponse`
- `Dtos/CompanyResponse` — `record` med 4 engelsk-felter
- `CompanyLookupResult` — discriminated union (sealed abstract base + subtypes `Found`, `NotFound`, `Unavailable`, `InvalidInput`) for type-safe håndtering uten exceptions
- `Validators/OrganizationNumberValidator` — FluentValidation `AbstractValidator<string>`
- `ServiceCollectionExtensions.AddBrregApplication(this IServiceCollection)` — registrerer handlers og validators
- **Avhengigheter:** Domain. Ingen Infrastructure-referanse.

### Infrastructure (`Bronnoysund.Lookup.Infrastructure`)

- `Brreg/BrregHttpClient` — typed HttpClient. `GetAsync(OrganizationNumber org, CancellationToken ct)` → `BrregEnhetDto?`. Returnerer `null` ved 404, kaster `BrregUnavailableException` ved timeout/5xx etter Polly-pipeline.
- `Brreg/BrregEnhetDto` — JSON-kontrakt for norske Brreg-felter (`organisasjonsnummer`, `navn`, `organisasjonsform.kode`, `maalform`). `record` med `System.Text.Json`-attributter.
- `Brreg/BrregCompanyProvider` — `internal sealed class`. Implementerer `ICompanyProvider`. Primary constructor injiserer `BrregHttpClient`. Mapper DTO til `Company`.
- `Caching/CachingCompanyProvider` — `internal sealed class`. **Decorator** rundt `ICompanyProvider`. Bruker `HybridCache`. Cache-key = `org:{orgnr}`. TTL fra `IOptions<BrregOptions>`.
- `BrregOptions` — `sealed class` med `required` properties: `BaseUrl`, `Timeout`, `CacheTtl`, `UserAgent`. Validert ved oppstart.
- `ServiceCollectionExtensions.AddBrregInfrastructure(this IServiceCollection, IConfiguration)` — registrerer typed HttpClient med `AddStandardResilienceHandler()`, HybridCache, options, og decorator-kjeden.
- **Avhengigheter:** Application. Detaljene er HttpClient, Polly, HybridCache, Serilog.

### WebApi (`Bronnoysund.Lookup.WebApi`)

- `Program.cs` — top-level statements. Serilog bootstrap → `AddBrregApplication()` → `AddBrregInfrastructure(builder.Configuration)` → Minimal API-endepunkt `GET /companies/{orgnr}` som:
    1. Bygger `LookupCompanyQuery`
    2. Kaller `LookupCompanyHandler.HandleAsync`
    3. Pattern matcher `CompanyLookupResult` → mapper til `Results.Ok` / `Results.BadRequest` / `Results.NotFound` / `Results.StatusCode(503)`
- `appsettings.json` — binder mot `BrregOptions`-seksjon
- `appsettings.Development.json` — overstyringer for utvikling (mer verbose logging)

## Tester

### Domain.Tests

- `OrganizationNumberTests`:
  - Gyldige eksempler (919300388 og noen til)
  - Ugyldige: tom, for kort, for lang, ikke-tall, starter ikke på 8/9, feil kontrollsiffer, kontrollsiffer = 10 (rest=1)
  - Equality på normalisert form
- Mål: ~100% dekning av VO

### Application.Tests

- `LookupCompanyHandlerTests`:
  - Gyldig orgnr + provider returnerer Company → `CompanyResponse` mappet riktig
  - Gyldig orgnr + provider returnerer NotFound → result.IsNotFound
  - Ugyldig orgnr → result.IsInvalidInput, ikke kaller provider
  - Provider kaster Unavailable → result.IsUnavailable
  - Bruk NSubstitute for `ICompanyProvider`
- `LookupCompanyHandlerDIWiringTest` — verifiserer at `AddBrregApplication()` registrerer handler korrekt i en test-container

### Infrastructure.Tests

- `BrregHttpClientIntegrationTests` med WireMock.Net:
  - 200 + JSON → returnerer DTO
  - 404 → returnerer null
  - 408/503 → kaster `BrregUnavailableException`
  - Timeout → kaster `BrregUnavailableException`
- `CachingCompanyProviderTests`:
  - Andre kall samme orgnr → indre provider kalles kun én gang (verifiserer decorator-pattern)
- `BrregInfrastructureDIWiringTest` — verifiserer at `AddBrregInfrastructure()` bygger en gyldig service-provider og at decorator-kjeden er riktig montert

## README-disposisjon

1. Hva er dette?
2. Kjøre lokalt (`dotnet run --project src/Bronnoysund.Lookup.WebApi`)
3. Kjøre tester (`dotnet test`)
4. Eksempel-curl
5. Viktige valg og avveininger (Clean Architecture, Ports & Adapters, HybridCache, Polly, FluentValidation, Serilog)
6. Inspirasjon & kildehenvisninger (Frank.Libraries.Brreg, organisationsnummer/csharp, SindreMA)
7. Lisens

## Definition of Done for Fase 0

- [ ] `dotnet build` grønt
- [ ] `dotnet test` — alle tester grønne
- [ ] `dotnet run --project src/Brreg.WebApi` starter
- [ ] `curl http://localhost:<port>/companies/919300388` returnerer 200 + engelsk JSON
- [ ] `curl .../companies/12345` returnerer 400 m/ tydelig feilmelding
- [ ] `curl .../companies/999999999` (gyldig MOD11 men ikke i registeret) returnerer 404 m/ tydelig melding
- [ ] Logg viser cache-hit ved gjentatt kall
- [ ] README finnes og er kjørbar av fersk leser
- [ ] `git log` viser ryddige commits
