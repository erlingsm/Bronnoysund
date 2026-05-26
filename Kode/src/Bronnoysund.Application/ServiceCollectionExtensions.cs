// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.UseCases.LookupCompany;
using Bronnoysund.Application.UseCases.SearchCompaniesByName;
using Bronnoysund.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bronnoysund.Application;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Application-layer services: use case handlers and validators.
    /// Requires that Infrastructure has registered ports (ICompanyProvider etc.) either before
    /// or after — the DI order is irrelevant for resolve.
    /// </summary>
    public static IServiceCollection AddBronnoysundApplication(this IServiceCollection services)
    {
        services.AddTransient<LookupCompanyHandler>();
        services.AddTransient<LookupAggregatedCompanyHandler>();
        services.AddTransient<SearchCompaniesByNameHandler>();
        services.AddValidatorsFromAssemblyContaining<OrganizationNumberValidator>();

        // Host-replaceable: MAUI registers MauiDeviceLayout, Blazor Web registers
        // WebDeviceLayout. TryAddSingleton means the host registration (which runs after this
        // call by convention) wins because it was added first; this fallback only takes
        // effect when no host adapter exists (e.g. the headless WebApi).
        services.TryAddSingleton<IDeviceLayout, DesktopDeviceLayout>();

        return services;
    }
}
