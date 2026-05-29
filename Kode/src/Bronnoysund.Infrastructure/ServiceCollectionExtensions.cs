// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Aggregation;
using Bronnoysund.Infrastructure.Brreg;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Caching;
using Bronnoysund.Infrastructure.Croatia;
using Bronnoysund.Infrastructure.Denmark;
using Bronnoysund.Infrastructure.Detection;
using Bronnoysund.Infrastructure.Estonia;
using Bronnoysund.Infrastructure.Finland;
using Bronnoysund.Infrastructure.Greece;
using Bronnoysund.Infrastructure.Ireland;
using Bronnoysund.Infrastructure.Latvia;
using Bronnoysund.Infrastructure.Lithuania;
using Bronnoysund.Infrastructure.OpenCorporates;
using Bronnoysund.Infrastructure.Poland;
using Bronnoysund.Infrastructure.Slovenia;
using Bronnoysund.Infrastructure.Sweden;
using Bronnoysund.Infrastructure.Remote;
using Bronnoysund.Infrastructure.Stubs;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Bronnoysund.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Infrastructure. Chooses between Fat Client (Direct against Brreg) or
    /// Thin Client (RemoteApi against our cloud WebApi) based on <c>DataSource:Mode</c> in config.
    /// HybridCache + the decorator chain are shared regardless of mode.
    /// </summary>
    public static IServiceCollection AddBronnoysundInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BrregOptions>()
            .Bind(configuration.GetSection(BrregOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<RemoteApiOptions>()
            .Bind(configuration.GetSection(RemoteApiOptions.SectionName));

        services.AddHybridCache();

        var modeText = configuration["DataSource:Mode"] ?? "Direct";
        var mode = Enum.TryParse<DataSourceMode>(modeText, ignoreCase: true, out var parsed)
            ? parsed
            : DataSourceMode.Direct;

        if (mode == DataSourceMode.RemoteApi)
        {
            // Thin Client — call our own WebApi in the cloud
            services.AddHttpClient<RemoteApiCompanyProvider>((sp, http) =>
            {
                var opts = sp.GetRequiredService<IOptions<RemoteApiOptions>>().Value;
                if (string.IsNullOrWhiteSpace(opts.BaseUrl))
                {
                    throw new InvalidOperationException(
                        "DataSource:Mode=RemoteApi requires RemoteApi:BaseUrl to be set in configuration.");
                }
                http.BaseAddress = new Uri(opts.BaseUrl);
                http.Timeout = opts.RequestTimeout;
                http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
                http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            }).AddStandardResilienceHandler();

            services.AddSingleton<ICompanyProvider>(sp =>
                new CachingCompanyProvider(
                    inner: sp.GetRequiredService<RemoteApiCompanyProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    options: sp.GetRequiredService<IOptions<BrregOptions>>(),
                    logger: sp.GetRequiredService<ILogger<CachingCompanyProvider>>()));
        }
        else
        {
            // Fat Client — call Brreg directly through the Kiota-generated client.
            // We register the HttpClient under the BrregClient typed name so Polly's
            // resilience handler attaches to the chain Kiota's adapter uses.
            services.AddHttpClient<BrregClient>((sp, http) =>
            {
                var opts = sp.GetRequiredService<IOptions<BrregOptions>>().Value;
                // Generated BaseUrl is https://data.brreg.no (from spec.servers[0]); BrregOptions
                // can still override it — useful for WireMock-based tests pointing at localhost.
                http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/').EndsWith("/enhetsregisteret/api", StringComparison.Ordinal)
                    ? opts.BaseUrl[..opts.BaseUrl.IndexOf("/enhetsregisteret/api", StringComparison.Ordinal)]
                    : opts.BaseUrl);
                http.Timeout = opts.RequestTimeout;
                http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
                http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            // Brreg ships several endpoints (notably /roller/totalbestand) with gzip-encoded
            // bodies. Without decompression we'd read raw bytes and fail the parse step.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
            })
            .AddStandardResilienceHandler();

            services.AddSingleton(sp =>
            {
                var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(BrregClient));
                var adapter = new HttpClientRequestAdapter(
                    new AnonymousAuthenticationProvider(), httpClient: http);
                // Override Kiota's spec-derived BaseUrl with our HttpClient's BaseAddress so the
                // WireMock fixture (which points BaseUrl at localhost) is honored.
                if (http.BaseAddress is not null)
                {
                    adapter.BaseUrl = http.BaseAddress.ToString().TrimEnd('/');
                }
                return new BrregClient(adapter);
            });

            services.AddSingleton<BrregCompanyProvider>();
            services.AddSingleton<ICompanyProvider>(sp =>
                new CachingCompanyProvider(
                    inner: sp.GetRequiredService<BrregCompanyProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    options: sp.GetRequiredService<IOptions<BrregOptions>>(),
                    logger: sp.GetRequiredService<ILogger<CachingCompanyProvider>>()));

            services.AddSingleton<ICompanySearchProvider, BrregCompanySearchProvider>();

            // Kodeverk-tables drift on the order of years/never, so wrap in HybridCache with
            // a 24h TTL — the four other Direct-mode providers got the same treatment in F5.
            services.AddSingleton<BrregKodeverkProvider>();
            services.AddSingleton<IKodeverkProvider>(sp =>
                new CachingKodeverkProvider(
                    inner: sp.GetRequiredService<BrregKodeverkProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    logger: sp.GetRequiredService<ILogger<CachingKodeverkProvider>>()));

            // Brreg statistics — currently just /roller/totalbestand. Wrapped in a 2-minute
            // HybridCache (H9) so dashboard tiles re-rendering on every navigation do not
            // hammer Brreg. The TTL matches the entity-changes feed: short enough to feel
            // live, long enough to absorb a render burst.
            services.AddSingleton<BrregStatisticsProvider>();
            services.AddSingleton<IBrregStatisticsProvider>(sp =>
                new CachingBrregStatisticsProvider(
                    inner: sp.GetRequiredService<BrregStatisticsProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    logger: sp.GetRequiredService<ILogger<CachingBrregStatisticsProvider>>()));
        }

        // Phase 4 aggregator: parallel calls to every registered provider, per-provider error
        // isolation via RegistryError. Falls back to stubs for registries we have not yet built.
        services.AddSingleton<ICompanyDataAggregator, ParallelCompanyDataAggregator>();

        // Real Brreg-backed providers (Direct mode only — RemoteApi mode goes through the WebApi
        // which exposes aggregated data via its own /companies/{orgnr}/aggregated endpoint).
        if (mode == DataSourceMode.Direct)
        {
            services.AddSingleton<IRolesProvider, BrregRolesProvider>();
            services.AddSingleton<ISubUnitsProvider, BrregSubUnitsProvider>();

            // The four per-entity providers below get HybridCache decorators so repeated
            // Lookup-page renders for the same orgnr do not hammer Brreg. TTLs picked per
            // volatility: sub-unit details + voluntary status are stable (1h), legal roles
            // are stable-ish (30m), change feeds are volatile (2m).
            services.AddSingleton<BrregSubUnitDetailsProvider>();
            services.AddSingleton<ISubUnitDetailsProvider>(sp =>
                new CachingSubUnitDetailsProvider(
                    inner: sp.GetRequiredService<BrregSubUnitDetailsProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    logger: sp.GetRequiredService<ILogger<CachingSubUnitDetailsProvider>>()));

            services.AddSingleton<BrregLegalRolesProvider>();
            services.AddSingleton<ILegalRolesProvider>(sp =>
                new CachingLegalRolesProvider(
                    inner: sp.GetRequiredService<BrregLegalRolesProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    logger: sp.GetRequiredService<ILogger<CachingLegalRolesProvider>>()));

            services.AddSingleton<BrregEntityChangesProvider>();
            services.AddSingleton<IEntityChangesProvider>(sp =>
                new CachingEntityChangesProvider(
                    inner: sp.GetRequiredService<BrregEntityChangesProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    logger: sp.GetRequiredService<ILogger<CachingEntityChangesProvider>>()));

            services.AddSingleton<BrregVoluntaryOrganizationProvider>();
            services.AddSingleton<IVoluntaryOrganizationProvider>(sp =>
                new CachingVoluntaryOrganizationProvider(
                    inner: sp.GetRequiredService<BrregVoluntaryOrganizationProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    logger: sp.GetRequiredService<ILogger<CachingVoluntaryOrganizationProvider>>()));

            services.AddSingleton<IVoluntaryOrganizationSearchProvider, BrregVoluntaryOrganizationSearchProvider>();

            // D2/I8: Matrikkelenhet results are stable on the order of days (cadastral updates
            // flow through municipalities slowly), so a 1-hour HybridCache TTL keeps repeat
            // Lookup-page renders for the same matrikkelenhet from hammering Brreg. The decorator
            // bypasses caching for InvalidInput so caller bugs always surface fresh.
            services.AddSingleton<BrregMatrikkelenhetProvider>();
            services.AddSingleton<IMatrikkelenhetProvider>(sp =>
                new CachingMatrikkelenhetProvider(
                    inner: sp.GetRequiredService<BrregMatrikkelenhetProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    logger: sp.GetRequiredService<ILogger<CachingMatrikkelenhetProvider>>()));

            services.AddSingleton<IBankruptcyProvider, BrregBankruptcyProvider>();
        }
        else
        {
            // Thin Client: register stubs for now — Phase 6 will add RemoteApi adapters for
            // the aggregated endpoint, which then makes per-provider registrations moot.
            services.AddSingleton<IRolesProvider, NotAvailableRolesProvider>();
            services.AddSingleton<ISubUnitsProvider, NotAvailableSubUnitsProvider>();
            services.AddSingleton<ISubUnitDetailsProvider, NotAvailableSubUnitDetailsProvider>();
            services.AddSingleton<ILegalRolesProvider, NotAvailableLegalRolesProvider>();
            services.AddSingleton<IEntityChangesProvider, NotAvailableEntityChangesProvider>();
            services.AddSingleton<IVoluntaryOrganizationProvider, NotAvailableVoluntaryOrganizationProvider>();
            services.AddSingleton<IVoluntaryOrganizationSearchProvider, NotAvailableVoluntaryOrganizationSearchProvider>();
            services.AddSingleton<IMatrikkelenhetProvider, NotAvailableMatrikkelenhetProvider>();
            services.AddSingleton<IBankruptcyProvider, NotAvailableBankruptcyProvider>();
            services.AddSingleton<IKodeverkProvider, NotAvailableKodeverkProvider>();
            services.AddSingleton<IBrregStatisticsProvider, NotAvailableBrregStatisticsProvider>();
        }

        // D1: Bulk-download catalog is mode-agnostic (it serves metadata only — the actual
        // bytes are fetched from Brreg directly by the consumer). Register unconditionally
        // so RemoteApi-mode hosts can serve it too.
        services.AddSingleton<IBulkDownloadCatalog, BrregBulkDownloadCatalog>();

        // Stub providers for registries that require gated access (Maskinporten, AML, commercial).
        services.AddSingleton<IAnnualReportProvider, NotAvailableAnnualReportProvider>();
        services.AddSingleton<IBeneficialOwnerProvider, NotAvailableBeneficialOwnerProvider>();
        services.AddSingleton<IDebtRegisterProvider, NotAvailableDebtRegisterProvider>();
        services.AddSingleton<IPersonRolesProvider, NotAvailablePersonRolesProvider>();

        // Country-agnostic plumbing — populated by the providers registered above plus the
        // Finland adapter wired up below. International adapters (Plan 21 Bølge 1+) plug in
        // as additional ICompanyProvider implementations and are picked up automatically by
        // the registry.
        services.AddSingleton<ICountryDetector, CountryDetector>();
        services.AddSingleton<ICompanyProviderRegistry, CompanyProviderRegistry>();
        services.AddSingleton<IProviderHealthTracker, InMemoryProviderHealthTracker>();
        services.AddSingleton<ILookupMetrics, MeterLookupMetrics>();

        // Plan 21 Bølge 1 — international adapters. Each one is registered unconditionally
        // (RemoteApi mode included) so the registry exposes a consistent set of country
        // codes across hosts. Adapters that need credentials check IsConfigured at request
        // time and return Unavailable with a descriptive message — they never crash the
        // host on a missing key.
        services.AddBronnoysundFinland(configuration);
        services.AddBronnoysundEstonia(configuration);
        services.AddBronnoysundIreland(configuration);
        services.AddBronnoysundPoland(configuration);

        // Plan 21 Bølge 2 — registers with longer activation lead times (OAuth signup,
        // e-mail credentials, prepaid VTA balance). All four return Unavailable with a
        // specific reason until App Config provisions the required keys.
        services.AddBronnoysundSweden(configuration);
        services.AddBronnoysundDenmark(configuration);
        services.AddBronnoysundSlovenia(configuration);
        services.AddBronnoysundLithuania(configuration);

        // Plan 21 Bølge 3 — trade-offs (Latvia has no free live API, Greece needs an
        // API key, Croatia uses OAuth2 client-credentials). Latvia today returns
        // Unavailable until the bulk-import pipeline is implemented.
        services.AddBronnoysundCroatia(configuration);
        services.AddBronnoysundGreece(configuration);
        services.AddBronnoysundLatvia(configuration);

        // Plan 21 Bølge 4 — OpenCorporates fasade covering Spain, Italy and Serbia in
        // one go. Each jurisdiction registers its own ICompanyProvider but they share
        // one HttpClient + one API token via the shared OpenCorporatesClient.
        services.AddBronnoysundOpenCorporates(configuration);

        return services;
    }
}
