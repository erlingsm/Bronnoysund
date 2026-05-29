// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Text.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.International;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Ireland;

/// <summary>
/// Adapter for <see cref="ICompanyProvider"/> backed by the CRO CKAN datastore. The
/// Companies dataset is published daily, flagged as a High Value Dataset under EU
/// 2019/1024, and freely accessible without authentication. CKAN's datastore_search
/// endpoint returns the matching CSV row as JSON. Field names (company_num, company_name,
/// company_status, ...) match what the live API surfaces today.
/// </summary>
internal sealed class CroCompanyProvider(
    HttpClient http,
    IOptions<IrelandOptions> options,
    ILogger<CroCompanyProvider> logger) : ICompanyProvider
{
    public string CountryCode => "IE";

    // CRO's CKAN datastore is an open API (CC-BY 4.0) with no auth requirement.
    public bool IsConfigured => true;

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not IrishCroNumber ie)
        {
            return new CompanyLookupResult.InvalidInput(
                $"CroCompanyProvider only accepts Irish CRO numbers (got {id.CountryCode}:{id.Value}).");
        }

        return await ProviderExceptionTranslator.CatchUpstreamAsync(async () =>
        {
            var opts = options.Value;
            // CKAN datastore_search takes filters as URL-encoded JSON. company_num is the
            // numeric field on CRO's resource, so we send the value as a JSON number.
            var filters = $"{{\"company_num\":{ie.Value}}}";
            var url = $"api/3/action/datastore_search?resource_id={Uri.EscapeDataString(opts.CompaniesResourceId)}&filters={Uri.EscapeDataString(filters)}&limit=1";
            using var res = await http.GetAsync(new Uri(url, UriKind.Relative), ct).ConfigureAwait(false);
            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                return new CompanyLookupResult.NotFound(ie.Value);
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            // CKAN error responses sometimes return "success": "false" (string) or omit
            // it entirely on auth errors. ValueKind guard avoids InvalidOperationException
            // from GetBoolean(). Code-review-2026-05-29-iter2 should-fix.
            if (!doc.RootElement.TryGetProperty("success", out var success) ||
                success.ValueKind != JsonValueKind.True)
            {
                return new CompanyLookupResult.Unavailable("CRO CKAN response was not successful.");
            }
            if (!doc.RootElement.TryGetProperty("result", out var result) ||
                !result.TryGetProperty("records", out var records) ||
                records.ValueKind != JsonValueKind.Array ||
                records.GetArrayLength() == 0)
            {
                return new CompanyLookupResult.NotFound(ie.Value);
            }

            var mapped = MapToResponse(records[0], ie.Value);
            return mapped is null
                ? new CompanyLookupResult.NotFound(ie.Value)
                : new CompanyLookupResult.Found(mapped);
        }, "Ireland (CRO)", ie.Value, logger, ct).ConfigureAwait(false);
    }

    private static CompanyResponse? MapToResponse(JsonElement record, string companyNumber)
    {
        // Code-review-2026-05-29: return null on missing name so the caller maps to
        // NotFound, matching every other adapter's contract. The earlier behaviour
        // (return Found with empty OrganizationName) surfaced as a "blank result"
        // bug rather than a clean miss.
        var name = ReadString(record, "company_name");
        if (string.IsNullOrWhiteSpace(name)) return null;

        var address = ComposeAddress(record);
        var registeredDate = ParseDate(ReadString(record, "company_reg_date"));
        return new CompanyResponse(
            OrganizationNumber: companyNumber,
            OrganizationName: name,
            CompanyType: ReadString(record, "company_type") ?? ReadString(record, "company_type_code") ?? "UKJENT",
            LanguageForm: "Unknown",
            BusinessAddress: address,
            PostalAddress: address,
            RegisteredDate: registeredDate,
            FoundingDate: registeredDate);
    }

    private static PostalAddress? ComposeAddress(JsonElement record)
    {
        var lines = new[]
        {
            ReadString(record, "company_address_1"),
            ReadString(record, "company_address_2"),
            ReadString(record, "company_address_3"),
            ReadString(record, "company_address_4"),
        }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (lines.Length == 0) return null;
        return new PostalAddress(
            StreetAddress: string.Join(", ", lines),
            PostalCode: ReadString(record, "eircode"),
            City: null,
            Municipality: null,
            Country: "IE");
    }

    private static string? ReadString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            _ => null,
        };
    }

    private static DateOnly? ParseDate(string? raw) =>
        DateTime.TryParse(raw, out var dt) ? DateOnly.FromDateTime(dt) : null;
}
