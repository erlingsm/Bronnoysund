// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.International;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Poland;

/// <summary>
/// Adapter that routes Polish identifiers to the correct backend: KRS (no auth) for
/// PolishKrsNumber, CEIDG v3 (Bearer) for PolishNip and PolishRegon. The KRS open API is
/// the anonymised "Odpis aktualny" variant; the unprefixed full variant is reserved for
/// public-sector consumers and unavailable to us. KRS requires an explicit
/// <c>?rejestr={P|S}</c> parameter — we try the predominant P (przedsiębiorców /
/// commercial register) first and fall back to S (stowarzyszeń / associations).
/// </summary>
internal sealed class PolishCompanyProvider(
    HttpClient krs,
    HttpClient ceidg,
    IOptions<PolandOptions> options,
    ILogger<PolishCompanyProvider> logger) : ICompanyProvider
{
    private static readonly string[] KrsRegisters = ["P", "S"];

    public string CountryCode => "PL";

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        return id switch
        {
            PolishKrsNumber k => await LookupKrsAsync(k, ct).ConfigureAwait(false),
            PolishNip n => await LookupCeidgAsync(n.Value, "nip", ct).ConfigureAwait(false),
            PolishRegon r => await LookupCeidgAsync(r.Value, "regon", ct).ConfigureAwait(false),
            _ => new CompanyLookupResult.InvalidInput(
                $"PolishCompanyProvider only accepts KRS, NIP or REGON (got {id.CountryCode}:{id.Value}).")
        };
    }

    private async Task<CompanyLookupResult> LookupKrsAsync(PolishKrsNumber krsNumber, CancellationToken ct)
    {
        return await ProviderExceptionTranslator.CatchUpstreamAsync(async () =>
        {
            // Try the commercial register first; on a 404 fall back to the associations
            // register. Per-attempt transient failures are recorded and re-raised only if
            // we exit the loop without ever returning Found — at that point we can't
            // distinguish "not in P, not in S" from "didn't actually find out about P":
            //
            //   P=Found             → return Found
            //   P=transient + S=Found → return Found
            //   P=404 + S=Found     → return Found
            //   P=404 + S=404       → NotFound  (we know it's nowhere)
            //   P=transient + S=404 → Unavailable  (we don't know P; can't claim NotFound)
            //   P=404 + S=transient → Unavailable  (we don't know S)
            //   P=transient + S=transient → Unavailable
            //
            // The iter-2 review questioned whether P=transient+S=404 should be NotFound;
            // we keep Unavailable because we honestly don't know whether the KRS exists
            // in P. Code-review-2026-05-29-iter2 false-positive (verified semantics).
            Exception? lastTransientException = null;
            foreach (var rejestr in KrsRegisters)
            {
                try
                {
                    var url = $"api/krs/OdpisAktualny/{krsNumber.Value}?rejestr={rejestr}&format=json";
                    using var res = await krs.GetAsync(new Uri(url, UriKind.Relative), ct).ConfigureAwait(false);
                    if (res.StatusCode == HttpStatusCode.NotFound) continue;
                    res.EnsureSuccessStatusCode();

                    await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                    var mapped = MapKrsToResponse(doc.RootElement, krsNumber.Value);
                    return mapped is null
                        ? new CompanyLookupResult.Unavailable("KRS returned an unexpected response structure.")
                        : new CompanyLookupResult.Found(mapped);
                }
                catch (HttpRequestException ex)
                {
                    lastTransientException = ex;
                }
                catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
                {
                    lastTransientException = ex;
                }
            }
            if (lastTransientException is not null)
            {
                // Re-throw so the shared translator gives a single canonical Unavailable.
                throw lastTransientException;
            }
            return new CompanyLookupResult.NotFound(krsNumber.Value);
        }, "Poland (KRS)", krsNumber.Value, logger, ct).ConfigureAwait(false);
    }

    private async Task<CompanyLookupResult> LookupCeidgAsync(string value, string queryParam, CancellationToken ct)
    {
        var opts = options.Value;
        if (!opts.CeidgIsConfigured)
        {
            return new CompanyLookupResult.Unavailable(
                "CEIDG bearer token not configured — set Bronnoysund:International:Poland:CeidgBearerToken in Azure App Config. (KRS lookups by KRS number still work.)");
        }

        return await ProviderExceptionTranslator.CatchUpstreamAsync(async () =>
        {
            var url = $"api/ceidg/v3/firmy?{queryParam}={value}&limit=1";
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Relative));
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.CeidgBearerToken);
            using var res = await ceidg.SendAsync(req, ct).ConfigureAwait(false);
            if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            {
                return new CompanyLookupResult.NotFound(value);
            }
            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new CompanyLookupResult.Unavailable("CEIDG bearer token was rejected — check Azure App Config / token expiry.");
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var mapped = MapCeidgToResponse(doc.RootElement, value);
            return mapped is null
                ? new CompanyLookupResult.NotFound(value)
                : new CompanyLookupResult.Found(mapped);
        }, "Poland (CEIDG)", value, logger, ct).ConfigureAwait(false);
    }

    private static CompanyResponse? MapKrsToResponse(JsonElement root, string krs)
    {
        if (!root.TryGetProperty("odpis", out var odpis) ||
            !odpis.TryGetProperty("dane", out var dane) ||
            !dane.TryGetProperty("dzial1", out var d1) ||
            !d1.TryGetProperty("danePodmiotu", out var podmiot))
        {
            return null;
        }
        var name = ReadString(podmiot, "nazwa");
        if (string.IsNullOrWhiteSpace(name)) return null;

        string? form = null, nip = null, regon = null;
        if (podmiot.TryGetProperty("formaPrawna", out var fp)) form = ReadString(fp, "nazwa");
        if (d1.TryGetProperty("identyfikatory", out var ids))
        {
            nip = ReadString(ids, "nip");
            regon = ReadString(ids, "regon");
        }

        PostalAddress? address = null;
        if (dane.TryGetProperty("dzial1", out var d1b) &&
            d1b.TryGetProperty("siedzibaIAdres", out var siedziba) &&
            siedziba.TryGetProperty("adres", out var adres))
        {
            address = new PostalAddress(
                StreetAddress: $"{ReadString(adres, "ulica") ?? string.Empty} {ReadString(adres, "nrDomu") ?? string.Empty}".Trim(),
                PostalCode: ReadString(adres, "kodPocztowy"),
                City: ReadString(adres, "miejscowosc"),
                Municipality: null,
                Country: ReadString(adres, "kraj") ?? "PL");
        }

        return new CompanyResponse(
            OrganizationNumber: krs,
            OrganizationName: name!,
            CompanyType: form ?? "UKJENT",
            LanguageForm: "Unknown",
            BusinessAddress: address,
            PostalAddress: address);
    }

    private static CompanyResponse? MapCeidgToResponse(JsonElement root, string id)
    {
        if (!root.TryGetProperty("firma", out var firmaArr) || firmaArr.ValueKind != JsonValueKind.Array || firmaArr.GetArrayLength() == 0)
        {
            return null;
        }
        var firma = firmaArr[0];
        var name = ReadString(firma, "nazwa");
        if (string.IsNullOrWhiteSpace(name)) return null;

        PostalAddress? address = null;
        if (firma.TryGetProperty("adresGlownegoMiejscaWykonywaniaDzialalnosci", out var adres) && adres.ValueKind == JsonValueKind.Object)
        {
            address = new PostalAddress(
                StreetAddress: $"{ReadString(adres, "ulica") ?? string.Empty} {ReadString(adres, "budynek") ?? string.Empty}".Trim(),
                PostalCode: ReadString(adres, "kod"),
                City: ReadString(adres, "miasto"),
                Municipality: ReadString(adres, "gmina"),
                Country: ReadString(adres, "kraj") ?? "PL");
        }

        return new CompanyResponse(
            OrganizationNumber: ReadString(firma, "nip") ?? id,
            OrganizationName: name!,
            CompanyType: "ENK",
            LanguageForm: "Unknown",
            BusinessAddress: address,
            PostalAddress: address,
            RegisteredDate: ParseDate(ReadString(firma, "dataRozpoczeciaWykonywaniaDzialalnosci")));
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static DateOnly? ParseDate(string? raw) =>
        DateTime.TryParse(raw, out var dt) ? DateOnly.FromDateTime(dt) : null;
}
