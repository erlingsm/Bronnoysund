// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;

namespace Bronnoysund.Application.Results;

/// <summary>
/// Discriminated union for matrikkelenhet (cadastral) lookups. Brreg's endpoint accepts
/// either a <c>matrikkelenhetid</c> or a <c>matrikkelnummer</c>; supplying neither (or both)
/// is an <see cref="InvalidInput"/>. Empty result list is <see cref="NotFound"/> — distinct
/// from <see cref="Unavailable"/> (technical failure).
/// </summary>
public abstract record MatrikkelenhetLookupResult
{
    private MatrikkelenhetLookupResult() { }

    public sealed record Found(IReadOnlyList<MatrikkelenhetResponse> Matrikkelenheter) : MatrikkelenhetLookupResult;

    public sealed record NotFound(string Query) : MatrikkelenhetLookupResult;

    public sealed record InvalidInput(string Message) : MatrikkelenhetLookupResult;

    public sealed record Unavailable(string Message) : MatrikkelenhetLookupResult;
}
