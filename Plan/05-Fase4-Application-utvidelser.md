# 05 — Fase 4: Application-utvidelser (navn-søk + drill-down + stub-ports)

**Forutsetning:** Fase 0-3 ferdig.

## Mål

Utvid Application-laget med navn-søk og drill-down, og legg inn arkitektur-grunnlag for fremtidige registre (gjeld, regnskap, eierskap) som Strategy-implementasjoner.

## Nye use cases

- `SearchCompaniesByNameQuery(name, page, size)` → `SearchResult { Items, Page, TotalCount }`
- `LookupCompanyDetailsByOrgNumber` — utvidet variant av Fase 0-handler som henter mer (registreringsdatoer, adresser, MVA-status, ansatte). Avhenger av Brreg-felter bestemt i diskusjon før Fase 4.

## Brreg-utvidelser

- `GET /enheter?navn=...&page=0&size=20` — søk
- `GET /enheter/{orgnr}/roller` — roller (ny port `IRoleProvider`)

## Stub-ports for fremtidige registre

```csharp
namespace Brreg.Application.Ports;

public interface IDebtRegisterProvider
{
    Task<DebtSummary> GetDebtAsync(OrganizationNumber org, CancellationToken ct);
}
public interface IAccountingProvider
{
    Task<AnnualReport?> GetLatestAsync(OrganizationNumber org, CancellationToken ct);
}
public interface IBeneficialOwnerProvider
{
    Task<IReadOnlyList<BeneficialOwner>> GetAsync(OrganizationNumber org, CancellationToken ct);
}
```

Default-implementasjoner i Infrastructure:

- `NotAvailableYetDebtRegisterProvider` — kaster `RegisterNotAvailableException`
- Tilsvarende for de andre

Når et register åpnes/avtaler signeres, byttes implementasjonen ut i DI uten å røre Application/UI.

## UI

- Søk-felt i toppen — søker på navn hvis input ikke matcher orgnr-format
- Resultat-liste (`SearchResultsList.razor`) → klikk for drill-down
- `CompanyDetailsPage.razor` viser fullt utvalg + seksjoner for "Gjeld" / "Regnskap" / "Eiere" som viser "Ikke tilgjengelig — krever avtale med X"

## Tester

- `SearchCompaniesByNameHandlerTests`
- Stub-providers verifiserer at de kaster riktig eksepsjon
