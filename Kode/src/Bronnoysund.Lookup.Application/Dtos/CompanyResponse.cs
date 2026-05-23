// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Dtos;

/// <summary>
/// Engelsk-felt respons for org.nr-oppslag, jf. opprinnelig oppgavekrav.
/// </summary>
public sealed record CompanyResponse(
    string OrganizationNumber,
    string OrganizationName,
    string CompanyType,
    string LanguageForm
);
