// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.International;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Sweden;

/// <summary>
/// Adapter for <see cref="ICompanyProvider"/> backed by Bolagsverket's "Värdefulla
/// datamängder" REST API. OAuth2 client-credentials issues a Bearer token cached for the
/// lifetime indicated by the token response (with a 60 s safety margin). When ClientId or
/// ClientSecret is missing, returns Unavailable rather than failing.
/// </summary>
internal sealed class BolagsverketCompanyProvider(
    HttpClient http,
    IOptions<SwedenOptions> options,
    IMemoryCache tokenCache,
    ILogger<BolagsverketCompanyProvider> logger) : ICompanyProvider
{
    private const string TokenCacheKey = "bolagsverket.access_token";

    public string CountryCode => "SE";

    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not SwedishOrganizationNumber se)
        {
            return new CompanyLookupResult.InvalidInput(
                $"BolagsverketCompanyProvider only accepts Swedish organisation numbers (got {id.CountryCode}:{id.Value}).");
        }

        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            return new CompanyLookupResult.Unavailable(
                "Sweden (Bolagsverket) credentials not configured — set Bronnoysund:International:Sweden:ClientId and :ClientSecret in Azure App Config.");
        }

        return await ProviderExceptionTranslator.CatchUpstreamAsync(async () =>
        {
            var token = await GetTokenAsync(opts, ct).ConfigureAwait(false);
            using var req = new HttpRequestMessage(HttpMethod.Get,
                new Uri($"foretagsinformation/v2/organisationer/{se.Value}", UriKind.Relative));
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var res = await http.SendAsync(req, ct).ConfigureAwait(false);
            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                return new CompanyLookupResult.NotFound(se.Value);
            }
            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                tokenCache.Remove(TokenCacheKey);
                return new CompanyLookupResult.Unavailable("Bolagsverket token was rejected — credentials may be stale.");
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var mapped = MapToResponse(doc.RootElement, se.Value);
            return mapped is null
                ? new CompanyLookupResult.NotFound(se.Value)
                : new CompanyLookupResult.Found(mapped);
        }, "Sweden (Bolagsverket)", se.Value, logger, ct).ConfigureAwait(false);
    }

    private async Task<string> GetTokenAsync(SwedenOptions opts, CancellationToken ct)
    {
        if (tokenCache.TryGetValue(TokenCacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }
        using var req = new HttpRequestMessage(HttpMethod.Post, opts.TokenUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{opts.ClientId}:{opts.ClientSecret}")));
        req.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
        });
        using var res = await http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        // Code-review-2026-05-29: tolerate token-endpoint error envelopes (e.g.
        // {"error":"invalid_client"}) and unexpected response shapes by returning a
        // typed HttpRequestException, which the outer translator turns into a
        // consistent Unavailable. The earlier code threw InvalidOperationException
        // / KeyNotFoundException which propagated uncaught past the per-lookup catch.
        if (!doc.RootElement.TryGetProperty("access_token", out var tokenEl) ||
            tokenEl.ValueKind != JsonValueKind.String)
        {
            throw new HttpRequestException("Bolagsverket token response missing access_token.");
        }
        var token = tokenEl.GetString()!;
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var ei) &&
                        ei.ValueKind is JsonValueKind.Number
            ? ei.GetInt32()
            : 3600;
        tokenCache.Set(TokenCacheKey, token, TimeSpan.FromSeconds(Math.Max(60, expiresIn - 60)));
        return token;
    }

    private static CompanyResponse? MapToResponse(JsonElement root, string orgnr)
    {
        var name = ReadString(root, "namn") ?? ReadString(root, "foretagsnamn");
        if (string.IsNullOrWhiteSpace(name)) return null;
        var form = ReadString(root, "juridiskForm") ?? ReadString(root, "juridisk_form");
        var status = ReadString(root, "status");
        PostalAddress? address = null;
        if (root.TryGetProperty("postadress", out var post) && post.ValueKind == JsonValueKind.Object)
        {
            address = new PostalAddress(
                StreetAddress: ReadString(post, "gatuadress"),
                PostalCode: ReadString(post, "postnummer"),
                City: ReadString(post, "postort"),
                Municipality: ReadString(post, "kommun"),
                Country: ReadString(post, "land") ?? "SE");
        }
        return new CompanyResponse(
            OrganizationNumber: orgnr,
            OrganizationName: name!,
            CompanyType: form ?? "UKJENT",
            LanguageForm: "Unknown",
            BusinessAddress: address,
            PostalAddress: address,
            RegisteredDate: ParseDate(ReadString(root, "registreringsdatum")),
            IsBankrupt: string.Equals(status, "Konkurs", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static DateOnly? ParseDate(string? raw) =>
        DateTime.TryParse(raw, out var dt) ? DateOnly.FromDateTime(dt) : null;
}
