// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Ireland;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBronnoysundIreland(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<IrelandOptions>()
            .Bind(configuration.GetSection(IrelandOptions.SectionName));

        services.AddHttpClient<CroCompanyProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<IrelandOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddSingleton<ICompanyProvider, CroCompanyProvider>(sp =>
            ActivatorUtilities.CreateInstance<CroCompanyProvider>(sp,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CroCompanyProvider))));

        return services;
    }
}
