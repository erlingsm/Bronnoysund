// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;

namespace Bronnoysund.Application.Results;

/// <summary>
/// Discriminated union for legal-roles lookup. Brreg returns the entity's set of
/// legal roles in other companies. NotFound when the subject org has no roles or
/// the org number is unknown; Unavailable for technical failures.
/// </summary>
public abstract record LegalRolesLookupResult
{
    private LegalRolesLookupResult() { }

    public sealed record Found(LegalRolesResponse Roles) : LegalRolesLookupResult;

    public sealed record NotFound(string OrganizationNumber) : LegalRolesLookupResult;

    public sealed record Unavailable(string Message) : LegalRolesLookupResult;
}
