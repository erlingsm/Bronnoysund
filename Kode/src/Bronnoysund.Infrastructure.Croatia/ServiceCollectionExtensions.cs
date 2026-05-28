// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Croatia;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBronnoysundCroatia(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CroatiaOptions>().Bind(configuration.GetSection(CroatiaOptions.SectionName));
        services.AddMemoryCache();
        services.AddHttpClient<SudregCompanyProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<CroatiaOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();
        services.AddSingleton<ICompanyProvider, SudregCompanyProvider>(sp =>
            ActivatorUtilities.CreateInstance<SudregCompanyProvider>(sp,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(SudregCompanyProvider))));
        return services;
    }
}
