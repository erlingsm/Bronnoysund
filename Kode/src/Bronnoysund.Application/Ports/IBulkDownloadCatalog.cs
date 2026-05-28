// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Catalogue of Brreg bulk-download URLs. The list is static (curated against the Brreg
/// OpenAPI spec) and lives in Infrastructure so the WebApi can serve it without touching
/// the upstream registers. Updating the list is a code change tied to spec regeneration.
/// </summary>
public interface IBulkDownloadCatalog
{
    IReadOnlyList<BulkDownload> GetAll();
}
