// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Text.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.OpenCorporates;

/// <summary>
/// Shared OpenCorporates client. Each per-country provider passes its jurisdiction code
/// (e.g. "es", "it", "rs") plus the company identifier. The response envelope is
/// <c>{"results":{"company":{...}}}</c> with a uniform field shape across jurisdictions.
/// </summary>
internal sealed class OpenCorporatesClient(
    HttpClient http,
    IOptions<OpenCorporatesOptions> options,
    ILogger<OpenCorporatesClient> logger)
{
    public async Task<CompanyLookupResult> LookupAsync(string jurisdiction, string companyId, string countryCode, CancellationToken ct)
    {
        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            return new CompanyLookupResult.Unavailable(
                "OpenCorporates API token not configured — set Bronnoysund:International:OpenCorporates:ApiToken in Azure App Config.");
        }

        try
        {
            var url = $"v0.4/companies/{jurisdiction}/{Uri.EscapeDataString(companyId)}?api_token={Uri.EscapeDataString(opts.ApiToken)}";
            using var res = await http.GetAsync(new Uri(url, UriKind.Relative), ct).ConfigureAwait(false);
            if (res.StatusCode == HttpStatusCode.NotFound) return new CompanyLookupResult.NotFound(companyId);
            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or (HttpStatusCode)402)
            {
                return new CompanyLookupResult.Unavailable("OpenCorporates rejected the request — API key invalid or quota exhausted.");
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("results", out var results) ||
                !results.TryGetProperty("company", out var company))
            {
                return new CompanyLookupResult.NotFound(companyId);
            }
            var mapped = MapToResponse(company, companyId, countryCode);
            return mapped is null
                ? new CompanyLookupResult.NotFound(companyId)
                : new CompanyLookupResult.Found(mapped);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OpenCorporates transport error for {Jurisdiction}/{Id}", jurisdiction, companyId);
            return new CompanyLookupResult.Unavailable($"Could not contact OpenCorporates for {jurisdiction}/{companyId}: {ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new CompanyLookupResult.Unavailable($"OpenCorporates did not respond within timeout for {jurisdiction}/{companyId}");
        }
    }

    private static CompanyResponse? MapToResponse(JsonElement company, string companyId, string countryCode)
    {
        var name = ReadString(company, "name");
        if (string.IsNullOrWhiteSpace(name)) return null;
        var form = ReadString(company, "company_type");
        var status = ReadString(company, "current_status");
        var inactive = company.TryGetProperty("inactive", out var ina) && ina.ValueKind == JsonValueKind.True;
        PostalAddress? address = null;
        if (company.TryGetProperty("registered_address_in_full", out var addrV) && addrV.ValueKind == JsonValueKind.String)
        {
            address = new PostalAddress(addrV.GetString(), null, null, null, countryCode);
        }
        return new CompanyResponse(
            OrganizationNumber: ReadString(company, "company_number") ?? companyId,
            OrganizationName: name!,
            CompanyType: form ?? "UKJENT",
            LanguageForm: "Unknown",
            BusinessAddress: address,
            PostalAddress: address,
            RegisteredDate: ParseDate(ReadString(company, "incorporation_date")),
            DeletedDate: inactive ? ParseDate(ReadString(company, "dissolution_date")) : null,
            IsBankrupt: status?.Contains("bankrupt", StringComparison.OrdinalIgnoreCase) == true ||
                        status?.Contains("liquidation", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string? ReadString(JsonElement el, string p) =>
        el.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static DateOnly? ParseDate(string? raw) =>
        DateTime.TryParse(raw, out var dt) ? DateOnly.FromDateTime(dt) : null;
}
