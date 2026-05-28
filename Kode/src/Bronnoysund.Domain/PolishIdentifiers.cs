// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Diagnostics.CodeAnalysis;

namespace Bronnoysund.Domain;

/// <summary>
/// Polish KRS — National Court Register number for legal entities (companies, associations,
/// foundations). 10 digits, sequence id with leading zeros, no checksum.
/// </summary>
public sealed record PolishKrsNumber : CompanyIdentifier
{
    private PolishKrsNumber(string value) : base("PL", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out PolishKrsNumber? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Polish KRS number cannot be empty.";
            return false;
        }
        var trimmed = new string(raw.Where(char.IsDigit).ToArray());
        if (trimmed.Length != 10)
        {
            error = "Polish KRS number must be 10 digits.";
            return false;
        }
        value = new PolishKrsNumber(trimmed);
        error = null;
        return true;
    }

    public static PolishKrsNumber Create(string raw)
    {
        if (!TryCreate(raw, out var value, out var error)) throw new ArgumentException(error, nameof(raw));
        return value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Polish NIP — tax / VAT identification number for all entities. 10 digits with MOD-11
/// checksum on the first 9 (weights [6,5,7,2,3,4,5,6,7]). Distinguished from KRS by the
/// absence of leading zeros (NIPs never start with 0000).
/// </summary>
public sealed record PolishNip : CompanyIdentifier
{
    private static readonly int[] Mod11Weights = [6, 5, 7, 2, 3, 4, 5, 6, 7];

    private PolishNip(string value) : base("PL", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out PolishNip? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Polish NIP cannot be empty.";
            return false;
        }
        var trimmed = new string(raw.Where(char.IsDigit).ToArray());
        if (trimmed.Length != 10)
        {
            error = "Polish NIP must be 10 digits.";
            return false;
        }

        var digits = trimmed.Select(c => c - '0').ToArray();
        var sum = 0;
        for (var i = 0; i < 9; i++) sum += digits[i] * Mod11Weights[i];
        var check = sum % 11;
        if (check == 10 || check != digits[9])
        {
            error = "Polish NIP has an invalid MOD-11 checksum.";
            return false;
        }
        value = new PolishNip(trimmed);
        error = null;
        return true;
    }

    public static PolishNip Create(string raw)
    {
        if (!TryCreate(raw, out var value, out var error)) throw new ArgumentException(error, nameof(raw));
        return value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Polish REGON — statistical identification number. 9 digits with MOD-11 checksum
/// (weights [8,9,2,3,4,5,6,7]) for companies, or 14 digits (with extra final-check) for
/// branches. The 9-digit form clashes with Norwegian organisation numbers on length, so
/// the country detector treats it as the lowest-priority Polish pattern.
/// </summary>
public sealed record PolishRegon : CompanyIdentifier
{
    private static readonly int[] Weights9 = [8, 9, 2, 3, 4, 5, 6, 7];
    private static readonly int[] Weights14 = [2, 4, 8, 5, 0, 9, 7, 3, 6, 1, 2, 4, 8];

    private PolishRegon(string value) : base("PL", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out PolishRegon? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Polish REGON cannot be empty.";
            return false;
        }
        var trimmed = new string(raw.Where(char.IsDigit).ToArray());
        if (trimmed.Length != 9 && trimmed.Length != 14)
        {
            error = "Polish REGON must be 9 or 14 digits.";
            return false;
        }
        var digits = trimmed.Select(c => c - '0').ToArray();
        if (!IsValid9(digits))
        {
            error = "Polish REGON has an invalid MOD-11 checksum.";
            return false;
        }
        if (digits.Length == 14 && !IsValid14(digits))
        {
            error = "Polish 14-digit REGON has an invalid secondary checksum.";
            return false;
        }
        value = new PolishRegon(trimmed);
        error = null;
        return true;
    }

    public static PolishRegon Create(string raw)
    {
        if (!TryCreate(raw, out var value, out var error)) throw new ArgumentException(error, nameof(raw));
        return value;
    }

    private static bool IsValid9(int[] digits)
    {
        var sum = 0;
        for (var i = 0; i < 8; i++) sum += digits[i] * Weights9[i];
        var check = sum % 11;
        if (check == 10) check = 0;
        return check == digits[8];
    }

    private static bool IsValid14(int[] digits)
    {
        var sum = 0;
        for (var i = 0; i < 13; i++) sum += digits[i] * Weights14[i];
        var check = sum % 11;
        if (check == 10) check = 0;
        return check == digits[13];
    }

    public override string ToString() => Value;
}
