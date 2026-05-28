// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Estonia;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBronnoysundEstonia(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EstoniaOptions>()
            .Bind(configuration.GetSection(EstoniaOptions.SectionName));

        services.AddHttpClient<AriregXmlCompanyProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<EstoniaOptions>>().Value;
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
        }).AddStandardResilienceHandler();

        services.AddSingleton<ICompanyProvider, AriregXmlCompanyProvider>(sp =>
            ActivatorUtilities.CreateInstance<AriregXmlCompanyProvider>(sp,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AriregXmlCompanyProvider))));

        return services;
    }
}
