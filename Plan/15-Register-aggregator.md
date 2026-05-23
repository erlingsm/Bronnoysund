# 15 — Register-aggregator (parallelle oppslag mot offentlige registre)

Tverrgående plan for å aggregere bedriftsinformasjon fra flere offentlige registre. Brukerens krav 2026-05-23: parallelle oppslag ved org.nr, lazy drill-down ved navn-søk, støtte for regnskap/eiere/styre, og person-oppslag i eierstruktur.

## Strategi

To ulike oppslagsmodi:

### A) Org.nr-søk → parallell aggregering

Når bruker har et konkret organisasjonsnummer, fyrer vi **alle aktive register-kall i parallell** (`Task.WhenAll`), så bygger vi en aggregert respons. Hver provider returnerer enten data eller en "Unavailable"-markør — UI viser delvis respons mens den venter, og oppdaterer fortløpende.

### B) Navn-søk → staged oppslag

Når bruker har et navn (eller talt input):

1. **Steg 1:** Kall Brreg Enhetsregisteret med `?navn=...` → liste over treff
2. **Steg 2:** Bruker velger ett selskap fra lista
3. **Steg 3:** Parallell aggregering kjører på det valgte orgnr (samme som A)

Ingen "kasting bort" av kall på selskaper bruker ikke har valgt.

### C) Person-drill-down (Fase 4+)

Når bruker ser et styremedlem eller eier:

1. Klikk på personen → ny oppslag mot "Roller"-API for å finne andre selskaper personen er involvert i
2. Vis liste → bruker kan velge nytt selskap → tilbake til (A) på dette selskap

Resulterer i en navigasjonsgraf bruker kan utforske.

## Aggregator-arkitektur (Ports & Adapters + Composite)

### Ports i Application

```csharp
namespace Bronnoysund.Lookup.Application.Ports;

/// <summary>Aggregert respons med data fra flere registre.</summary>
public sealed record AggregatedCompanyResponse(
    CompanyResponse Core,                          // Brreg Enhetsregisteret (kjerne)
    RolesResponse? Roles,                          // Brreg Roller (styret, signatur, daglig leder)
    AnnualReportResponse? LatestAnnualReport,      // Regnskap (Brreg eller annen kilde)
    BeneficialOwnersResponse? BeneficialOwners,    // Reelle rettighetshavere
    DebtSummaryResponse? Debt,                     // Gjeldsregisteret (om tilgang)
    BankruptcyResponse? Bankruptcy,                // Konkursregisteret
    SubUnitsResponse? SubUnits,                    // Underenheter (filialer)
    IReadOnlyList<RegistryError> Errors            // Per-provider feil
);

public interface ICompanyDataAggregator
{
    /// <summary>Hent all tilgjengelig info i parallell. Delvise svar mulig.</summary>
    Task<AggregatedCompanyResponse> AggregateAsync(OrganizationNumber org, CancellationToken ct);

    /// <summary>Hent kun kjerne (raskest, brukes for Fase 0 MVP).</summary>
    Task<CompanyLookupResult> CoreOnlyAsync(OrganizationNumber org, CancellationToken ct);
}

// Individuelle porter — én per register-type
public interface ICompanyProvider { /* Brreg Enhetsregisteret — finnes allerede */ }
public interface IRolesProvider
{
    Task<RolesResponse?> GetRolesAsync(OrganizationNumber org, CancellationToken ct);
}
public interface IAnnualReportProvider
{
    Task<AnnualReportResponse?> GetLatestAsync(OrganizationNumber org, CancellationToken ct);
}
public interface IBeneficialOwnerProvider
{
    Task<BeneficialOwnersResponse?> GetAsync(OrganizationNumber org, CancellationToken ct);
}
public interface IDebtRegisterProvider
{
    Task<DebtSummaryResponse?> GetAsync(OrganizationNumber org, CancellationToken ct);
}
public interface IBankruptcyProvider
{
    Task<BankruptcyResponse?> GetAsync(OrganizationNumber org, CancellationToken ct);
}
public interface ISubUnitsProvider
{
    Task<SubUnitsResponse?> GetSubUnitsAsync(OrganizationNumber org, CancellationToken ct);
}
public interface IPersonRolesProvider
{
    /// <summary>Finn andre selskaper en person er involvert i (drill-down).</summary>
    Task<PersonRolesResponse?> GetByPersonAsync(PersonIdentifier person, CancellationToken ct);
}
```

### Implementasjoner i Infrastructure

