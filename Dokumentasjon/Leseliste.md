# Leseliste — teknologier og patterns brukt i Bronnoysund.Lookup

Liste over alt vi bruker, med pekere til primærkilder. Alt er åpent tilgjengelig unntatt der "(Code Magazine)" er notert — du har abonnement der.

Sortert etter lag/tema, ikke alfabetisk.

## Arkitektur-patterns

### Clean Architecture

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture>
- Robert C. Martins blogpost (forløp til boka): <https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html>
- Wikipedia: <https://en.wikipedia.org/wiki/Clean_architecture>
- Code Magazine: <https://codemag.com/Article/2207051/Clean-Architecture-Beginning-with-Clean-Architecture>

### Hexagonal Architecture / Ports & Adapters

- Alistair Cockburns originalartikkel (kort): <https://alistair.cockburn.us/hexagonal-architecture/>
- Microsoft Learn (i kontekst av .NET): <https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model>
- Wikipedia: <https://en.wikipedia.org/wiki/Hexagonal_architecture_(software)>

### Domain-Driven Design (Value Objects, Entities)

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice>
- Wikipedia: <https://en.wikipedia.org/wiki/Domain-driven_design>
- Martin Fowlers definisjon av ValueObject: <https://martinfowler.com/bliki/ValueObject.html>
- Code Magazine: <https://codemag.com/Article/1809051/Domain-Driven-Design-The-Definitive-Guide-to-Building-Robust-Software>

### Separation of Concerns

- Dijkstra EWD 447 (primærkilde, fritt tilgjengelig): <https://www.cs.utexas.edu/~EWD/transcriptions/EWD04xx/EWD447.html>
- Wikipedia: <https://en.wikipedia.org/wiki/Separation_of_concerns>

### Dependency Injection

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection>
- Mark Seemanns blogg ploeh.dk: <https://blog.ploeh.dk/2014/06/10/pure-di/>
- Wikipedia: <https://en.wikipedia.org/wiki/Dependency_injection>
- Code Magazine: <https://codemag.com/Article/1907041/Dependency-Injection-in-.NET-Core>

## GoF-patterns vi bruker

### Strategy

- Refactoring.Guru: <https://refactoring.guru/design-patterns/strategy>
- Wikipedia: <https://en.wikipedia.org/wiki/Strategy_pattern>

### Adapter

- Refactoring.Guru: <https://refactoring.guru/design-patterns/adapter>
- Wikipedia: <https://en.wikipedia.org/wiki/Adapter_pattern>

### Decorator

- Refactoring.Guru: <https://refactoring.guru/design-patterns/decorator>
- Wikipedia: <https://en.wikipedia.org/wiki/Decorator_pattern>

### Composite/Aggregator (parallelle oppslag)

- Wikipedia: <https://en.wikipedia.org/wiki/Composite_pattern>
- Microsoft Learn (Task.WhenAll for parallell): <https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/asynchronous-programming-model-apm>

## Resiliency

### Circuit Breaker, Retry, Timeout, Bulkhead (Polly v8)

- Microsoft Learn HTTP Resilience: <https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience>
- Microsoft Learn Polly-ressurser: <https://learn.microsoft.com/en-us/dotnet/core/resilience/>
- Martin Fowler CircuitBreaker: <https://martinfowler.com/bliki/CircuitBreaker.html>
- Polly GitHub: <https://github.com/App-vNext/Polly>
- Code Magazine: <https://codemag.com/Article/2207061/Building-Resilient-.NET-Applications-with-Polly>

### Cache-aside

- Microsoft Learn: <https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside>
- HybridCache (ny i .NET 9 GA): <https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid>

## .NET 10 / C# 13 språk-features

### Records og readonly record struct

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record>
- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct>

### Nullable Reference Types

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references>

### File-scoped namespaces

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/file-scoped-namespaces>

### Primary constructors (C# 12)

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#primary-constructors>

### Pattern matching

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching>

### Async/await + CancellationToken

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/>
- Microsoft Learn CancellationToken: <https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads>

## Frameworks/biblioteker

### ASP.NET Core Minimal API

- Microsoft Learn: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview>

### Blazor (Web + Hybrid + WASM)

- Microsoft Learn Blazor: <https://learn.microsoft.com/en-us/aspnet/core/blazor/>
- Blazor Hybrid med MAUI: <https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/tutorials/maui>
- Razor Class Library: <https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries>

### .NET MAUI

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/maui/>
- MAUI Blazor Hybrid intro: <https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/>
- MAUI Community Toolkit: <https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/>
- Code Magazine: <https://codemag.com/Article/2305081/Getting-Started-with-.NET-MAUI>

### CommunityToolkit.Mvvm (MVVM source generators)

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/>
- GitHub: <https://github.com/CommunityToolkit/dotnet>

### MudBlazor (UI-bibliotek)

- Hjemmeside + docs: <https://mudblazor.com/>
- GitHub: <https://github.com/MudBlazor/MudBlazor>

### Entity Framework Core (SQLite)

- Microsoft Learn EF Core: <https://learn.microsoft.com/en-us/ef/core/>
- EF Core med SQLite: <https://learn.microsoft.com/en-us/ef/core/providers/sqlite/>
- Code-first migrations: <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/>

