// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IVoluntaryOrganizationProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// Bypasses the Kiota generated client and goes through the same typed
/// <see cref="HttpClient"/> directly. Reason: the OpenAPI spec for
/// <c>FrivilligOrganisasjon</c> declares a <c>oneOf</c> without a discriminator
/// property, so Kiota generates a composed-type wrapper that cannot deserialize
/// the response (mapping path is <c>""</c>). Until upstream fixes the spec, we
/// read JSON manually. Polly + headers are inherited from the shared HttpClient
/// registration in <see cref="ServiceCollectionExtensions"/>.
/// </para>
/// <para>
/// Konsekvenser av Kiota-bypass-en (dokumentert eksplisitt fordi disse er
/// usynlige for den som leser adapter-koden):
/// </para>
/// <list type="number">
///   <item>
///     <description>
///     Ingen nightly drift-deteksjon for dette endepunktet. Plan 53
///     spec-drift-CI dekker kun Kiota-baserte adaptere — felt-rename
///     (f.eks. <c>frivilligOrganisasjonsstatus</c> → <c>status</c>) vil
///     bare falle gjennom til "UKJENT"-fallback uten varsel.
///     </description>
///   </item>
///   <item>
///     <description>
///     Tap av typesikker enum-deserialisering — <see cref="VoluntaryOrganizationResponse.Status"/>
///     er en <see cref="string"/>. Kiota ville gitt et sterkt-typet
///     <c>Frivilligorganisasjonsstatus</c>-enum med kompileringsfeil ved
///     nye verdier fra Brreg.
///     </description>
///   </item>
///   <item>
///     <description>
///     Egen <see cref="ParseDate"/> er mindre robust enn Kiota's
///     <c>Date</c>-håndtering: den tar kun ren <c>yyyy-MM-dd</c> og vil
///     returnere null hvis Brreg en dag sender et datetime-format.
///     </description>
///   </item>
///   <item>
///     <description>
///     Når Brreg fikser spec-en (legger til
///     <c>discriminator: { propertyName: "respons_klasse" }</c> på
///     <c>FrivilligOrganisasjon</c>'s <c>oneOf</c>), skal denne adapter-en
///     migreres tilbake til vanlig Kiota-mønster — slett den manuelle
///     JSON-modellen og bruk
///     <c>client.Frivillighetsregisteret.Api.FrivilligeOrganisasjoner[org.Value].GetAsync</c>.
///     </description>
///   </item>
/// </list>
/// </remarks>
internal sealed class BrregVoluntaryOrganizationProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<BrregVoluntaryOrganizationProvider> logger) : IVoluntaryOrganizationProvider
{
    private const string Path = "frivillighetsregisteret/api/frivillige-organisasjoner";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public async Task<VoluntaryOrganizationLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient(nameof(BrregClient));
        var url = $"{Path}/{org.Value}";

        try
        {
            using var response = await http.GetAsync(url, ct).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Gone)
            {
                return new VoluntaryOrganizationLookupResult.NotRegistered(org.Value);
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Frivillighetsregisteret returned HTTP {Status} for {OrgNumber}",
                    (int)response.StatusCode, org.Value);
                return new VoluntaryOrganizationLookupResult.Unavailable(
                    $"Frivillighetsregisteret returned HTTP {(int)response.StatusCode} for /{Path}/{org.Value}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync<FrivilligOrganisasjonJson>(stream, JsonOpts, ct)
                .ConfigureAwait(false);

            if (payload is null || string.Equals(payload.RespondsKlasse, "FrivilligOrganisasjonSlettet", StringComparison.OrdinalIgnoreCase))
            {
                return new VoluntaryOrganizationLookupResult.NotRegistered(org.Value);
            }

            return new VoluntaryOrganizationLookupResult.Found(MapToResponse(payload, org.Value));
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret circuit open for {OrgNumber}", org.Value);
            return new VoluntaryOrganizationLookupResult.Unavailable("Frivillighetsregisteret is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret transport error for {OrgNumber}", org.Value);
            return new VoluntaryOrganizationLookupResult.Unavailable(
                $"Could not contact Frivillighetsregisteret for /{Path}/{org.Value}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret timeout for {OrgNumber}", org.Value);
            return new VoluntaryOrganizationLookupResult.Unavailable(
                $"Frivillighetsregisteret did not respond within the timeout for /{Path}/{org.Value}");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret returned unparseable JSON for {OrgNumber}", org.Value);
            return new VoluntaryOrganizationLookupResult.Unavailable(
                "Frivillighetsregisteret returned an unexpected response structure.");
        }
    }

    private static VoluntaryOrganizationResponse MapToResponse(FrivilligOrganisasjonJson f, string fallbackOrg)
    {
        var primary = (f.IcnpoKategorier ?? [])
            .OrderBy(c => c.Rekkefoelge ?? int.MaxValue)
            .FirstOrDefault();

        return new VoluntaryOrganizationResponse(
            OrganizationNumber: string.IsNullOrWhiteSpace(f.Organisasjonsnummer) ? fallbackOrg : f.Organisasjonsnummer!,
            Status: f.FrivilligOrganisasjonsstatus ?? "UKJENT",
            FirstRegisteredDate: ParseDate(f.FoersteGangInnfoert),
            RegisteredDate: ParseDate(f.InnfoertDato),
            PrimaryIcnpoCategoryNumber: primary?.IcnpoNummer,
            PrimaryIcnpoCategoryName: primary?.Navn,
            ParticipatesInGrasrotandel: f.Grasrotandel?.DeltarI == true,
            AccountNumber: NullIfEmpty(f.Kontonummer));
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static DateOnly? ParseDate(string? raw) =>
        DateOnly.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;

    private sealed record FrivilligOrganisasjonJson(
        [property: JsonPropertyName("respons_klasse")] string? RespondsKlasse,
        string? Organisasjonsnummer,
        string? FrivilligOrganisasjonsstatus,
        string? FoersteGangInnfoert,
        string? InnfoertDato,
        string? Kontonummer,
        IcnpoCategoryJson[]? IcnpoKategorier,
        GrasrotandelJson? Grasrotandel);

    private sealed record IcnpoCategoryJson(string? IcnpoNummer, string? Navn, int? Rekkefoelge);

    private sealed record GrasrotandelJson(bool? DeltarI);
}
