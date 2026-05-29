// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Application.UseCases.LookupCompany;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.MauiMobile.Watch;

/// <summary>
/// Bridges a Watch companion message into the country-aware lookup pipeline and
/// returns a <see cref="LookupResponse"/> ready for serialisation. Owns the per-message
/// DI scope so transient ports stay decoupled from the long-lived listener/delegate.
///
/// `countryCode` is not on <see cref="Application.Dtos.CompanyResponse"/>; we derive it
/// from <see cref="ICountryDetector"/> upstream so the listener stays a thin wire-shim.
/// </summary>
public sealed class WatchLookupService(IServiceProvider rootServices, ILogger<WatchLookupService> logger)
{
    public async Task<LookupResponse> HandleAsync(LookupRequest request, CancellationToken ct)
    {
        logger.LogInformation("Watch request action={Action} v={Version}", request.Action, request.Version);
        if (request.Version != WatchProtocol.CurrentVersion)
        {
            return Invalid("unsupportedVersion", $"Protocol version {request.Version} is not supported by this phone.");
        }

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            return Invalid("emptyValue", "Tomt søk.");
        }

        return request.Action switch
        {
            "lookup" => await LookupAsync(request.Value.Trim(), ct).ConfigureAwait(false),
            "search" => Invalid("notImplemented", "Navnesøk støttes ikke i denne protokoll-versjonen."),
            _ => Invalid("unsupportedAction", $"Ukjent handling '{request.Action}'."),
        };
    }

    private async Task<LookupResponse> LookupAsync(string value, CancellationToken ct)
    {
        await using var scope = rootServices.CreateAsyncScope();
        var detector = scope.ServiceProvider.GetRequiredService<ICountryDetector>();
        var handler = scope.ServiceProvider.GetRequiredService<LookupCompanyHandler>();

        var identifier = detector.Detect(value);
        var countryCode = identifier?.CountryCode;

        var outcome = await handler.HandleAsync(new LookupCompanyQuery(value), ct).ConfigureAwait(false);
        return outcome switch
        {
            CompanyLookupResult.Found f => new LookupResponse(
                Version: WatchProtocol.CurrentVersion,
                Result: "found",
                OrganizationNumber: f.Company.OrganizationNumber,
                OrganizationName: f.Company.OrganizationName,
                CompanyType: f.Company.CompanyType,
                LanguageForm: f.Company.LanguageForm,
                CountryCode: countryCode),
            CompanyLookupResult.NotFound nf => new LookupResponse(
                Version: WatchProtocol.CurrentVersion,
                Result: "notFound",
                Value: nf.OrganizationNumber,
                Message: $"Fant ingen treff for {nf.OrganizationNumber}."),
            CompanyLookupResult.InvalidInput inv => Invalid("unrecognizedFormat", inv.Message),
            CompanyLookupResult.Unavailable un => new LookupResponse(
                Version: WatchProtocol.CurrentVersion,
                Result: "unavailable",
                Message: un.Message),
            _ => new LookupResponse(
                Version: WatchProtocol.CurrentVersion,
                Result: "unavailable",
                Message: "Uventet svar fra lookup-tjenesten."),
        };
    }

    private static LookupResponse Invalid(string code, string message) => new(
        Version: WatchProtocol.CurrentVersion,
        Result: "invalid",
        Code: code,
        Message: message);
}
