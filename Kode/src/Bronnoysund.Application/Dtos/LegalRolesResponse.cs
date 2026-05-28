// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Dtos;

/// <summary>
/// Response from <c>/roller/enheter/{orgnr}/juridiskeroller</c> — the set of OTHER
/// entities where the subject organization holds a legal role (e.g. partner,
/// accountant, auditor). Distinct from <see cref="RolesResponse"/> (Application.Ports),
/// which lists person-roles INSIDE the subject entity.
/// </summary>
public sealed record LegalRolesResponse(
    string OrganizationNumber,
    bool IsDeleted,
    IReadOnlyList<LegalRoleHolding> Holdings);

/// <summary>One company where the subject has at least one legal role.</summary>
public sealed record LegalRoleHolding(
    string OrganizationNumber,
    string Name,
    IReadOnlyList<LegalRoleAssignment> Roles);

/// <summary>A single role the subject holds in the target company.</summary>
public sealed record LegalRoleAssignment(
    string TypeCode,
    string TypeDescription,
    bool IsResigned,
    bool IsDeregistered,
    int? Order);
