// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.UseCases.LookupCompany;
using Bronnoysund.Lookup.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Bronnoysund.Lookup.Application;

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
        services.AddValidatorsFromAssemblyContaining<OrganizationNumberValidator>();
        return services;
    }
}
