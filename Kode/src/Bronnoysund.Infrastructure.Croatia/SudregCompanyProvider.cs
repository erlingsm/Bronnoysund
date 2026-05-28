// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Croatia;

internal sealed class SudregCompanyProvider(
    HttpClient http,
    IOptions<CroatiaOptions> options,
    IMemoryCache tokenCache,
    ILogger<SudregCompanyProvider> logger) : ICompanyProvider
{
    private const string TokenCacheKey = "sudreg.access_token";

    public string CountryCode => "HR";

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not CroatianOib hr)
        {
            return new CompanyLookupResult.InvalidInput(
                $"SudregCompanyProvider only accepts Croatian OIB (got {id.CountryCode}:{id.Value}).");
        }

        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            return new CompanyLookupResult.Unavailable(
                "Croatia (Sudski Registar) credentials not configured — set Bronnoysund:International:Croatia:ClientId and :ClientSecret in Azure App Config.");
        }

        try
        {
            var token = await GetTokenAsync(opts, ct).ConfigureAwait(false);
            using var req = new HttpRequestMessage(HttpMethod.Get,
                new Uri($"api/javni/subjekt_detalji?oib={hr.Value}", UriKind.Relative));
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var res = await http.SendAsync(req, ct).ConfigureAwait(false);
            if (res.StatusCode == HttpStatusCode.NotFound) return new CompanyLookupResult.NotFound(hr.Value);
            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                tokenCache.Remove(TokenCacheKey);
                return new CompanyLookupResult.Unavailable("Sudski Registar token rejected — credentials may be stale.");
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var mapped = MapToResponse(doc.RootElement, hr.Value);
            return mapped is null
                ? new CompanyLookupResult.NotFound(hr.Value)
                : new CompanyLookupResult.Found(mapped);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Sudski Registar transport error for {Oib}", hr.Value);
            return new CompanyLookupResult.Unavailable($"Could not contact Sudski Registar for {hr.Value}: {ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new CompanyLookupResult.Unavailable($"Sudski Registar did not respond within timeout for {hr.Value}");
        }
    }

    private async Task<string> GetTokenAsync(CroatiaOptions opts, CancellationToken ct)
    {
        if (tokenCache.TryGetValue(TokenCacheKey, out string? cached) && cached is not null) return cached;
        using var req = new HttpRequestMessage(HttpMethod.Post, opts.TokenUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{opts.ClientId}:{opts.ClientSecret}")));
        req.Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("grant_type", "client_credentials") });
        using var res = await http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        var token = doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Sudski Registar token response missing access_token.");
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 21600;
        tokenCache.Set(TokenCacheKey, token, TimeSpan.FromSeconds(Math.Max(60, expiresIn - 60)));
        return token;
    }

    private static CompanyResponse? MapToResponse(JsonElement root, string oib)
    {
        var name = ReadString(root, "ime") ?? ReadString(root, "naziv");
        if (string.IsNullOrWhiteSpace(name)) return null;
        var form = ReadString(root, "pravni_oblik") ?? ReadString(root, "oblik");
        PostalAddress? address = null;
        if (root.TryGetProperty("sjediste", out var sj) && sj.ValueKind == JsonValueKind.Object)
        {
            address = new PostalAddress(
                StreetAddress: ReadString(sj, "ulica"),
                PostalCode: ReadString(sj, "postanski_broj"),
                City: ReadString(sj, "mjesto"),
                Municipality: null,
                Country: "HR");
        }
        return new CompanyResponse(
            OrganizationNumber: oib,
            OrganizationName: name!,
            CompanyType: form ?? "UKJENT",
            LanguageForm: "Unknown",
            BusinessAddress: address,
            PostalAddress: address);
    }

    private static string? ReadString(JsonElement el, string p) =>
        el.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
