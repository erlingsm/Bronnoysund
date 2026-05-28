// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bronnoysund.Infrastructure.Latvia;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBronnoysundLatvia(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LatviaOptions>().Bind(configuration.GetSection(LatviaOptions.SectionName));
        services.AddSingleton<ICompanyProvider, UrCompanyProvider>();
        return services;
    }
}
