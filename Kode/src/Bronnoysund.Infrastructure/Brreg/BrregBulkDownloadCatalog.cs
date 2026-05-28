// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Static catalogue of Brreg's bulk-download URLs. We do not proxy the bytes — these
/// dumps can be tens of MB and Brreg serves them with stable URLs. The URLs are
/// hand-curated against Brreg's OpenAPI spec at <c>/dokumentasjon/no/openapi.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// Plan 53's nightly spec-drift CI verifies the Kiota-generated client against the live
/// Brreg spec. It will catch field renames or endpoint removals because the Kiota
/// request-builder tree changes. **It does not directly verify the URLs in this
/// catalogue** — they are constructed as strings, not via Kiota's request builders, so
/// a 301-redirect or a silent URL change at Brreg would not fail the drift CI.
/// </para>
/// <para>
/// When regenerating the Kiota client, cross-reference each entry below against the
/// new request-builder tree (Enheter/Lastned/*, Underenheter/Lastned/*, etc.) and
/// update URLs that no longer match. Consider migrating to Kiota request-builder-derived
/// URLs in a future iteration to eliminate the drift gap.
/// </para>
/// </remarks>
internal sealed class BrregBulkDownloadCatalog : IBulkDownloadCatalog
{
    private const string BaseUrl = "https://data.brreg.no";

    private static readonly IReadOnlyList<BulkDownload> Entries =
    [
        new BulkDownload(
            Title: "Alle enheter (Enhetsregisteret)",
            Description: "Full bulk dump of all entities in the main register, gzip-compressed JSON.",
            Register: "Enhetsregisteret",
            Format: "application/gzip+json",
            Url: $"{BaseUrl}/enhetsregisteret/api/enheter/lastned",
            Compressed: true),

        new BulkDownload(
            Title: "Alle enheter (Excel)",
            Description: "Full bulk dump of all entities as an Excel spreadsheet.",
            Register: "Enhetsregisteret",
            Format: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Url: $"{BaseUrl}/enhetsregisteret/api/enheter/lastned/regneark",
            Compressed: false),

        new BulkDownload(
            Title: "Alle enheter (CSV)",
            Description: "Full bulk dump of all entities as a CSV file (RFC 4180 escaping).",
            Register: "Enhetsregisteret",
            Format: "text/csv",
            Url: $"{BaseUrl}/enhetsregisteret/api/enheter/lastned/csv-escaped",
            Compressed: false),

        new BulkDownload(
            Title: "Alle underenheter (Enhetsregisteret)",
            Description: "Full bulk dump of all sub-units, gzip-compressed JSON.",
            Register: "Enhetsregisteret",
            Format: "application/gzip+json",
            Url: $"{BaseUrl}/enhetsregisteret/api/underenheter/lastned",
            Compressed: true),

        new BulkDownload(
            Title: "Alle underenheter (Excel)",
            Description: "Full bulk dump of all sub-units as an Excel spreadsheet.",
            Register: "Enhetsregisteret",
            Format: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Url: $"{BaseUrl}/enhetsregisteret/api/underenheter/lastned/regneark",
            Compressed: false),

        new BulkDownload(
            Title: "Alle underenheter (CSV)",
            Description: "Full bulk dump of all sub-units as a CSV file (RFC 4180 escaping).",
            Register: "Enhetsregisteret",
            Format: "text/csv",
            Url: $"{BaseUrl}/enhetsregisteret/api/underenheter/lastned/csv-escaped",
            Compressed: false),

        new BulkDownload(
            Title: "Alle frivillige organisasjoner (CSV)",
            Description: "Full bulk dump of all entries in Frivillighetsregisteret.",
            Register: "Frivillighetsregisteret",
            Format: "text/csv",
            Url: $"{BaseUrl}/frivillighetsregisteret/api/frivillige-organisasjoner/totalbestand/csv-escaped",
            Compressed: false),

        new BulkDownload(
            Title: "Alle partier (CSV)",
            Description: "Full bulk dump of all entries in Partiregisteret (the only callable endpoint Partiregisteret exposes).",
            Register: "Partiregisteret",
            Format: "text/csv",
            Url: $"{BaseUrl}/partiregisteret/api/lastned/csv-escaped",
            Compressed: false),
    ];

    public IReadOnlyList<BulkDownload> GetAll() => Entries;
}
