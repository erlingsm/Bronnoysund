// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.OpenCorporates;

public static class ServiceCollectionExtensions
{
    public const string HttpClientName = "Bronnoysund.OpenCorporates";

    /// <summary>
    /// Registers the OpenCorporates fasade as three <see cref="ICompanyProvider"/>
    /// implementations: Spain (ES), Italy (IT), Serbia (RS). One shared HttpClient + one
    /// shared API token cover all three. Until the token is provisioned via Azure App
    /// Config every lookup returns Unavailable with a descriptive reason.
    /// </summary>
    public static IServiceCollection AddBronnoysundOpenCorporates(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenCorporatesOptions>().Bind(configuration.GetSection(OpenCorporatesOptions.SectionName));

        services.AddHttpClient(HttpClientName, (sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenCorporatesOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddSingleton(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new OpenCorporatesClient(http,
                sp.GetRequiredService<IOptions<OpenCorporatesOptions>>(),
                sp.GetRequiredService<ILogger<OpenCorporatesClient>>());
        });

        services.AddSingleton<ICompanyProvider, SpainCompanyProvider>();
        services.AddSingleton<ICompanyProvider, ItalyCompanyProvider>();
        services.AddSingleton<ICompanyProvider, SerbiaCompanyProvider>();

        return services;
    }
}
