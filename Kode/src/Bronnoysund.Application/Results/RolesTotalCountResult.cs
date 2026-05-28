// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Results;

/// <summary>
/// Discriminated union for <c>/enhetsregisteret/api/roller/totalbestand</c>. Brreg returns
/// a single integer representing the total number of person-roles across all entities in
/// Enhetsregisteret — useful as a "Brreg statistics" tile.
/// </summary>
public abstract record RolesTotalCountResult
{
    private RolesTotalCountResult() { }

    public sealed record Found(long TotalCount) : RolesTotalCountResult;

    public sealed record Unavailable(string Message) : RolesTotalCountResult;
}
