// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Slovenia;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBronnoysundSlovenia(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SloveniaOptions>().Bind(configuration.GetSection(SloveniaOptions.SectionName));

        services.AddHttpClient<AjpesCompanyProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<SloveniaOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddSingleton<ICompanyProvider, AjpesCompanyProvider>(sp =>
            ActivatorUtilities.CreateInstance<AjpesCompanyProvider>(sp,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AjpesCompanyProvider))));

        return services;
    }
}
