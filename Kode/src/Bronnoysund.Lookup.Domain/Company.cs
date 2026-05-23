// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Domain;

/// <summary>
/// Norsk virksomhet identifisert ved sitt organisasjonsnummer.
/// Domeneentitet med immutable struktur (record). Identitet = <see cref="OrganizationNumber"/>.
/// </summary>
public sealed record Company(
    OrganizationNumber OrganizationNumber,
    string Name,
    string OrganizationFormCode,
    LanguageForm LanguageForm
);
