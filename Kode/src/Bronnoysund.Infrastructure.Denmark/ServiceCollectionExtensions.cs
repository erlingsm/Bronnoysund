// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Denmark;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBronnoysundDenmark(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DenmarkOptions>().Bind(configuration.GetSection(DenmarkOptions.SectionName));

        services.AddHttpClient<CvrCompanyProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<DenmarkOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddSingleton<ICompanyProvider, CvrCompanyProvider>(sp =>
            ActivatorUtilities.CreateInstance<CvrCompanyProvider>(sp,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CvrCompanyProvider))));

        return services;
    }
}
