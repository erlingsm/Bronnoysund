// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Finland.Generated;
using Bronnoysund.Infrastructure.Finland.Prh;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Bronnoysund.Infrastructure.Finland;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Finnish PRH/YTJ provider as an additional <see cref="ICompanyProvider"/>.
    /// Reads <see cref="FinlandOptions"/> from configuration (defaults match the live PRH
    /// production endpoint, so an empty config section is fine for local development);
    /// the provider is automatically picked up by <see cref="ICompanyProviderRegistry"/>.
    /// </summary>
    public static IServiceCollection AddBronnoysundFinland(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FinlandOptions>()
            .Bind(configuration.GetSection(FinlandOptions.SectionName));

        services.AddHttpClient<PrhClient>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<FinlandOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = opts.RequestTimeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
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
