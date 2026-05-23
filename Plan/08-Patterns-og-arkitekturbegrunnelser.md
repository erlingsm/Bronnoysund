# 08 — Patterns og arkitekturbegrunnelser (for presentasjonen)

Dette dokumentet brukes som talepunkter under presentasjonen. Hvert avsnitt har: pattern/prinsipp → hvor i koden → kilde-litteratur → "hvorfor".

## Overordnet arkitektur: Clean Architecture

**Kilde:** Robert C. Martin, *Clean Architecture: A Craftsman's Guide to Software Structure and Design* (2017).

**Kjerneregel — The Dependency Rule:** Avhengighets-pilen peker alltid innover. Forretningslogikk (Domain, Application) er uavhengig av tekniske detaljer (Infrastructure, Presentation). Dette gjør at samme Domain/Application kan kjøres i MAUI, Blazor og Web API uten endring.

I vår løsning:

```text
Domain          ← rene C#-typer, ingen rammeverk
Application     ← bruker Domain, definerer ports som interfaces
Infrastructure  ← implementerer ports (Brreg HTTP, HybridCache, Polly)
Presentation    ← MAUI, Blazor, WebApi — vet bare om Application
```

## Separation of Concerns (SoC)

**Kilde:** Edsger W. Dijkstra (1974, "On the role of scientific thought"); senere kodifisert i SOLID-prinsippene av Robert C. Martin.

**Prinsipp:** Hver komponent skal ha ett ansvar og bare én grunn til å endre seg (Single Responsibility Principle).

**Hvor i løsningen:**

| Lag | Eneste ansvar |
| --- | --- |
| `Bronnoysund.Lookup.Domain` | Domeneregler og uforanderlige sannheter (orgnr-formel, hva en `Company` er) |
| `Bronnoysund.Lookup.Application` | Orkestrering av use cases, definisjon av porter |
| `Bronnoysund.Lookup.Infrastructure` | Tekniske integrasjoner (HTTP, cache, logging-sinks, telemetri) |
| `Bronnoysund.Lookup.WebApi` | HTTP-eksponering: routing, serialisering, statuskoder |
| `Bronnoysund.Lookup.Components` | UI-presentasjon: layout, styling, brukerinteraksjon |
| `Bronnoysund.Lookup.ViewModels` | UI-tilstand og presentasjonslogikk (ikke domene) |

**Hvorfor:** Endring i Brreg-API-formatet skal aldri kreve endring i Domain. Bytte av cache-implementasjon skal aldri kreve endring i Application. Bytte av UI-rammeverk skal aldri kreve endring i forretningsregler. Hver "akse av endring" er isolert til ett lag.

## Loose Coupling (løs kobling) og Dependency Inversion

**Kilde:** Robert C. Martin, Dependency Inversion Principle (DIP), kapittel i *Agile Software Development, Principles, Patterns, and Practices* (2002).

**Prinsipp:** Høynivå-moduler skal ikke avhenge av lavnivå-moduler. Begge skal avhenge av abstraksjoner. Abstraksjoner skal ikke avhenge av detaljer; detaljer skal avhenge av abstraksjoner.

**Hvor i løsningen:** Application definerer porter som interfaces (`ICompanyProvider`, `ISpeechToText`, `ICacheService`). Infrastructure implementerer dem. Application *kjenner ikke* til Infrastructure.

**Konkret eksempel:**

```csharp
// Application-laget (vet INGENTING om HTTP)
public interface ICompanyProvider
{
    Task<CompanyLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct);
}

public sealed class LookupCompanyHandler(ICompanyProvider provider)
{
    public Task<CompanyLookupResult> HandleAsync(OrganizationNumber org, CancellationToken ct)
        => provider.LookupAsync(org, ct);
}
```

```csharp
// Infrastructure-laget (vet ALT om HTTP, men ingenting om hvem som kaller)
internal sealed class BrregCompanyProvider(BrregHttpClient http) : ICompanyProvider
{
    public async Task<CompanyLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct)
    {
        var dto = await http.GetAsync(org, ct);
        return dto is null
            ? CompanyLookupResult.NotFound(org)
            : CompanyLookupResult.Found(dto.ToDomain());
    }
}
```

**Hvorfor løs kobling er viktig:**