### FluentValidation

- Hjemmeside: <https://docs.fluentvalidation.net/>

### Serilog

- Hjemmeside: <https://serilog.net/>
- Microsoft Learn (strukturert logging): <https://learn.microsoft.com/en-us/dotnet/core/extensions/logging>

### IOptions, IOptionsSnapshot, IOptionsMonitor

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/core/extensions/options>

## Speech

### MAUI on-device Speech-to-Text

- Microsoft Learn (CommunityToolkit.Maui SpeechToText): <https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/essentials/speech-to-text>
- DevBlog: <https://devblogs.microsoft.com/dotnet/speech-recognition-in-dotnet-maui-with-community-toolkit/>

### MAUI Text-to-Speech

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device-media/text-to-speech>

### Web Speech API (Blazor JS-interop)

- MDN: <https://developer.mozilla.org/en-US/docs/Web/API/Web_Speech_API>

## Watch-utvikling (Fase 5)

### Apple WatchConnectivity (Swift)

- Apple Developer: <https://developer.apple.com/documentation/watchconnectivity>
- SwiftUI for watchOS: <https://developer.apple.com/documentation/swiftui/>

### Android Wearable Data Layer (Kotlin)

- Android Developers: <https://developer.android.com/training/wearables/data/data-layer>
- Jetpack Compose for Wear: <https://developer.android.com/training/wearables/compose>

## Testing

### xUnit

- Hjemmeside: <https://xunit.net/>

### FluentAssertions

- Hjemmeside: <https://fluentassertions.com/>

### NSubstitute

- Hjemmeside: <https://nsubstitute.github.io/>

### WireMock.Net (HTTP-mock)

- GitHub: <https://github.com/WireMock-Net/WireMock.Net>

### bUnit (Razor-komponent-tester)

- Hjemmeside: <https://bunit.dev/>

## Deployment / DevOps (kommer i Fase 6)

### Containerisering (.NET 10 → Docker)

- Microsoft Learn .NET i container: <https://learn.microsoft.com/en-us/dotnet/core/docker/introduction>
- Microsoft Learn `dotnet publish` + container: <https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container>

### Kubernetes (for fremtidig hosting)

- Wikipedia: <https://en.wikipedia.org/wiki/Kubernetes>
- Microsoft Learn AKS (Azure Kubernetes Service): <https://learn.microsoft.com/en-us/azure/aks/>
- AWS EKS docs: <https://docs.aws.amazon.com/eks/>
- kubernetes.io tutorials: <https://kubernetes.io/docs/tutorials/>

### Azure App Service (alternativ til Kubernetes på Azure)

- Microsoft Learn: <https://learn.microsoft.com/en-us/azure/app-service/>

### AWS ECS / Fargate (alternativ til Kubernetes på AWS)

- AWS docs: <https://docs.aws.amazon.com/ecs/>

## Brønnøysundregistrene (offentlige data)

### Enhetsregisteret API

- Brreg api-docs: <https://data.brreg.no/enhetsregisteret/api/dokumentasjon/no/>
- GitHub-docs: <https://brreg.github.io/docs/>
- Hovedside om data og API: <https://www.brreg.no/bruke-data-fra-bronnoysundregistrene/datasett-og-api/>

### Organisasjonsnummer + MOD11

- Spesifikasjon hos Brreg: <https://www.brreg.no/om-oss/registrene-vare/om-enhetsregisteret/organisasjonsnummeret/>

### NLOD 2.0 (data-lisens)

- Norsk lisens for åpne data: <https://data.norge.no/nlod/>

## Lisens-arkitektur (AGPL-3.0 + commercial)

### AGPL-3.0

- GNU offisielt: <https://www.gnu.org/licenses/agpl-3.0.html>
- Wikipedia: <https://en.wikipedia.org/wiki/GNU_Affero_General_Public_License>

### Dual licensing-modell

- Wikipedia: <https://en.wikipedia.org/wiki/Multi-licensing>
- Bra praktisk gjennomgang (MongoDB, Nextcloud, Mautic gjør dette): søk "AGPL commercial dual licensing"

### SPDX-identifikatorer

- Spec: <https://spdx.dev/learn/handling-license-info/>

## Lesetips — anbefalt rekkefølge

Hvis du har begrenset tid, les i denne rekkefølgen for best ROI:

1. **Cockburn — Hexagonal Architecture** (5 min) — gir mental modell for porter
2. **Clean Architecture blogpost** (10 min) — Martins versjon før boka
3. **Microsoft Learn Clean Architecture i .NET** (15 min) — konkret kobling til vår stack
4. **Microsoft Learn Minimal API** (15 min) — vår WebApi-modell
5. **Microsoft Learn Blazor Hybrid** (20 min) — kobling MAUI ↔ Blazor
6. **Microsoft Learn DI in .NET** (15 min) — DI-container vi bruker
7. **Microsoft Learn HybridCache** (10 min) — caching-tilnærming
8. **Polly v8 / HTTP Resilience** (15 min) — robusthet-pipeline
9. **MudBlazor docs** (etter behov) — for UI-komponenter
10. **EF Core SQLite + migrations** (etter behov, før Fase 1)

Totalt ~2 timer for full kontekst på arkitektur og rammeverk.
