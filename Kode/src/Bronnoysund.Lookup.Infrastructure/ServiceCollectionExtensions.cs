// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Infrastructure.Aggregation;
using Bronnoysund.Lookup.Infrastructure.Brreg;
using Bronnoysund.Lookup.Infrastructure.Caching;
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
    /// Registrerer all Infrastructure: Brreg HTTP-klient (med Polly resilience),
    /// HybridCache, decorator-kjede, og NotAvailable*-stubs for ikke-implementerte registre.
    /// </summary>
    public static IServiceCollection AddBronnoysundInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BrregOptions>()
            .Bind(configuration.GetSection(BrregOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient<BrregHttpClient>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<BrregOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddHybridCache();

        // Decorator-kjede: HTTP -> CompanyProvider -> Caching-decorator (ytterste)
        services.AddSingleton<BrregCompanyProvider>();
        services.AddSingleton<ICompanyProvider>(sp =>
            new CachingCompanyProvider(
                inner: sp.GetRequiredService<BrregCompanyProvider>(),
                cache: sp.GetRequiredService<HybridCache>(),
                options: sp.GetRequiredService<IOptions<BrregOptions>>(),
                logger: sp.GetRequiredService<ILogger<CachingCompanyProvider>>()));

        // Aggregator (Fase 0: kjerne-only). Fase 4 erstatter med ParallelCompanyDataAggregator.
        services.AddSingleton<ICompanyDataAggregator, CoreOnlyAggregator>();

        // Stub-providers for ikke-implementerte registre — kaster RegistryNotAvailableException.
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
