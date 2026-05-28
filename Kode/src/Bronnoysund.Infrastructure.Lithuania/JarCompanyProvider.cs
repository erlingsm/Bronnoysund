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

namespace Bronnoysund.Infrastructure.Lithuania;

/// <summary>
/// Adapter for <see cref="ICompanyProvider"/> backed by the data.gov.lt JAR dataset.
/// data.gov.lt exposes per-record queries via <c>get.data.gov.lt/{dataset-id}/{record-id}</c>
/// when the dataset has the GET API enabled; we use the company code as the record key.
/// For a future high-volume use case the recommended path is a bulk-import job into a
/// local store — see Plan 21/Lithuania.md for the design.
/// </summary>
internal sealed class JarCompanyProvider(
    HttpClient http,
    IOptions<LithuaniaOptions> options,
    ILogger<JarCompanyProvider> logger) : ICompanyProvider
{
    public string CountryCode => "LT";

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not LithuanianCompanyCode lt)
        {
            return new CompanyLookupResult.InvalidInput(
                $"JarCompanyProvider only accepts Lithuanian company codes (got {id.CountryCode}:{id.Value}).");
        }

        return await ProviderExceptionTranslator.CatchUpstreamAsync(async () =>
        {
            var opts = options.Value;
            var url = $"{opts.JarDatasetId}/?company_code={lt.Value}&limit(1)";
            using var res = await http.GetAsync(new Uri(url, UriKind.Relative), ct).ConfigureAwait(false);
            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                return new CompanyLookupResult.NotFound(lt.Value);
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            JsonElement record;
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                record = doc.RootElement[0];
            }
            // Code-review-2026-05-29: guard ValueKind before GetArrayLength — _data could
            // be an object or null in an error envelope.
            else if (doc.RootElement.TryGetProperty("_data", out var data) &&
                     data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
            {
                record = data[0];
            }
            else
            {
                return new CompanyLookupResult.NotFound(lt.Value);
            }

            var mapped = MapToResponse(record, lt.Value);
            return mapped is null
                ? new CompanyLookupResult.NotFound(lt.Value)
                : new CompanyLookupResult.Found(mapped);
        }, "Lithuania (data.gov.lt)", lt.Value, logger, ct).ConfigureAwait(false);
    }

    private static CompanyResponse? MapToResponse(JsonElement record, string code)
    {
        var name = ReadString(record, "name") ?? ReadString(record, "company_name") ?? ReadString(record, "pavadinimas");
        if (string.IsNullOrWhiteSpace(name)) return null;
        var form = ReadString(record, "legal_form") ?? ReadString(record, "teisine_forma");
        var status = ReadString(record, "status") ?? ReadString(record, "busena");
        var street = ReadString(record, "address") ?? ReadString(record, "adresas");
        var city = ReadString(record, "city") ?? ReadString(record, "miestas");
        return new CompanyResponse(
            OrganizationNumber: code,
            OrganizationName: name!,
            CompanyType: form ?? "UKJENT",
            LanguageForm: "Unknown",
            BusinessAddress: string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city)
                ? null
                : new PostalAddress(street, null, city, null, "LT"),
            PostalAddress: null,
            IsBankrupt: string.Equals(status, "bankrutavusi", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ReadString(JsonElement el, string p) =>
        el.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
