// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Dtos;

/// <summary>
/// Detailed response for a single underenhet (sub-unit) lookup. Mirrors the shape of
/// <see cref="CompanyResponse"/> for the fields that exist on underenheter; the Brreg
/// underenhet endpoint exposes its own subset (no Konkurs/Sektorkode/Stiftelsesdato).
/// </summary>
public sealed record SubUnitDetailsResponse(
    string OrganizationNumber,
    string OrganizationName,
    string? ParentOrganizationNumber,
    string? Website = null,
    string? Email = null,
    string? Phone = null,
    string? MobilePhone = null,
    PostalAddress? BusinessAddress = null,
    PostalAddress? PostalAddress = null,
    IndustryCode? PrimaryIndustry = null,
    int? EmployeeCount = null,
    DateOnly? StartDate = null,
    DateOnly? ClosedDate = null,
    DateOnly? RegisteredDate = null,
    bool? RegisteredInVatRegistry = null);
