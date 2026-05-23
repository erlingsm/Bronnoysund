// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Domain;

/// <summary>
/// Norsk målform (skriftspråk) en virksomhet er registrert med, jf. Brreg-feltet "maalform".
/// </summary>
public enum LanguageForm
{
    Unknown = 0,
    Bokmål = 1,
    Nynorsk = 2,
}
