// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Diagnostics.CodeAnalysis;

namespace Bronnoysund.Domain;

/// <summary>
/// Finnish Y-tunnus (Business ID) as a DDD value object. Self-validating: an existing
/// instance is always a normalised 7-digit + dash + check-digit string (NNNNNNN-N) that
/// passes the MOD11 check.
/// </summary>
/// <remarks>
/// MOD11 algorithm per PRH's published validation rule:
/// https://www.vero.fi/en/businesses-and-corporations/about-corporate-taxes/business_id_and_y_tunnus/
/// Weights [7,9,10,5,8,4,2] are applied to digits 1-7 from the left, sum mod 11 gives the check digit (digit 8) = 11 - remainder.
/// If remainder = 0 -> check digit = 0. If remainder = 1 -> the Business ID is invalid (the check digit would have been 10).
/// </remarks>
public sealed record FinnishBusinessId : CompanyIdentifier
{
    private static readonly int[] Mod11Weights = [7, 9, 10, 5, 8, 4, 2];

    private FinnishBusinessId(string value) : base("FI", value) { }

    /// <summary>
    /// Try to build a Finnish Business ID. Accepts both 8-digit ("01120389") and dashed
    /// ("0112038-9") forms; the normalised value always carries the dash. Returns false on
    /// invalid input and sets an error description.
    /// </summary>
    public static bool TryCreate(string? raw, [NotNullWhen(true)] out FinnishBusinessId? value, out string? error)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Finnish Business ID cannot be empty.";
            return false;
        }

        var digits = StripToDigits(raw);

        if (digits.Length != 8)
        {
            error = $"Finnish Business ID must be exactly 8 digits (got {digits.Length}).";
            return false;
        }

        if (!IsValidMod11(digits))
        {
            error = "Finnish Business ID has an invalid MOD11 check digit.";
            return false;
        }

        value = new FinnishBusinessId($"{digits[..7]}-{digits[7]}");
        error = null;
        return true;
    }

    /// <summary>Build a Finnish Business ID or throw <see cref="ArgumentException"/> on invalid input.</summary>
    public static FinnishBusinessId Create(string raw)
    {
        if (!TryCreate(raw, out var value, out var error))
        {
            throw new ArgumentException(error, nameof(raw));
        }
        return value;
    }

    /// <summary>The 8-digit form without the dash — the PRH API accepts both, but some
    /// query-parameter contexts prefer the unpunctuated variant.</summary>
    public string DigitsOnly => Value.Replace("-", string.Empty);

    private static string StripToDigits(string raw)
    {
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsDigit(c))
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static bool IsValidMod11(string eightDigits)
    {
        var sum = 0;
        for (var i = 0; i < 7; i++)
        {
            sum += (eightDigits[i] - '0') * Mod11Weights[i];
        }
        var remainder = sum % 11;
        if (remainder == 1)
        {
            return false;
        }
        var expectedCheckDigit = remainder == 0 ? 0 : 11 - remainder;
        var actualCheckDigit = eightDigits[7] - '0';
        return expectedCheckDigit == actualCheckDigit;
    }

    public override string ToString() => Value;
}
