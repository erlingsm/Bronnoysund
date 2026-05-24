# Kreditering og kilder

## Inspirasjon — krediteres, men ikke kopiert kode

Vi har bygget alt selv for å beholde full kontroll over lisens, vedlikehold og avhengigheter. Følgende prosjekter ga inspirasjon for tilnærming:

| Prosjekt | Lisens | Hva vi tok med oss |
| --- | --- | --- |
| [Frank.Libraries.Brreg](https://www.nuget.org/packages/Frank.Libraries.Brreg/) | MIT | Tilnærming til typed HttpClient og DTO-mapping |
| [organisationsnummer/csharp](https://github.com/organisationsnummer/csharp) | MIT | Form på `IsValid`-API for orgnr-validator |
| [storbukas/norsk-validator](https://github.com/storbukas/norsk-validator) | MIT | Referanse for MOD11-vektene |
| [SindreMA/Blazor-Brønnøysundregistrene](https://github.com/SindreMA/Blazor-Br-nn-ysundregistrene) | (ikke oppgitt) | Bekreftelse på Blazor som UI-valg |
| [oysandvik94/brreg-app](https://github.com/oysandvik94/brreg-app) | Apache-2.0 | Ingen direkte — annen stack (React) |

## NuGet-pakker vi bruker (cross-cutting concerns)

| Pakke | Lisens | Til hva |
| --- | --- | --- |
| Microsoft.Extensions.Http.Resilience | MIT | Polly v8 resilience pipeline |
| Microsoft.Extensions.Caching.Hybrid | MIT | HybridCache (L1 in-memory) |
| Microsoft.Extensions.Logging | MIT | Logging-abstraksjon |
| Serilog.AspNetCore + Sinks.Console + Sinks.File | Apache-2.0 | Strukturert logging |
| FluentValidation | Apache-2.0 | Request-validering |
| CommunityToolkit.Mvvm | MIT | MVVM source generators |
| MudBlazor | MIT | UI-komponentbibliotek |
| xUnit, FluentAssertions, NSubstitute, WireMock.Net | Apache-2.0 / BSD-3 | Test-rammeverk og mocking |

**Lisens-kompatibilitet:** Alle disse er MIT, Apache-2.0 eller BSD-3-Clause — fullt kompatible med vår AGPL-3.0 outbound lisens.

## Data fra Brønnøysundregistrene

- **Lisens:** [NLOD 2.0](https://data.norge.no/nlod/) (Norwegian License for Open Data) — fri bruk
- **Krediteres:** Data fra Brønnøysundregistrene, lisensiert under NLOD 2.0
- **API-dokumentasjon:** <https://data.brreg.no/enhetsregisteret/api/dokumentasjon/no/>

### Brønnøysundregistrenes egne åpen kildekode-prosjekter

Vår implementasjon er forankret mot følgende ressurser publisert av Brreg:

- **OpenAPI-spesifikasjoner:** <https://github.com/brreg/openAPI> (MIT) — vi kan regenerere DTOs herfra ved behov
- **Hoved-dokumentasjonshub:** <https://brreg.github.io/docs/apidokumentasjon/> — autoritativ kilde for endpoints og felter
- **Maskinporten-integrasjonsveiledning:** <https://brreg.github.io/docs/apidokumentasjon/integrasjon-maskinporten/mp-integrasjonsveiledning/> — følges trinn-for-trinn ved Fase 4 (Roller-API med fnr)
- **Reference-app for Maskinporten-integrasjon (Java):** <https://github.com/brreg/refapp-integrasjon> — arkitektur-mønster vi følger i .NET-implementasjonen

### Maskinporten (Digdir)

Vår Maskinporten-klient er **egen implementasjon** skrevet fra bunnen for å unngå
lisens-kompleksitet med AGPL + kommersiell dobbel lisens. Vi følger Digdirs
offisielle guide og krediterer kilder vi har lest:

- **Digdir konsumentguide** (autoritativ kilde): <https://docs.digdir.no/docs/Maskinporten/maskinporten_guide_apikonsument.html>
- **Samarbeidsportalen** (klient-registrering): <https://samarbeid.digdir.no/>
- **Brreg integrasjonsveiledning:** <https://brreg.github.io/docs/apidokumentasjon/integrasjon-maskinporten/mp-integrasjonsveiledning/>
- **KS FIKS Maskinporten-klient** som referansebibliotek for sanity-check (vi bruker
  ikke koden direkte): <https://github.com/ks-no/fiks-maskinporten-client-dotnet> (Apache-2.0)

## Litteratur — verifiserbare henvisninger

Disclaimer: Sidetall varierer mellom utgaver — vi gir kapittel-henvisning som mer stabil referanse. Direkte sitater er parafrasert konservativt for å unngå feilsitering. **Verifiser mot originalen.**

### Bøker

- **Robert C. Martin** — *Clean Architecture: A Craftsman's Guide to Software Structure and Design* (2017, Prentice Hall, ISBN 978-0134494166)
  - Sentralt: Ch. 22 "The Clean Architecture"
  - Forfatterens egen blogpost som forløp: <https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html>

- **Eric Evans** — *Domain-Driven Design: Tackling Complexity in the Heart of Software* (2003, Addison-Wesley, ISBN 978-0321125217)
  - Sentralt: Ch. 5 "A Model Expressed in Software" (Value Objects, Entities)

- **Gamma, Helm, Johnson, Vlissides** — *Design Patterns: Elements of Reusable Object-Oriented Software* (1994, Addison-Wesley, ISBN 978-0201633610)
  - Strategy (Ch. 5), Adapter (Ch. 4), Decorator (Ch. 4)
  - Sekundærkilde med samme definisjoner: <https://refactoring.guru/design-patterns>

- **Martin Fowler** — *Patterns of Enterprise Application Architecture* (2002, Addison-Wesley, ISBN 978-0321127426)
  - Repository, Cache-aside
  - Fritt tilgjengelig katalog: <https://martinfowler.com/eaaCatalog/>

- **Michael T. Nygard** — *Release It! Design and Deploy Production-Ready Software* (2018, 2. utg., Pragmatic Bookshelf, ISBN 978-1680502398)
  - Circuit Breaker, Bulkhead, Timeout, Retry
  - Fowlers CircuitBreaker-introduksjon: <https://martinfowler.com/bliki/CircuitBreaker.html>

- **Mark Seemann & Steven van Deursen** — *Dependency Injection Principles, Practices, and Patterns* (2019, Manning, ISBN 978-1617294730)
  - Seemanns blogg: <https://blog.ploeh.dk/>

### Online-kilder (fritt tilgjengelig)

- **Alistair Cockburn** — "Hexagonal Architecture" (2005): <https://alistair.cockburn.us/hexagonal-architecture/>
- **Edsger W. Dijkstra** — "On the role of scientific thought" (EWD 447, 1974): <https://www.cs.utexas.edu/~EWD/transcriptions/EWD04xx/EWD447.html> (opphav til Separation of Concerns)

### Microsoft Learn (alt fritt tilgjengelig)

- Clean Architecture i .NET: <https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture>
- HybridCache: <https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid>
- HTTP Resilience (Polly v8): <https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience>
- Dependency Injection: <https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection>
- MVVM i .NET MAUI: <https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm>
- Blazor Hybrid med MAUI: <https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/tutorials/maui>
- WatchConnectivity (Apple): <https://developer.apple.com/documentation/watchconnectivity>
- Wearable Data Layer (Android): <https://developer.android.com/training/wearables/data/data-layer>