- **Testbarhet** — `LookupCompanyHandler` kan testes med en NSubstitute-mock av `ICompanyProvider`. Vi trenger ikke nettverk i unit-tester.
- **Fleksibilitet** — bytte fra direkte Brreg-kall til kall via vår egen Web API (Fase 6) er en DI-registrering, ikke en omskriving.
- **Parallell utvikling** — Application-laget kan bygges og testes før Infrastructure er ferdig. Bare port-kontrakten må være på plass.
- **Vedlikehold** — endring i Brreg-DTO påvirker bare mappingen i ett sted (`BrregEnhetDto.ToDomain()`). Ingen ripple gjennom kodebasen.
- **Erstatt en avhengighet** — hvis Polly v8 erstattes av noe nytt om to år, er det Infrastructure-detalj. Application bryr seg ikke.

## Dependency Injection (DI)

**Kilde:** Mark Seemann & Steven van Deursen, *Dependency Injection Principles, Practices, and Patterns* (2019, Manning).

**Vi bruker den innebygde `Microsoft.Extensions.DependencyInjection`-containeren** (samme container som ASP.NET Core og Blazor bruker). Ingen Autofac, ingen Ninject — minste avhengighet, samme container på tvers av alle plattformer.

**Tre livstider vi bruker bevisst:**

| Lifetime | Bruk | Eksempler |
| --- | --- | --- |
| `Singleton` | Tilstandsløs tjeneste som er trygg å dele | `BrregHttpClient` (typed client, internt thread-safe), `HybridCache`, `IOptions<BrregOptions>` |
| `Scoped` | Én instans per request/scope | (relevant i Web API: per HTTP-request; i Blazor Server: per kretsforbindelse) |
| `Transient` | Ny instans hver gang | `LookupCompanyHandler`, `CompanyLookupViewModel` |

**Registrering — én extension method per lag, idempotent og kjedelig:**

```csharp
// Brreg.Application
public static IServiceCollection AddBrregApplication(this IServiceCollection services)
{
    services.AddTransient<LookupCompanyHandler>();
    services.AddTransient<SearchCompaniesByNameHandler>();
    services.AddValidatorsFromAssemblyContaining<OrganizationNumberValidator>();
    return services;
}

// Brreg.Infrastructure
public static IServiceCollection AddBrregInfrastructure(this IServiceCollection services, IConfiguration config)
{
    services.AddOptions<BrregOptions>().Bind(config.GetSection("Brreg")).ValidateOnStart();

    services.AddHttpClient<BrregHttpClient>((sp, http) =>
    {
        var opts = sp.GetRequiredService<IOptions<BrregOptions>>().Value;
        http.BaseAddress = new Uri(opts.BaseUrl);
        http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
    }).AddStandardResilienceHandler();

    services.AddHybridCache();

    // Decorator-mønster: CachingCompanyProvider wrapper BrregCompanyProvider
    services.AddSingleton<BrregCompanyProvider>();
    services.AddSingleton<ICompanyProvider>(sp =>
        new CachingCompanyProvider(
            inner: sp.GetRequiredService<BrregCompanyProvider>(),
            cache: sp.GetRequiredService<HybridCache>(),
            options: sp.GetRequiredService<IOptions<BrregOptions>>()));

    return services;
}
```

**Hvorfor DI:** Konsumenter ber om det de trenger via konstruktør — containeren leverer. Ingen `new BrregHttpClient(...)` strødd rundt; ingen statiske singletoner. Konsekvenser:

- **Testbar** — bytte til mock i tester er én linje
- **Konfigurerbar** — `IOptions<T>` lar Brreg-base-URL og TTL endres uten rekompilering
- **Konsistent** — samme DI-pattern i WebApi (Fase 0), MAUI (Fase 1), Blazor (Fase 2), MAUI Mobile (Fase 3). DI er kontrakten på tvers av plattformer.

## Hexagonal / Ports & Adapters

**Kilde:** Alistair Cockburn, "Hexagonal Architecture" (2005).

**Hvor:** Hver eksternt avhengighet uttrykkes som et interface i Application (port). Infrastructure leverer adapter-implementasjoner.

| Port | Adapter(e) |
| --- | --- |
| `ICompanyProvider` | `BrregCompanyProvider` (HTTP), senere `RemoteApiCompanyProvider` (vår API) |
| `ISpeechToText` / `ITextToSpeech` | `MauiSpeech*` (Plugin.Maui.SpeechToText), `WebSpeech*` (Web Speech API via JS-interop) |
| `IDebtRegisterProvider` / `IAccountingProvider` / `IBeneficialOwnerProvider` | `NotAvailableYet*` stubs (Fase 4) |
| `ILogger<T>` | Microsoft.Extensions.Logging — implementasjon bestemmes av hosting-plattformen |

