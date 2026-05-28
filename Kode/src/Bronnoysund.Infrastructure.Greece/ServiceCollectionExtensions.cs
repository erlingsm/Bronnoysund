// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Greece;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBronnoysundGreece(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GreeceOptions>().Bind(configuration.GetSection(GreeceOptions.SectionName));
        services.AddHttpClient<GemiCompanyProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<GreeceOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();
        services.AddSingleton<ICompanyProvider, GemiCompanyProvider>(sp =>
            ActivatorUtilities.CreateInstance<GemiCompanyProvider>(sp,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GemiCompanyProvider))));
        return services;
    }
}
