// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Infrastructure.Aggregation;
using Bronnoysund.Lookup.Infrastructure.Brreg;
using Bronnoysund.Lookup.Infrastructure.Caching;
using Bronnoysund.Lookup.Infrastructure.Remote;
using Bronnoysund.Lookup.Infrastructure.Stubs;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Lookup.Infrastructure;

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
            // Fat Client — call Brreg directly
            services.AddHttpClient<BrregHttpClient>((sp, http) =>
            {
                var opts = sp.GetRequiredService<IOptions<BrregOptions>>().Value;
                http.BaseAddress = new Uri(opts.BaseUrl);
                http.Timeout = opts.RequestTimeout;
                http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
                http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            }).AddStandardResilienceHandler();

            services.AddSingleton<BrregCompanyProvider>();
            services.AddSingleton<ICompanyProvider>(sp =>
                new CachingCompanyProvider(
                    inner: sp.GetRequiredService<BrregCompanyProvider>(),
                    cache: sp.GetRequiredService<HybridCache>(),
                    options: sp.GetRequiredService<IOptions<BrregOptions>>(),
                    logger: sp.GetRequiredService<ILogger<CachingCompanyProvider>>()));
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
            services.AddSingleton<IBankruptcyProvider, BrregBankruptcyProvider>();
        }
        else
        {
            // Thin Client: register stubs for now — Phase 6 will add RemoteApi adapters for
            // the aggregated endpoint, which then makes per-provider registrations moot.
            services.AddSingleton<IRolesProvider, NotAvailableRolesProvider>();
            services.AddSingleton<ISubUnitsProvider, NotAvailableSubUnitsProvider>();
            services.AddSingleton<IBankruptcyProvider, NotAvailableBankruptcyProvider>();
        }

        // Stub providers for registries that require gated access (Maskinporten, AML, commercial).
        services.AddSingleton<IAnnualReportProvider, NotAvailableAnnualReportProvider>();
        services.AddSingleton<IBeneficialOwnerProvider, NotAvailableBeneficialOwnerProvider>();
        services.AddSingleton<IDebtRegisterProvider, NotAvailableDebtRegisterProvider>();
        services.AddSingleton<IPersonRolesProvider, NotAvailablePersonRolesProvider>();

        return services;
    }
}