**Hvorfor:** Gjør testing trivielt (NSubstitute mocker porter) og gjør Fase 6 (sentral backend) til en konfig-endring, ikke en omskriving.

## DDD-byggesteiner

**Kilde:** Eric Evans, *Domain-Driven Design* (2003).

- **Value Object** `OrganizationNumber`: immutable `readonly record struct` som selv-validerer i konstruksjon. Garanterer at ingen kode lenger nede i stacken må re-validere — type-systemet sikrer at hvis du har en `OrganizationNumber`, så er den gyldig.
- **Entity** `Company`: identifisert av sin `OrganizationNumber`.

## GoF-patterns brukt eksplisitt

**Kilde:** Gamma, Helm, Johnson, Vlissides, *Design Patterns: Elements of Reusable Object-Oriented Software* (1994).

| Pattern | Hvor | Hvorfor |
| --- | --- | --- |
| Strategy | `ICompanyProvider` med ulike adaptere | Bytte underliggende kilde uten å røre Application |
| Adapter | `BrregCompanyProvider` adapterer Brreg-API til vår port | Holder Brreg-spesifikke detaljer ute av Application |
| Decorator | `CachingCompanyProvider` rundt en annen `ICompanyProvider` | Krydre med caching uten å endre den dekorerte |

## Resiliency-patterns (Polly v8 / Microsoft.Extensions.Http.Resilience)

**Kilde:** Michael T. Nygard, *Release It! Design and Deploy Production-Ready Software* (2007/2018).

`AddStandardResilienceHandler()` gir oss en pipeline med:

- **Timeout** (per-attempt + total)
- **Retry** (med exponential backoff + jitter)
- **Circuit Breaker** (åpner ved gjentatte feil, lukker etter cool-down)
- **Bulkhead / Rate limiter** (begrenser samtidige requests)

**Hvorfor:** Brreg er en offentlig tjeneste — vi kan ikke anta at den alltid svarer raskt. Pipeline gir kjent oppførsel ved transient feil, og inkapsulerer hele kompleksiteten i én HttpClient-registrering.

## Cache-aside (read-through)

**Kilde:** Microsoft Caching-dokumentasjon + Martin Fowler, *Patterns of Enterprise Application Architecture* (2002).

`CachingCompanyProvider` ser cache først, kaller indre provider ved miss, lagrer resultat med TTL. TTL 24t default — orgdata endres sjelden, men ikke aldri.

## MVVM

**Kilde:** John Gossman (Microsoft, 2005); Microsoft Learn MAUI-dokumentasjon.

- View (Razor / XAML) ← binder mot ViewModel
- ViewModel (CommunityToolkit.Mvvm) ← orkestrerer mot Application
- Model (Domain + Application + DTOs)

**Hvorfor:** Deler ViewModel mellom MAUI og Blazor. CommunityToolkit.Mvvm bruker source generators, så lite boilerplate (`[ObservableProperty]`, `[RelayCommand]` genererer INotifyPropertyChanged-kode ved kompilering).

## Companion-pattern på watch

**Kilde:** Apple Developer (WatchConnectivity), Android Developers (Wearable Data Layer).

Watch er thin client, telefon er backend. Begrunnelse: batteri, kode-vekt, robusthet — og at klokken kan stole på at telefonen allerede har cache.

## Moderne .NET 10 og C#-teknikker vi bruker

