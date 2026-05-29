// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Brreg.Generated.Models;
using Bronnoysund.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Date = Microsoft.Kiota.Abstractions.Date;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="ICompanyProvider"/> backed by the Kiota-generated <see cref="BrregClient"/>.
/// Maps the generated <c>Enhet</c> model to our <see cref="CompanyResponse"/>. Every Brreg field
/// access uses <c>?.</c> so a future spec change that drops an optional field becomes a null
/// rather than an exception — see Plan 53 for the full graceful-fallback policy.
/// </summary>
internal sealed class BrregCompanyProvider(
    BrregClient client,
    ILogger<BrregCompanyProvider> logger) : ICompanyProvider
{
    public string CountryCode => "NO";

    // Brreg is an open API with no per-host credentials.
    public bool IsConfigured => true;

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not OrganizationNumber org)
        {
            return new CompanyLookupResult.InvalidInput(
                $"BrregCompanyProvider only accepts Norwegian organization numbers (got {id.CountryCode}:{id.Value}).");
        }

        try
        {
            // The endpoint returns a composed-type wrapper that can be either Enhet (live) or
            // SlettetEnhet (soft-deleted). The non-deprecated method makes that discriminated
            // union explicit. We treat SlettetEnhet (or both null) as NotFound — callers asked
            // for a live company.
            var response = await client.Enhetsregisteret.Api.Enheter[org.Value]
                .GetAsWithEnhetorgnrGetResponseAsync(cancellationToken: ct).ConfigureAwait(false);

            if (response?.Enhet is null)
            {
                return new CompanyLookupResult.NotFound(org.Value);
            }

            var mapped = MapToResponse(response.Enhet);
            return mapped is null
                ? new CompanyLookupResult.Unavailable("Brreg returned an unexpected response structure.")
                : new CompanyLookupResult.Found(mapped);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound ||
                                       ex.ResponseStatusCode == (int)HttpStatusCode.Gone)
        {
            logger.LogInformation("Brreg returned {Status} for {OrgNumber}", ex.ResponseStatusCode, org.Value);
            return new CompanyLookupResult.NotFound(org.Value);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg upstream error for {OrgNumber}", org.Value);
            return new CompanyLookupResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for /enheter/{org.Value}");
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            // Resilience pipeline tripped — Brreg has been failing recently. Return
            // Unavailable so the UI can render "midlertidig utilgjengelig" instead of
            // bubbling a stack trace. Caching layer (CachingCompanyProvider) does not
            // cache Unavailable for long, so we'll probe again on the next request.
            logger.LogWarning(ex, "Brreg circuit open for {OrgNumber}", org.Value);
            return new CompanyLookupResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg transport error for {OrgNumber}", org.Value);
            return new CompanyLookupResult.Unavailable(
                $"Could not contact Brreg for /enheter/{org.Value}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg timeout for {OrgNumber}", org.Value);
            return new CompanyLookupResult.Unavailable(
                $"Brreg did not respond within the timeout for /enheter/{org.Value}");
        }
    }

    private static CompanyResponse? MapToResponse(Enhet e)
    {
        if (string.IsNullOrWhiteSpace(e.Organisasjonsnummer) || string.IsNullOrWhiteSpace(e.Navn))
        {
            return null;
        }

        return new CompanyResponse(
            OrganizationNumber: e.Organisasjonsnummer,
            OrganizationName: e.Navn,
            CompanyType: e.Organisasjonsform?.Kode ?? "UKJENT",
            LanguageForm: MapLanguageForm(e.Maalform),
            Website: NullIfEmpty(e.Hjemmeside),
            Email: NullIfEmpty(e.Epostadresse),
            Phone: NullIfEmpty(e.Telefon),
            MobilePhone: NullIfEmpty(e.Mobil),
            BusinessAddress: MapAddress(e.Forretningsadresse),
            PostalAddress: MapPostalAddress(e.Postadresse),
            PrimaryIndustry: MapIndustry(e.Naeringskode1?.Kode, e.Naeringskode1?.Beskrivelse),
            EmployeeCount: e.HarRegistrertAntallAnsatte == true && e.AntallAnsatte is { } cnt ? (int)cnt : null,
            SectorCode: e.InstitusjonellSektorkode?.Kode,
            SectorDescription: e.InstitusjonellSektorkode?.Beskrivelse,
            FoundingDate: ToDateOnly(e.Stiftelsesdato),
            RegisteredDate: ToDateOnly(e.RegistreringsdatoEnhetsregisteret),
            RegisteredInVatRegistry: e.RegistrertIMvaregisteret,
            RegisteredInBusinessRegistry: e.RegistrertIForetaksregisteret,
            IsBankrupt: e.Konkurs == true,
            BankruptcyDate: ToDateOnly(e.Konkursdato),
            // Slettedato is exposed on SlettetEnhet/GoneEnhet (410 responses), not on Enhet —
            // a live entity is by definition not deleted, so always null here.
            DeletedDate: null);
    }

    private static string MapLanguageForm(string? maalform) => maalform switch
    {
        "Bokmål" or "BOKM" or "NB" => "Bokmål",
        "Nynorsk" or "NYNO" or "NN" => "Nynorsk",
        _ => "Unknown",
    };

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static PostalAddress? MapAddress(Enhet_forretningsadresse? a)
    {
        if (a is null) return null;
        return new PostalAddress(
            StreetAddress: JoinLines(a.Adresse),
            PostalCode: NullIfEmpty(a.Postnummer),
            City: NullIfEmpty(a.Poststed),
            Municipality: NullIfEmpty(a.Kommune),
            Country: NullIfEmpty(a.Land));
    }

    private static PostalAddress? MapPostalAddress(Enhet_postadresse? a)
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
