// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Dtos;

/// <summary>
/// One matrikkelenhet (cadastral unit) row from Brreg's
/// <c>/enhetsregisteret/api/matrikkelenhet</c> endpoint. A matrikkelnummer can resolve
/// to several matrikkelenheter (e.g. when leases are registered separately), so the
/// adapter returns a list.
/// </summary>
public sealed record MatrikkelenhetResponse(
    string MatrikkelenhetId,
    string OrganizationNumber,
    string? KommuneNumber,
    string? GardsNumber,
    string? BruksNumber,
    string? FesteNumber,
    int? Order);
