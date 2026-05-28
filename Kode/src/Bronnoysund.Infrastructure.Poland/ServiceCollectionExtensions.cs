// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Poland;

public static class ServiceCollectionExtensions
{
    public const string KrsHttpClient = "Bronnoysund.Poland.Krs";
    public const string CeidgHttpClient = "Bronnoysund.Poland.Ceidg";

    public static IServiceCollection AddBronnoysundPoland(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PolandOptions>()
            .Bind(configuration.GetSection(PolandOptions.SectionName));

        services.AddHttpClient(KrsHttpClient, (sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<PolandOptions>>().Value;
            http.BaseAddress = new Uri(opts.KrsBaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddHttpClient(CeidgHttpClient, (sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<PolandOptions>>().Value;
            http.BaseAddress = new Uri(opts.CeidgBaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddSingleton<ICompanyProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new PolishCompanyProvider(
                krs: factory.CreateClient(KrsHttpClient),
                ceidg: factory.CreateClient(CeidgHttpClient),
                options: sp.GetRequiredService<IOptions<PolandOptions>>(),
                logger: sp.GetRequiredService<ILogger<PolishCompanyProvider>>());
        });

        return services;
    }
}
