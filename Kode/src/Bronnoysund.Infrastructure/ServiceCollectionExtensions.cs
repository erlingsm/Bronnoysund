// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Aggregation;
using Bronnoysund.Infrastructure.Brreg;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Caching;
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
            }).AddStandardResilienceHandler();

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
            services.AddSingleton<IBankruptcyProvider, NotAvailableBankruptcyProvider>();
            services.AddSingleton<IKodeverkProvider, NotAvailableKodeverkProvider>();
        }

        // Stub providers for registries that require gated access (Maskinporten, AML, commercial).
        services.AddSingleton<IAnnualReportProvider, NotAvailableAnnualReportProvider>();
        services.AddSingleton<IBeneficialOwnerProvider, NotAvailableBeneficialOwnerProvider>();
        services.AddSingleton<IDebtRegisterProvider, NotAvailableDebtRegisterProvider>();
        services.AddSingleton<IPersonRolesProvider, NotAvailablePersonRolesProvider>();

        return services;
    }
}
