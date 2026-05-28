// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Text.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Greece;

internal sealed class GemiCompanyProvider(
    HttpClient http,
    IOptions<GreeceOptions> options,
    ILogger<GemiCompanyProvider> logger) : ICompanyProvider
{
    public string CountryCode => "GR";

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            return new CompanyLookupResult.Unavailable(
                "Greece (GEMI) API key not configured — set Bronnoysund:International:Greece:ApiKey in Azure App Config.");
        }

        var (path, lookupValue) = id switch
        {
            GreekVatNumber afm => ($"searchCompany?afm={afm.Value}", afm.Value),
            GreekGemiNumber gemi => ($"showCompany?arGemi={gemi.Value}", gemi.Value),
            _ => (null, null),
        };
        if (path is null || lookupValue is null)
        {
            return new CompanyLookupResult.InvalidInput(
                $"GemiCompanyProvider only accepts Greek AFM or GEMI numbers (got {id.CountryCode}:{id.Value}).");
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
            req.Headers.Add("X-API-Key", opts.ApiKey);
            using var res = await http.SendAsync(req, ct).ConfigureAwait(false);
            if (res.StatusCode == HttpStatusCode.NotFound) return new CompanyLookupResult.NotFound(lookupValue);
            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new CompanyLookupResult.Unavailable("GEMI API key rejected — check Bronnoysund:International:Greece:ApiKey.");
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var mapped = MapToResponse(doc.RootElement, lookupValue);
            return mapped is null
                ? new CompanyLookupResult.NotFound(lookupValue)
                : new CompanyLookupResult.Found(mapped);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "GEMI transport error for {Value}", lookupValue);
            return new CompanyLookupResult.Unavailable($"Could not contact GEMI for {lookupValue}: {ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new CompanyLookupResult.Unavailable($"GEMI did not respond within timeout for {lookupValue}");
        }
    }

    private static CompanyResponse? MapToResponse(JsonElement root, string id)
    {
        var name = ReadString(root, "eponymia") ?? ReadString(root, "name");
        if (string.IsNullOrWhiteSpace(name)) return null;
        return new CompanyResponse(
            OrganizationNumber: ReadString(root, "arGemi") ?? id,
            OrganizationName: name!,
            CompanyType: ReadString(root, "nomikiMorfi") ?? "UKJENT",
            LanguageForm: "Unknown",
            BusinessAddress: ReadString(root, "edra") is { Length: > 0 } edra
                ? new PostalAddress(edra, null, null, null, "GR")
                : null,
            PostalAddress: null);
    }

    private static string? ReadString(JsonElement el, string p) =>
        el.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