```text
src/Bronnoysund.Lookup.Infrastructure/
├── Brreg/
│   ├── BrregHttpClient.cs                  — typed HttpClient (Enhetsregisteret)
│   ├── BrregCompanyProvider.cs             — implements ICompanyProvider
│   ├── BrregRolesProvider.cs               — implements IRolesProvider
│   ├── BrregAnnualReportProvider.cs        — implements IAnnualReportProvider (Maskinporten — stub for nå)
│   ├── BrregBeneficialOwnerProvider.cs     — implements IBeneficialOwnerProvider (begrenset — stub for nå)
│   ├── BrregBankruptcyProvider.cs          — implements IBankruptcyProvider
│   ├── BrregSubUnitsProvider.cs            — implements ISubUnitsProvider
│   └── BrregPersonRolesProvider.cs         — implements IPersonRolesProvider
├── Gjeldsregisteret/
│   └── NotAvailableYetDebtProvider.cs      — stub (kommersiell tilgang kreves)
├── Aggregation/
│   └── ParallelCompanyDataAggregator.cs    — implements ICompanyDataAggregator
└── Caching/
    └── CachingCompanyProvider.cs            — decorator, gjelder kjerne
```

### Aggregator-implementasjon (skissert)

```csharp
internal sealed class ParallelCompanyDataAggregator(
    ICompanyProvider company,
    IRolesProvider roles,
    IAnnualReportProvider annualReport,
    IBeneficialOwnerProvider beneficialOwner,
    IDebtRegisterProvider debt,
    IBankruptcyProvider bankruptcy,
    ISubUnitsProvider subUnits,
    ILogger<ParallelCompanyDataAggregator> log) : ICompanyDataAggregator
{
    public async Task<AggregatedCompanyResponse> AggregateAsync(
        OrganizationNumber org, CancellationToken ct)
    {
        // Kjør alle 7 kall i parallell
        var coreTask        = SafeAsync(() => company.LookupAsync(org, ct), "core");
        var rolesTask       = SafeAsync(() => roles.GetRolesAsync(org, ct), "roles");
        var annualTask      = SafeAsync(() => annualReport.GetLatestAsync(org, ct), "annual");
        var beneficialTask  = SafeAsync(() => beneficialOwner.GetAsync(org, ct), "beneficial");
        var debtTask        = SafeAsync(() => debt.GetAsync(org, ct), "debt");
        var bankruptcyTask  = SafeAsync(() => bankruptcy.GetAsync(org, ct), "bankruptcy");
        var subUnitsTask    = SafeAsync(() => subUnits.GetSubUnitsAsync(org, ct), "subUnits");

        await Task.WhenAll(coreTask, rolesTask, annualTask, beneficialTask, debtTask, bankruptcyTask, subUnitsTask);

        var errors = new List<RegistryError>();
        // ... samle feil + bygg respons
        return new AggregatedCompanyResponse(/* ... */);
    }

    private async Task<Result<T>> SafeAsync<T>(Func<Task<T>> op, string name)
    {
        try { return Result.Ok(await op()); }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Provider {Name} failed", name);
            return Result.Failed<T>(new RegistryError(name, ex.Message));
        }
    }
}
```

**Hver provider:**

- Har egen `IHttpClient` + Polly-pipeline (resilience isolert per register)
- Cacher med egen TTL via HybridCache (regnskap kan TTL-eldre seg dagvis, mens roller endrer seg sjeldnere)
- Egen DI-registrering i `AddBronnoysundInfrastructure`

## Hvilke registre — status per 2026-05-23

| Register | Provider-port | Brreg API-status | Implementasjon (per fase) |
| --- | --- | --- | --- |
| Enhetsregisteret (kjerne) | `ICompanyProvider` | **Åpent** (NLOD 2.0) | Fase 0 — full implementasjon |
| Roller (styret, signatur, daglig leder, revisor, prokura) | `IRolesProvider` | **Åpent** (sensitive felt krever Maskinporten) | Fase 4 — åpne felt først |
| Underenheter (filialer) | `ISubUnitsProvider` | **Åpent** | Fase 4 |
| Konkursregisteret | `IBankruptcyProvider` | **Åpent** | Fase 4 |
| Årsregnskap | `IAnnualReportProvider` | **Krever Maskinporten** | Fase 4 — stub; full når Maskinporten-tilgang er på plass |
| Reelle rettighetshavere | `IBeneficialOwnerProvider` | **Begrenset tilgang** (AML-aktører, myndigheter, media) | Fase 4 — stub; sjekk om vi kvalifiserer |
| Gjeldsregisteret | `IDebtRegisterProvider` | **Ikke åpent API** (kommersiell tilgang via avtale) | Stub permanent; flagg som "krever avtale" |
| Person-drill-down (andre selskaper en person er i) | `IPersonRolesProvider` | **Åpent via Roller-API** (omvendt søk) | Fase 4 |

Detaljer i [[reference-brreg-apis]] memory.

## Trinnvis innføring per fase

### Fase 0 (MVP, denne sesjonen)

- **Implementer:** `ICompanyProvider` (kjerne) — komplett mot Brreg Enhetsregisteret
- **Etabler porter:** `ICompanyDataAggregator` + alle individuelle provider-porter (kun interfaces, ingen impl ennå)
- **Stub-implementasjoner:** `NotAvailableYet*Provider` for alle ikke-implementerte porter — kaster `RegistryNotAvailableException` med tydelig melding
- **DI:** `AddBronnoysundCore()` registrerer kjerne + alle stubs. Fremtidige `AddBronnoysundRolesFromBrreg()`, `AddBronnoysundAnnualReports()` etc. legger til faktiske provider-implementasjoner.
- **WebApi:** kun `GET /companies/{orgnr}` (kjerne) — matcher original MVP

