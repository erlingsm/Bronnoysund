// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Sweden;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBronnoysundSweden(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SwedenOptions>().Bind(configuration.GetSection(SwedenOptions.SectionName));
        services.AddMemoryCache();

        services.AddHttpClient<BolagsverketCompanyProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<SwedenOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddSingleton<ICompanyProvider, BolagsverketCompanyProvider>(sp =>
            ActivatorUtilities.CreateInstance<BolagsverketCompanyProvider>(sp,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(BolagsverketCompanyProvider))));

        return services;
    }
}
