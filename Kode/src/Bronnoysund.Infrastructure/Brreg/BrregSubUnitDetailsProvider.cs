// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Brreg.Generated.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Date = Microsoft.Kiota.Abstractions.Date;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="ISubUnitDetailsProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Maps <c>Underenhet</c> to <see cref="SubUnitDetailsResponse"/>.
/// Brreg's underenhet endpoint returns 200 OK with a thin payload when the unit is
/// soft-deleted (<c>SlettetUnderEnhet</c>) — we treat that as <see cref="SubUnitLookupResult.NotFound"/>.
/// 410 Gone (entity removed for juridical reasons) is also NotFound.
/// </summary>
internal sealed class BrregSubUnitDetailsProvider(
    BrregClient client,
    ILogger<BrregSubUnitDetailsProvider> logger) : ISubUnitDetailsProvider
{
    public async Task<SubUnitLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct)
    {
        try
        {
            var response = await client.Enhetsregisteret.Api.Underenheter[org.Value]
                .GetAsWithUnderenhetorgnrGetResponseAsync(cancellationToken: ct).ConfigureAwait(false);

            if (response?.Underenhet is null)
            {
                return new SubUnitLookupResult.NotFound(org.Value);
            }

            var mapped = MapToResponse(response.Underenhet);
            return mapped is null
                ? new SubUnitLookupResult.Unavailable("Brreg returned an unexpected response structure.")
                : new SubUnitLookupResult.Found(mapped);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound ||
                                       ex.ResponseStatusCode == (int)HttpStatusCode.Gone)
        {
            logger.LogInformation("Brreg returned {Status} for underenhet {OrgNumber}", ex.ResponseStatusCode, org.Value);
            return new SubUnitLookupResult.NotFound(org.Value);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg upstream error for underenhet {OrgNumber}", org.Value);
            return new SubUnitLookupResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for /underenheter/{org.Value}");
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Brreg circuit open for underenhet {OrgNumber}", org.Value);
            return new SubUnitLookupResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg transport error for underenhet {OrgNumber}", org.Value);
            return new SubUnitLookupResult.Unavailable(
                $"Could not contact Brreg for /underenheter/{org.Value}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg timeout for underenhet {OrgNumber}", org.Value);
            return new SubUnitLookupResult.Unavailable(
                $"Brreg did not respond within the timeout for /underenheter/{org.Value}");
        }
    }

    private static SubUnitDetailsResponse? MapToResponse(Underenhet u)
    {
        if (string.IsNullOrWhiteSpace(u.Organisasjonsnummer) || string.IsNullOrWhiteSpace(u.Navn))
        {
            return null;
        }

        return new SubUnitDetailsResponse(
            OrganizationNumber: u.Organisasjonsnummer,
            OrganizationName: u.Navn,
            ParentOrganizationNumber: NullIfEmpty(u.OverordnetEnhet),
            Website: NullIfEmpty(u.Hjemmeside),
            Email: NullIfEmpty(u.Epostadresse),
            Phone: NullIfEmpty(u.Telefon),
            MobilePhone: NullIfEmpty(u.Mobil),
            BusinessAddress: MapBusinessAddress(u.Beliggenhetsadresse),
            PostalAddress: MapPostalAddress(u.Postadresse),
            PrimaryIndustry: MapIndustry(u.Naeringskode1?.Kode, u.Naeringskode1?.Beskrivelse),
            EmployeeCount: u.HarRegistrertAntallAnsatte == true && u.AntallAnsatte is { } cnt ? (int)cnt : null,
            StartDate: ToDateOnly(u.Oppstartsdato),
            ClosedDate: ToDateOnly(u.Nedleggelsesdato),
            RegisteredDate: ToDateOnly(u.RegistreringsdatoEnhetsregisteret),
            RegisteredInVatRegistry: u.RegistrertIMvaregisteret);
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static PostalAddress? MapBusinessAddress(Underenhet_beliggenhetsadresse? a)
    {
        if (a is null) return null;
        return new PostalAddress(
            StreetAddress: JoinLines(a.Adresse),
            PostalCode: NullIfEmpty(a.Postnummer),
            City: NullIfEmpty(a.Poststed),
            Municipality: NullIfEmpty(a.Kommune),
            Country: NullIfEmpty(a.Land));
    }

    private static PostalAddress? MapPostalAddress(Underenhet_postadresse? a)
    {
        if (a is null) return null;
        return new PostalAddress(
            StreetAddress: JoinLines(a.Adresse),
            PostalCode: NullIfEmpty(a.Postnummer),
            City: NullIfEmpty(a.Poststed),
            Municipality: NullIfEmpty(a.Kommune),
            Country: NullIfEmpty(a.Land));
    }

    private static string? JoinLines(List<string>? lines) =>
        lines is { Count: > 0 }
            ? string.Join(' ', lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            : null;

    private static IndustryCode? MapIndustry(string? code, string? description) =>
        string.IsNullOrWhiteSpace(code) ? null : new IndustryCode(code, description ?? string.Empty);

    private static DateOnly? ToDateOnly(Date? kiotaDate) =>
        kiotaDate is null ? null : new DateOnly(kiotaDate.Value.Year, kiotaDate.Value.Month, kiotaDate.Value.Day);
}
