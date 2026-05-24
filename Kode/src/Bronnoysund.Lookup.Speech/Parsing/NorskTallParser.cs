// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text;

namespace Bronnoysund.Lookup.Speech.Parsing;

/// <summary>
/// Converts spoken Norwegian number text (digit-by-digit) to a string of digits.
/// Example: "ni en ni tre null null tre åtte åtte" → "919300388".
/// Also supports English number words (one/two/...) as a backup if speech recognition switches language.
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
    /// Try to parse a string as digit-by-digit pronunciation. Returns true if all tokens
    /// could be mapped to digits, and sets <paramref name="digits"/> to the concatenated string.
    /// Empty strings and digit-only strings are accepted directly.
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

            // If the token is already a sequence of digits, take them as-is.
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

            // Unknown word — cannot be interpreted as digit speech.
            return false;
        }

        digits = sb.ToString();
        return digits.Length > 0;
    }
}