**Kilder:** Microsoft Learn (.NET 10 release notes), Mads Torgersen (C# language design), David Fowler (ASP.NET Core).

| Teknikk | Bruk | Hvorfor |
| --- | --- | --- |
| **Nullable Reference Types** (`<Nullable>enable</Nullable>` i alle prosjekter) | Hele kodebasen | Kompilator hindrer NRE i de fleste tilfeller; gjør null-intensjoner eksplisitte |
| **File-scoped namespaces** (C# 10) | Alle filer | Mindre indentation, renere kode |
| **Implicit `global using`** (`<ImplicitUsings>enable</ImplicitUsings>`) | Alle prosjekter | Mindre boilerplate; vanlige using-er bare virker |
| **Primary constructors** (C# 12) | Use case handlers, providers | Mindre boilerplate for DI; `public sealed class Foo(IDep dep)` |
| **Records** og **`readonly record struct`** | DTOs, Value Objects | Innebygd verdi-likhet, immutability ved default |
| **`required` keyword** (C# 11) | DTO-felter som må settes | Kompilator-håndhevelse av init |
| **`sealed` by default** | De fleste klasser | Performance + intensjonelt design (Microsoft-anbefaling) |
| **Pattern matching** | Resultat-diskriminanter (`switch result { Found f => ..., NotFound => ... }`) | Lesbar feilhåndtering uten exceptions |
| **`async/await` overalt** | Alt IO | Skalerbarhet og responsivitet |
| **`CancellationToken` i alle async-metoder** | Hele Application + Infrastructure | Tidlig avbrudd, ressursbevisst |
| **Top-level statements** (C# 9+) | `Program.cs` | Minimalt boilerplate i Web API entry-point |
| **Minimal APIs** (ASP.NET Core 6+) | `WebApi/Program.cs` | Mindre ceremoni for små APIer; passer perfekt for ett endepunkt |
| **`IAsyncEnumerable<T>`** | Søke-endepunkt (Fase 4) når vi paginer over Brreg-treff | Streaming uten å lasta alt i minne |
| **Source generators** (CommunityToolkit.Mvvm, FluentValidation) | ViewModels + validators | Genererer kode på kompileringstid → ingen runtime reflection-kostnad |
| **`HybridCache`** (.NET 9 GA, .NET 10 fortsatt anbefalt) | Cache rundt Brreg-kall | Erstatter eldre `IMemoryCache` + `IDistributedCache`-kombinasjon |
| **`AddStandardResilienceHandler()`** (Polly v8) | Brreg HttpClient | Industri-standard resilience uten egen pipeline-kode |
| **`IOptions<T>` / `IOptionsSnapshot<T>` / `IOptionsMonitor<T>`** | Konfigurasjon | Strongly-typed config med live-reload-mulighet |

## Hva vi *ikke* gjør (og hvorfor)

| Avstått | Grunn |
| --- | --- |
| CQRS m/ event sourcing | Overkill for lookup-app uten skrive-side |
| MediatR | Liten verdi når vi har 1-2 use cases; en direkte injisert handler er klarere — og MediatR ble nylig kommersielt |
| Microservices | Vi har én bounded context |
| Egen ORM/database for cache | HybridCache + 24t TTL holder; vi har ingen lokal "sannhet" å lagre |
| Innebygd autentisering | Ingen sensitive data; offentlig register |
| Static helpers / service locator | Bruker DI overalt — testbart og eksplisitt |
| `new HttpClient()` direkte | Bruker `IHttpClientFactory` — håndterer socket-utlevering og resilience-pipeline |

## Litteratur — verifiserbare henvisninger

**Disclaimer for presentasjonen:** Sammendragene under er skrevet med AI-bistand. Direkte sitater er forsøkt parafrasert konservativt for å unngå feilsitering, og hvor sitater er gitt er de markert som "omtrentlig". Sidetall kan variere mellom utgaver — vi gir ISBN og kapittel-navn som mer stabile referanser. Lenker til fritt tilgjengelige primærkilder er førsteprioritet. **Evaluatorer oppfordres til å verifisere mot original.**

---

### Robert C. Martin: *Clean Architecture* (2017)

- **Utgiver / ISBN:** Prentice Hall, ISBN 978-0134494166
- **Sentralt kapittel:** Ch. 22 "The Clean Architecture" (introduserer det berømte konsentriske diagrammet)
- **Hvor brukt i vår løsning:** Hele lag-modellen (Domain ← Application ← Infrastructure ← Presentation) og **The Dependency Rule**
- **Kjernebudskap (parafrasert fra ch. 22):** Source-kode-avhengigheter skal kun peke innover, mot policy/abstraksjoner — aldri ut mot detaljer. Indre lag vet ingenting om ytre lag.
- **Tilgang:** Kjøpes som trykt bok eller e-bok (Pearson, O'Reilly Learning, Amazon). Forfatterens egen artikkel som forløp til boka: <https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html> — **fritt tilgjengelig** og dekker samme grunnidé.
- **Verifikasjonstips:** Diagrammet fra ch. 22 er reprodusert mange steder online (søk "clean architecture concentric circles"). Sammenlign med Martins blogginnlegg fra 2012.

### Alistair Cockburn: "Hexagonal Architecture" (2005)

- **Format:** Artikkel på forfatterens nettside (ikke bok). **Fritt tilgjengelig.**
- **URL:** <https://alistair.cockburn.us/hexagonal-architecture/>
- **Hvor brukt:** Ports & Adapters — Application definerer interfaces (porter), Infrastructure leverer adaptere
- **Kjernebudskap (parafrasert fra artikkelens åpning):** Tillat en applikasjon å bli drevet likeverdig av brukere, andre programmer, automatiserte tester eller batch-skript, og å utvikles og testes isolert fra dens endelige runtime-enheter og databaser.
- **Verifikasjonstips:** Artikkelen er kort (~5 minutters lesning). Cockburn omtaler først konseptet som "Ports and Adapters", senere som "Hexagonal" — begge navn brukes om hverandre.

### Eric Evans: *Domain-Driven Design: Tackling Complexity in the Heart of Software* (2003)

- **Utgiver / ISBN:** Addison-Wesley, ISBN 978-0321125217 ("The Blue Book")
- **Sentrale kapitler:** Ch. 5 "A Model Expressed in Software" — definerer Entity, Value Object og Service
- **Hvor brukt:** `OrganizationNumber` som Value Object (immutable, selv-validerende, verdi-likhet); `Company` som Entity (identifisert av sitt orgnr)
- **Kjernebudskap (parafrasert fra ch. 5):** Value Objects beskriver karakteristikker, har ingen konseptuell identitet og bør være uforanderlige. Entities har identitet som vedvarer over tid og er det som skiller dem fra hverandre.
- **Tilgang:** Trykt bok eller e-bok (Pearson, Safari, Amazon). Ingen offisiell gratis-versjon. Bibliotek-tilgjengelig.
- **Verifikasjonstips:** Eric Evans har gitt foredrag om disse konseptene på YouTube/InfoQ — søk "Eric Evans DDD value object" for video-introduksjoner som bekrefter terminologien.

### Gamma, Helm, Johnson, Vlissides ("GoF"): *Design Patterns: Elements of Reusable Object-Oriented Software* (1994)

- **Utgiver / ISBN:** Addison-Wesley, ISBN 978-0201633610
- **Patterns vi bruker:**
  - **Strategy** (Behavioral Patterns, ca. ch. 5) — for `ICompanyProvider` med ulike adaptere
  - **Adapter** (Structural Patterns, ca. ch. 4) — for `BrregCompanyProvider`
  - **Decorator** (Structural Patterns, ca. ch. 4) — for `CachingCompanyProvider`
- **Kjernebudskap:** Hver pattern er beskrevet i et standardisert format: Intent, Motivation, Applicability, Structure, Participants, Collaborations, Consequences, Implementation, Sample Code, Known Uses, Related Patterns.
- **Tilgang:** Trykt bok eller e-bok. Ingen offisiell gratis-versjon. Bibliotek-tilgjengelig.
- **Verifikasjonstips:** Refactoring.Guru har gratis, korrekt sammendrag av alle 23 GoF-patterns med diagrammer: <https://refactoring.guru/design-patterns> — bra sekundærkilde for evaluator som vil sjekke pattern-definisjonen uten å låne boka.

### Martin Fowler: *Patterns of Enterprise Application Architecture* (2002)

- **Utgiver / ISBN:** Addison-Wesley, ISBN 978-0321127426 ("PoEAA")
- **Sentralt pattern hos oss:** **Repository** og **Cache-aside / read-through**
- **Hvor brukt:** Cache-decorator-mønster rundt `ICompanyProvider`
- **Tilgang:**
  - Trykt bok eller e-bok
  - **Fritt tilgjengelig katalog på Fowlers nettside:** <https://martinfowler.com/eaaCatalog/> — inneholder kortform av hvert pattern fra boka
  - **Cache-aside-pattern dokumentert hos Microsoft:** <https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside>
- **Verifikasjonstips:** Fowlers EAA-katalog er den enkleste verifikasjonsruten — kortbeskrivelser av Repository, Unit of Work, Identity Map osv. ligger gratis online.

### Michael T. Nygard: *Release It! Design and Deploy Production-Ready Software* (2018, 2. utg.)

- **Utgiver / ISBN:** Pragmatic Bookshelf, ISBN 978-1680502398 (2. utg., 2018)
- **Sentrale patterns:** **Circuit Breaker**, **Bulkhead**, **Timeout**, **Retry with backoff** — alle i del 2 "Stability Patterns" (1. utg. ch. 5; 2. utg. omorganisert, men samme tema)
- **Hvor brukt:** `AddStandardResilienceHandler()` (Polly v8) implementerer disse patterns ferdig-konfigurert
- **Tilgang:** Pragmatic Bookshelf (DRM-fri e-bok eller trykt). Boka populariserte Circuit Breaker i .NET-økosystemet.
- **Verifikasjonstips:**
  - Martin Fowlers gratis introduksjon til Circuit Breaker: <https://martinfowler.com/bliki/CircuitBreaker.html> (krediterer Nygard direkte)
  - Microsoft Resilience Patterns dokumentasjon: <https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker>

### Mark Seemann & Steven van Deursen: *Dependency Injection Principles, Practices, and Patterns* (2019)

- **Utgiver / ISBN:** Manning, ISBN 978-1617294730 (.NET-fokusert 2. utgave; 1. utg. fra 2011 het "Dependency Injection in .NET")
- **Hvor brukt:** Hele DI-tilnærmingen vår — Composition Root i `Program.cs`, constructor injection overalt, lifetime-valg (Singleton/Scoped/Transient)
- **Kjernebudskap (parafrasert):** "Pure DI" er å gjøre Dependency Injection uten en DI-container; alle moderne .NET DI-containere implementerer samme grunnprinsipper. Bygg systemet så det fungerer like godt med eller uten container.
- **Tilgang:**
  - Manning trykt/e-bok
  - **Fritt sample-kapittel hos Manning:** søk "Manning Dependency Injection Principles sample chapter"
  - Mark Seemann har en omfattende blogg som dekker samme tema: <https://blog.ploeh.dk/>
- **Verifikasjonstips:** Seemanns blogginnlegg "When to use a DI Container" og "Pure DI" gir kortform av bokas argumenter, fritt tilgjengelig.

### Edsger W. Dijkstra: "On the role of scientific thought" (1974) — EWD 447

- **Format:** Håndskrevet manuskript / typeset paper. **Public domain via UT Austin EWD Archive.**
- **URL:** <https://www.cs.utexas.edu/~EWD/transcriptions/EWD04xx/EWD447.html> (transkripsjon)
- **PDF av original:** <https://www.cs.utexas.edu/~EWD/ewd04xx/EWD447.PDF>
- **Hvor brukt:** Separation of Concerns — Dijkstra introduserte termen i denne korte essay
- **Kjernebudskap (parafrasert, fra åpningen av EWD 447):** Det er kjernekarakteristikken til intelligent tenkning å være villig til å studere i dybden ett aspekt av et tema isolert, for konsistens-skyld, samtidig som man vet at man bare jobber med ett aspekt. Dette er hva han kalte "Separation of Concerns".
- **Verifikasjonstips:** Hele EWD-arkivet er fritt tilgjengelig — over 1300 dokumenter fra Dijkstras karriere er digitalisert av UT Austin. EWD 447 er kort (5 sider) og lett å lese for evaluator.

### Microsoft / Apple / Google offisiell dokumentasjon

Alt fritt tilgjengelig — disse er **stabilt verifiserbare**:

- **Clean Architecture i .NET (Microsoft Learn):** <https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture>
- **HybridCache (.NET):** <https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid>
- **HTTP Resilience i .NET (Polly v8):** <https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience>
- **Dependency Injection i .NET:** <https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection>
- **MVVM i .NET MAUI:** <https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm>
- **CommunityToolkit.Mvvm source generators:** <https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/>
- **WatchConnectivity (Apple):** <https://developer.apple.com/documentation/watchconnectivity>
- **Wearable Data Layer API (Android):** <https://developer.android.com/training/wearables/data/data-layer>
- **MVVM original blogpost** (John Gossman, 2005, arkivert hos Microsoft): <https://learn.microsoft.com/en-us/archive/blogs/johngossman/introduction-to-modelviewviewmodel-pattern-for-building-wpf-apps>

### Verifiserings-strategi for evaluator

1. **Online-kilder først:** Cockburn-artikkelen, Dijkstra EWD 447, Fowlers EAA-katalog og Microsoft Learn-sidene over kan klikkes og verifiseres direkte på 2-3 minutter. Disse dekker ~70% av referansene våre.
2. **Sekundære oppslag:** Refactoring.Guru for GoF-patterns; Fowlers blogg for Circuit Breaker; Seemanns blogg for DI-prinsipper. Alle gratis.
3. **Bøker (Martin, Evans, GoF, Fowler PoEAA, Nygard, Seemann):** ISBN-er over kan brukes for å låne på bibliotek eller kjøpe e-bok. Kapittel-henvisninger er gitt i stedet for sidetall fordi sidetall varierer mellom utgaver.
4. **Sitater er parafrasert** der jeg ikke er 100% sikker på ordlyden. Hvis evaluator vil ha eksakt sitat, anbefales oppslag i originalen.