### Fase 4 (Application-utvidelser)

- Implementer `BrregRolesProvider`, `BrregSubUnitsProvider`, `BrregBankruptcyProvider`, `BrregPersonRolesProvider` (alle Brreg-baserte, åpne)
- Implementer `ParallelCompanyDataAggregator` — full parallell-aggregering
- Implementer **navn-søk staged flow** i UI (Liste → drill-down)
- Legg til `GET /companies/{orgnr}/aggregated` endepunkt i WebApi
- Legg til `GET /persons/{personId}/companies` for drill-down

### Fase 4+ (når tilganger er på plass)

- `BrregAnnualReportProvider` — krever Maskinporten-konto
- `BrregBeneficialOwnerProvider` — krever AML-/myndighets-tilgang
- `KredittsjekkDebtRegisterProvider` — krever kommersiell avtale med gjeldsregisteret

## Eksisterende kode/inspirasjon (GitHub-research)

Sjekket 2026-05-23. Status:

- **Frank.Libraries.Brreg** (MIT, NuGet) — dekker kun Enhetsregisteret. Ingen aggregator. Inspirasjon for klient-struktur.
- **SindreMA/Blazor-Brønnøysundregistrene** — kun Enhetsregisteret. Ingen aggregator.
- **brreg-app (oysandvik94)** — .NET 5 + React, kun Enhetsregisteret.

**Konklusjon:** Det finnes ingen ferdig multi-register-aggregator for norske offentlige registre i åpen kildekode. Vi bygger arkitekturen selv. Detaljer i [[reference-brreg-inspirations]] og `09-Tredjepartskode-og-kreditering.md`.

For Roller-API og person-drill-down kan vi henvise til Brreg sine offisielle eksempler: <https://brreg.github.io/docs/apidokumentasjon/>

## UI-konsekvenser

### Fase 0 (denne sesjonen)

- Ett input-felt + "Søk"-knapp → ett resultat-kort med 4 felter

### Fase 2+ (Blazor Web + Desktop)

- Etter org.nr-oppslag: vis flere seksjoner som lastes parallelt (skeleton/spinner per seksjon mens den henter):
  - Selskapsinformasjon (kjerne — kommer først)
  - Styret + daglig leder (Roller)
  - Siste årsregnskap
  - Eiere / reelle rettighetshavere (om tilgang)
  - Underenheter
  - Konkurs-status (om aktuell)
- Navn-søk: liste-resultat → klikk på selskap → aggregert visning

### Fase 4 (drill-down på person)

- Klikk på person-navn (i styret eller blant eiere) → ny visning av "Personens involveringer" (liste over andre selskaper) → klikk på selskap → tilbake til aggregert visning

### Watch (Fase 5)

- Klokken viser kun kjerne-respons + evt. status om at "Mer finnes på telefonen". For å unngå tung scroll på liten skjerm.

## Cache-strategi per register

| Register | TTL | Begrunnelse |
| --- | --- | --- |
| Enhetsregisteret (kjerne) | 24 t | Endrer seg sjelden |
| Roller | 24 t | Endrer seg sjelden |
| Underenheter | 24 t | Endrer seg sjelden |
| Konkurs | 1 t | Mer tidssensitivt — bedrifter kan plutselig bli erklært konkurs |
| Årsregnskap | 7 dager | Endres bare én gang i året |
| Reelle rettighetshavere | 24 t | Endrer seg sjelden |
| Gjeldsregisteret | 1 t | Når aktuell — krever ferskere data |
| Person-roller | 24 t | Mest stabil |

Alle TTL-er konfigurerbare via `AppSettings`-tabellen.

## Verifikasjon (Fase 4)

- `dotnet test` med integrasjons-tester (WireMock.Net) som verifiserer at `ParallelCompanyDataAggregator` håndterer:
  - Alle providers OK → komplett respons
  - Én provider feiler → respons inneholder feilmarkør for den
  - Provider timer ut → respons inneholder timeout-feil
  - Cancellation → alle pending kall avbrytes
- Manuell test mot Brreg på Equinor (919300388) → ser styret, underenheter, evt. konkurs-status
- Manuell test på drill-down: klikk Eldar Sætre (styremedlem) → ser andre selskaper han er styremedlem i

## Open questions

- Skal vi støtte **batch-oppslag** (mange orgnr samtidig)? Brukeren har ikke nevnt — antar nei for nå.
- Skal **person-drill-down** kunne navigere flere ledd dyp (selskap → person → selskap → person → ...)? Antar ja, ubegrenset, men med klar navigasjons-historikk.
- Skal vi tilby **eksport** av aggregert respons (PDF/JSON)? Vurderes i Fase 4.
