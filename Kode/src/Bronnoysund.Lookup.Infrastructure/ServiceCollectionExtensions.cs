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

        // Aggregator (Phase 0: core-only). Phase 4 replaces this with ParallelCompanyDataAggregator.
        services.AddSingleton<ICompanyDataAggregator, CoreOnlyAggregator>();

        // Stub providers for registries not yet implemented — throw RegistryNotAvailableException.
        services.AddSingleton<IRolesProvider, NotAvailableRolesProvider>();
        services.AddSingleton<IAnnualReportProvider, NotAvailableAnnualReportProvider>();
        services.AddSingleton<IBeneficialOwnerProvider, NotAvailableBeneficialOwnerProvider>();
        services.AddSingleton<IDebtRegisterProvider, NotAvailableDebtRegisterProvider>();
        services.AddSingleton<IBankruptcyProvider, NotAvailableBankruptcyProvider>();
        services.AddSingleton<ISubUnitsProvider, NotAvailableSubUnitsProvider>();
        services.AddSingleton<IPersonRolesProvider, NotAvailablePersonRolesProvider>();

        return services;
    }
}
