// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Slovenia;

/// <summary>
/// Adapter for <see cref="ICompanyProvider"/> backed by AJPES restPrsInfo
/// /searchByMS endpoint. Authentication lives inside the request payload (not headers)
/// per the proprietary AJPES scheme. Returns Unavailable when credentials are missing or
/// when an HTTP 402-style "balance depleted" response is received.
/// </summary>
internal sealed class AjpesCompanyProvider(
    HttpClient http,
    IOptions<SloveniaOptions> options,
    ILogger<AjpesCompanyProvider> logger) : ICompanyProvider
{
    public string CountryCode => "SI";

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not SlovenianMaticnaStevilka si)
        {
            return new CompanyLookupResult.InvalidInput(
                $"AjpesCompanyProvider only accepts Slovenian matična številka (got {id.CountryCode}:{id.Value}).");
        }

        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            return new CompanyLookupResult.Unavailable(
                "Slovenia (AJPES) credentials not configured — set Bronnoysund:International:Slovenia:Username and :Password in Azure App Config.");
        }

        try
        {
            var payload = new
            {
                ident = new { uporabnik = opts.Username, geslo = opts.Password },
                ms = si.Value,
                tier = opts.Tier,
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri("searchByMS", UriKind.Relative))
            {
                Content = JsonContent.Create(payload),
            };
            using var res = await http.SendAsync(req, ct).ConfigureAwait(false);
            if (res.StatusCode == HttpStatusCode.PaymentRequired)
            {
                return new CompanyLookupResult.Unavailable("AJPES VTA balance depleted — top up via ePlacila.");
            }
            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new CompanyLookupResult.Unavailable("AJPES credentials rejected — check Bronnoysund:International:Slovenia.");
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("subjekt", out var subj) || subj.ValueKind != JsonValueKind.Object)
            {
                return new CompanyLookupResult.NotFound(si.Value);
            }
            var mapped = MapToResponse(subj, si.Value);
            return mapped is null
                ? new CompanyLookupResult.NotFound(si.Value)
                : new CompanyLookupResult.Found(mapped);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "AJPES transport error for {Ms}", si.Value);
            return new CompanyLookupResult.Unavailable($"Could not contact AJPES for {si.Value}: {ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new CompanyLookupResult.Unavailable($"AJPES did not respond within timeout for {si.Value}");
        }
    }

    private static CompanyResponse? MapToResponse(JsonElement subj, string ms)
    {
        var name = ReadString(subj, "imePolno") ?? ReadString(subj, "ime");
        if (string.IsNullOrWhiteSpace(name)) return null;
        var form = ReadString(subj, "oblika") ?? ReadString(subj, "vrsta");

        PostalAddress? address = null;
        if (subj.TryGetProperty("naslov", out var naslov) && naslov.ValueKind == JsonValueKind.Object)
        {
            address = new PostalAddress(
                StreetAddress: ReadString(naslov, "ulica"),
                PostalCode: ReadString(naslov, "posta"),
                City: ReadString(naslov, "kraj"),
                Municipality: ReadString(naslov, "obcina"),
                Country: "SI");
        }

        return new CompanyResponse(
            OrganizationNumber: ms,
            OrganizationName: name!,
            CompanyType: form ?? "UKJENT",
            LanguageForm: "Unknown",
            BusinessAddress: address,
            PostalAddress: address);
    }

    private static string? ReadString(JsonElement el, string p) =>
        el.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
