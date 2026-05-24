// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text;

namespace Bronnoysund.Lookup.Speech.Parsing;

/// <summary>
/// Konverterer norsk talt tall-tekst (siffer-for-siffer) til en streng av siffer.
/// Eksempel: "ni en ni tre null null tre åtte åtte" → "919300388".
/// Støtter engelske tall-ord også (one/two/...) som backup om talegjenkjenning bytter språk.
/// </summary>
public static class NorskTallParser
{
    private static readonly IReadOnlyDictionary<string, char> WordToDigit =
        new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase)
        {
            ["null"] = '0', ["zero"] = '0',
            ["en"] = '1', ["ett"] = '1', ["one"] = '1',
            ["to"] = '2', ["two"] = '2',
            ["tre"] = '3', ["three"] = '3',
            ["fire"] = '4', ["four"] = '4',
            ["fem"] = '5', ["five"] = '5',
            ["seks"] = '6', ["six"] = '6',
            ["sju"] = '7', ["syv"] = '7', ["seven"] = '7',
            ["åtte"] = '8', ["otte"] = '8', ["eight"] = '8',
            ["ni"] = '9', ["nine"] = '9',
        };

    /// <summary>
    /// Forsøk å parse en streng som siffer-for-siffer-uttale. Returnerer true hvis alle tokens
    /// kunne mappes til siffer, og setter <paramref name="digits"/> til den sammensatte strengen.
    /// Tomme strenger og bare-tall-strenger godtas direkte.
    /// </summary>
    public static bool TryParseDigits(string input, out string digits)
    {
        digits = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var sb = new StringBuilder();
        var separators = new[] { ' ', '\t', '\n', '-', ',', '.', ' ' };
        foreach (var raw in input.Split(separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var token = raw.Trim();
            if (token.Length == 0)
            {
                continue;
            }

            // Hvis tokenet allerede er en sekvens av siffer, ta dem som-er.
            if (token.All(char.IsDigit))
            {
                sb.Append(token);
                continue;
            }

            if (WordToDigit.TryGetValue(token, out var d))
            {
                sb.Append(d);
                continue;
            }

            // Ukjent ord — kan ikke tolkes som siffer-tale.
            return false;
        }

        digits = sb.ToString();
        return digits.Length > 0;
    }
}
