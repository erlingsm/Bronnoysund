// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Finland.Generated;
using Bronnoysund.Infrastructure.Finland.Prh;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Bronnoysund.Infrastructure.Finland;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Finnish PRH/YTJ provider as an additional <see cref="ICompanyProvider"/>.
    /// Safe to call from any host that already invoked <c>AddBronnoysundInfrastructure</c>;
    /// the provider will be picked up automatically by <see cref="ICompanyProviderRegistry"/>
    /// alongside the Norwegian one.
    /// </summary>
    public static IServiceCollection AddBronnoysundFinland(this IServiceCollection services)
    {
        // Anonymous endpoint — PRH publishes per-IP rate limits (~60 req/min is safe per
        // their guidance) and asks integrators to include an identifying User-Agent so they
        // can reach out on misuse. AddStandardResilienceHandler gives us retry/circuit
        // breaker matching the Brreg side.
        services.AddHttpClient<PrhClient>(http =>
        {
            http.BaseAddress = new Uri("https://avoindata.prh.fi/opendata-ytj-api/v3/");
            http.Timeout = TimeSpan.FromSeconds(30);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddSingleton(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(PrhClient));
            var adapter = new HttpClientRequestAdapter(
                new AnonymousAuthenticationProvider(),
                httpClient: http);
            if (http.BaseAddress is not null)
            {
                adapter.BaseUrl = http.BaseAddress.ToString().TrimEnd('/');
            }
            return new PrhClient(adapter);
        });

        services.AddSingleton<ICompanyProvider, PrhCompanyProvider>();

        return services;
    }
}
