// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.International;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Denmark;

/// <summary>
/// Adapter for <see cref="ICompanyProvider"/> backed by the Erhvervsstyrelsen CVR
/// Elasticsearch endpoint. Posts an ES Query DSL document with a term filter on
/// <c>Vrvirksomhed.cvrNummer</c>; the response wraps the company document inside
/// <c>hits.hits[0]._source.Vrvirksomhed</c>. Until Basic-auth credentials are provisioned
/// the adapter returns Unavailable.
/// </summary>
internal sealed class CvrCompanyProvider(
    HttpClient http,
    IOptions<DenmarkOptions> options,
    ILogger<CvrCompanyProvider> logger) : ICompanyProvider
{
    public string CountryCode => "DK";

    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not DanishCvrNumber dk)
        {
            return new CompanyLookupResult.InvalidInput(
                $"CvrCompanyProvider only accepts Danish CVR numbers (got {id.CountryCode}:{id.Value}).");
        }

        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            return new CompanyLookupResult.Unavailable(
                "Denmark (CVR) credentials not configured — set Bronnoysund:International:Denmark:Username and :Password in Azure App Config.");
        }

        return await ProviderExceptionTranslator.CatchUpstreamAsync(async () =>
        {
            var query = new
            {
                query = new { term = new Dictionary<string, string> { ["Vrvirksomhed.cvrNummer"] = dk.Value } },
                size = 1,
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri("cvr-permanent/_search", UriKind.Relative))
            {
                Content = JsonContent.Create(query),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{opts.Username}:{opts.Password}")));
            using var res = await http.SendAsync(req, ct).ConfigureAwait(false);
            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new CompanyLookupResult.Unavailable("CVR ES endpoint rejected credentials — check Bronnoysund:International:Denmark.");
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("hits", out var hits) ||
                !hits.TryGetProperty("hits", out var hitArray) ||
                hitArray.ValueKind != JsonValueKind.Array ||
                hitArray.GetArrayLength() == 0)
            {
                return new CompanyLookupResult.NotFound(dk.Value);
            }
            // Code-review-2026-05-29: guard the _source/Vrvirksomhed chain — an
            // Elasticsearch shape change (e.g. _source disabled) would otherwise
            // KeyNotFoundException past the per-lookup catch.
            var firstHit = hitArray[0];
            if (!firstHit.TryGetProperty("_source", out var src) || src.ValueKind != JsonValueKind.Object ||
                !src.TryGetProperty("Vrvirksomhed", out var source) || source.ValueKind != JsonValueKind.Object)
            {
                return new CompanyLookupResult.Unavailable("CVR response missing _source.Vrvirksomhed.");
            }
            var mapped = MapToResponse(source, dk.Value);
            return mapped is null
                ? new CompanyLookupResult.NotFound(dk.Value)
                : new CompanyLookupResult.Found(mapped);
        }, "Denmark (CVR)", dk.Value, logger, ct).ConfigureAwait(false);
    }

    private static CompanyResponse? MapToResponse(JsonElement vrvirksomhed, string cvr)
    {
        var name = PickCurrentValue(vrvirksomhed, "virksomhedMetadata", "nyesteNavn", "navn");
        if (string.IsNullOrWhiteSpace(name))
        {
            // Fallback: pick the latest non-null name from "navne"
            name = PickLatest(vrvirksomhed, "navne", "navn");
        }
        if (string.IsNullOrWhiteSpace(name)) return null;

        var form = ReadString(vrvirksomhed.TryGetProperty("virksomhedMetadata", out var meta) ? meta : default, "nyesteVirksomhedsform", "kortBeskrivelse");
        var status = ReadString(vrvirksomhed.TryGetProperty("virksomhedMetadata", out var meta2) ? meta2 : default, "sammensatStatus");

        PostalAddress? address = null;
        if (vrvirksomhed.TryGetProperty("virksomhedMetadata", out var meta3) &&
            meta3.TryGetProperty("nyesteBeliggenhedsadresse", out var addr))
        {
            address = new PostalAddress(
                StreetAddress: ComposeStreet(addr),
                PostalCode: ReadString(addr, "postnummer"),
                City: ReadString(addr, "postdistrikt"),
                Municipality: ReadString(addr, "kommune", "kommuneNavn"),
                Country: ReadString(addr, "landekode") ?? "DK");
        }

        return new CompanyResponse(
            OrganizationNumber: cvr,
            OrganizationName: name!,
            CompanyType: form ?? "UKJENT",
            LanguageForm: "Unknown",
            BusinessAddress: address,
            PostalAddress: address,
            IsBankrupt: status?.Contains("Konkurs", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string? ComposeStreet(JsonElement address)
    {
        var street = ReadString(address, "vejnavn");
        var num = ReadString(address, "husnummerFra");
        var combined = $"{street ?? string.Empty} {num ?? string.Empty}".Trim();
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private static string? ReadString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var step in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(step, out var next)) return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static string? PickCurrentValue(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var step in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(step, out var next)) return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static string? PickLatest(JsonElement root, string arrayProperty, string nameField)
    {
        if (!root.TryGetProperty(arrayProperty, out var arr) || arr.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in arr.EnumerateArray())
        {
            if (item.TryGetProperty("periode", out var period) &&
                period.TryGetProperty("gyldigTil", out var gt) &&
                gt.ValueKind != JsonValueKind.Null)
            {
                continue;
            }
            if (item.TryGetProperty(nameField, out var v) && v.ValueKind == JsonValueKind.String)
            {
                return v.GetString();
            }
        }
        return null;
    }
}
