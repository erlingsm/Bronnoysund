// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Dtos;

/// <summary>
/// Metadata about a Brreg bulk-download endpoint. We intentionally do not proxy the bytes
/// — these files can be tens of megabytes and Brreg already serves them with CDN
/// behaviour. Clients use the <see cref="Url"/> to download directly from Brreg.
/// </summary>
public sealed record BulkDownload(
    string Title,
    string Description,
    string Register,
    string Format,
    string Url,
    bool Compressed);
