// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Lithuania;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBronnoysundLithuania(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LithuaniaOptions>().Bind(configuration.GetSection(LithuaniaOptions.SectionName));

        services.AddHttpClient<JarCompanyProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<LithuaniaOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddSingleton<ICompanyProvider, JarCompanyProvider>(sp =>
            ActivatorUtilities.CreateInstance<JarCompanyProvider>(sp,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(JarCompanyProvider))));

        return services;
    }
}
