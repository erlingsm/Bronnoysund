// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Dtos;

/// <summary>
/// Response from <c>/frivillighetsregisteret/api/frivillige-organisasjoner/{orgnr}</c>.
/// Carries only the fields the UI needs to render a "frivillig organisasjon"-badge on
/// the Lookup page (status, første gang innført, kategori, Grasrotandel-deltakelse).
/// </summary>
public sealed record VoluntaryOrganizationResponse(
    string OrganizationNumber,
    string Status,
    DateOnly? FirstRegisteredDate,
    DateOnly? RegisteredDate,
    string? PrimaryIcnpoCategoryNumber,
    string? PrimaryIcnpoCategoryName,
    bool ParticipatesInGrasrotandel,
    string? AccountNumber);
