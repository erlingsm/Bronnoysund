// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Dtos;

/// <summary>
/// English-field response for organization-number lookups, per the original task requirements.
/// </summary>
public sealed record CompanyResponse(
    string OrganizationNumber,
    string OrganizationName,
    string CompanyType,
    string LanguageForm
);
