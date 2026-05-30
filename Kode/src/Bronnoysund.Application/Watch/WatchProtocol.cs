// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
//
// Wire-format DTOs for the Watch <-> Phone companion protocol v1.
// Spec: Kode/docs/watch-protocol.md. Lives in Application so that test
// projects + future hosts can ship the same shape without MAUI bindings.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bronnoysund.Application.Watch;

public sealed record LookupRequest(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("value")] string Value);

public sealed record LookupResponse(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("result")] string Result,
    [property: JsonPropertyName("organizationNumber")] string? OrganizationNumber = null,
    [property: JsonPropertyName("organizationName")] string? OrganizationName = null,
    [property: JsonPropertyName("companyType")] string? CompanyType = null,
    [property: JsonPropertyName("languageForm")] string? LanguageForm = null,
    [property: JsonPropertyName("countryCode")] string? CountryCode = null,
    [property: JsonPropertyName("value")] string? Value = null,
    [property: JsonPropertyName("message")] string? Message = null,
    [property: JsonPropertyName("code")] string? Code = null);

public static class WatchProtocol
{
    public const int CurrentVersion = 1;

    public const string AndroidMessagePath = "/com.bronnoysund/lookup/v1";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static byte[] Encode(LookupResponse response) =>
        JsonSerializer.SerializeToUtf8Bytes(response, JsonOptions);

    public static LookupRequest? TryDecode(byte[] payload)
    {
        try
        {
            return JsonSerializer.Deserialize<LookupRequest>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
