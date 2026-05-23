// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Domain;

/// <summary>
/// Norsk organisasjonsnummer som DDD value object. Selv-validerende: en eksisterende
/// instans er alltid et gyldig 9-sifret orgnr som passerer MOD11-kontroll og starter på 8 eller 9.
/// </summary>
/// <remarks>
/// MOD11-formelen er beskrevet av Brønnøysundregistrene:
/// https://www.brreg.no/om-oss/registrene-vare/om-enhetsregisteret/organisasjonsnummeret/
/// Vekter [3,2,7,6,5,4,3,2] anvendes på siffer 1-8 fra venstre, sum mod 11 gir kontrollsiffer (siffer 9) = 11 - rest.
/// Hvis rest = 0 -> kontrollsiffer = 0. Hvis rest = 1 -> orgnr er ugyldig (kontrollsiffer kunne blitt 10).
/// </remarks>
public readonly record struct OrganizationNumber
{
    private static readonly int[] Mod11Weights = [3, 2, 7, 6, 5, 4, 3, 2];

    /// <summary>Normalisert form: 9 siffer uten mellomrom eller separatorer.</summary>
    public string Value { get; }

    private OrganizationNumber(string value) => Value = value;

    /// <summary>
    /// Forsøk å lage et orgnr. Returnerer false ved ugyldig input og setter feilbeskrivelse.
    /// </summary>
    public static bool TryCreate(string? raw, out OrganizationNumber value, out string? error)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Organisasjonsnummer kan ikke være tomt.";
            return false;
        }

        var normalized = Normalize(raw);

        if (normalized.Length != 9)
        {
            error = $"Organisasjonsnummer må være nøyaktig 9 siffer (fikk {normalized.Length}).";
            return false;
        }

        if (!normalized.All(char.IsDigit))
        {
            error = "Organisasjonsnummer kan kun inneholde tall.";
            return false;
        }

        if (normalized[0] != '8' && normalized[0] != '9')
        {
            error = "Organisasjonsnummer må starte med 8 eller 9.";
            return false;
        }

        if (!IsValidMod11(normalized))
        {
            error = "Organisasjonsnummer har ugyldig kontrollsiffer (MOD11).";
            return false;
        }

        value = new OrganizationNumber(normalized);
        error = null;
        return true;
    }

    /// <summary>Lag et orgnr eller kast <see cref="ArgumentException"/> ved ugyldig input.</summary>
    public static OrganizationNumber Create(string raw)
    {
        if (!TryCreate(raw, out var value, out var error))
        {
            throw new ArgumentException(error, nameof(raw));
        }
        return value;
    }

    /// <summary>Fjerner whitespace og vanlige separatorer (mellomrom, bindestrek, punktum).</summary>
    private static string Normalize(string raw)
    {
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsWhiteSpace(c) || c == '-' || c == '.')
            {
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static bool IsValidMod11(string nineDigits)
    {
        var sum = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += (nineDigits[i] - '0') * Mod11Weights[i];
        }
        var remainder = sum % 11;
        if (remainder == 1)
        {
            return false; // Kontrollsiffer ville blitt 10 — ugyldig
        }
        var expectedCheckDigit = remainder == 0 ? 0 : 11 - remainder;
        var actualCheckDigit = nineDigits[8] - '0';
        return expectedCheckDigit == actualCheckDigit;
    }

    public override string ToString() => Value;
}
