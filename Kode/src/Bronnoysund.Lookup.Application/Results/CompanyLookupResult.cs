// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Dtos;

namespace Bronnoysund.Lookup.Application.Results;

/// <summary>
/// Type-safe diskriminert union for resultatet av et selskaps-oppslag. Unngår exceptions for
/// forretningsfeil (404, valideringsfeil) — kun tekniske feil kastes som exceptions.
/// </summary>
public abstract record CompanyLookupResult
{
    private CompanyLookupResult() { }

    public sealed record Found(CompanyResponse Company) : CompanyLookupResult;

    public sealed record NotFound(string OrganizationNumber) : CompanyLookupResult;

    public sealed record InvalidInput(string Message) : CompanyLookupResult;

    public sealed record Unavailable(string Message) : CompanyLookupResult;
}
