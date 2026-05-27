// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Provider for Brreg kodeverk-endepunkter (Organisasjonsformer, Kommuner, Rolletyper, etc.).
/// These change rarely so callers should cache aggressively (24h+ TTL). Plan 54's Browse-page
/// is the primary consumer.
/// </summary>
public interface IKodeverkProvider
{
    Task<IReadOnlyList<KodeverkEntry>> GetOrganisasjonsformerAsync(CancellationToken ct);
}

/// <summary>
/// One row in a kodeverk-table. The shape matches Brreg's standard kodeverk-respons:
/// short code + human-readable description, optionally marked as retired.
/// </summary>
public sealed record KodeverkEntry(string Code, string Description, bool IsRetired = false);
