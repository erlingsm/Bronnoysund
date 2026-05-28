// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Diagnostics.CodeAnalysis;

namespace Bronnoysund.Domain;

/// <summary>
/// Croatian OIB — 11 digits with ISO 7064 MOD 11,10 checksum. Universal tax / personal /
/// company identifier introduced 2009.
/// </summary>
public sealed record CroatianOib : CompanyIdentifier
{
    private CroatianOib(string value) : base("HR", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out CroatianOib? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Croatian OIB cannot be empty."; return false; }
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) { error = "Croatian OIB must be 11 digits."; return false; }
        var r = 10;
        for (var i = 0; i < 10; i++)
        {
            r = (r + (digits[i] - '0')) % 10;
            if (r == 0) r = 10;
            r = (r * 2) % 11;
        }
        var check = (11 - r) % 10;
        if (check != digits[10] - '0') { error = "Croatian OIB has an invalid ISO 7064 MOD 11,10 checksum."; return false; }
        value = new CroatianOib(digits);
        error = null;
        return true;
    }

    public static CroatianOib Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Greek ΑΦΜ (AFM / VAT number) — 9 digits with weighted MOD-11 (powers of two:
/// [256,128,64,32,16,8,4,2]). Tolerant of optional "EL" or "GR" prefix.
/// </summary>
public sealed record GreekVatNumber : CompanyIdentifier
{
    private GreekVatNumber(string value) : base("GR", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out GreekVatNumber? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Greek AFM cannot be empty."; return false; }
        var s = raw.Trim().ToUpperInvariant();
        if (s.StartsWith("EL", StringComparison.Ordinal) || s.StartsWith("GR", StringComparison.Ordinal)) s = s[2..];
        var digits = new string(s.Where(char.IsDigit).ToArray());
        if (digits.Length != 9) { error = "Greek AFM must be 9 digits."; return false; }
        var sum = 0;
        for (var i = 0; i < 8; i++) sum += (digits[i] - '0') << (8 - i);
        var check = (sum % 11) % 10;
        if (check != digits[8] - '0') { error = "Greek AFM has an invalid MOD-11 checksum."; return false; }
        value = new GreekVatNumber(digits);
        error = null;
        return true;
    }

    public static GreekVatNumber Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Greek ΓΕΜΗ (GEMI commercial registry number) — 12 digits, no published checksum.
/// Leading zeros are significant (Eurobank Holdings has 000223001000).
/// </summary>
public sealed record GreekGemiNumber : CompanyIdentifier
{
    private GreekGemiNumber(string value) : base("GR", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out GreekGemiNumber? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Greek GEMI number cannot be empty."; return false; }
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 12) { error = "Greek GEMI number must be 12 digits."; return false; }
        value = new GreekGemiNumber(digits);
        error = null;
        return true;
    }

    public static GreekGemiNumber Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Latvian registration number — 11 digits starting with 4 or 5. No published checksum;
/// format-only validation. Tolerates optional "LV" VAT prefix.
/// </summary>
public sealed record LatvianRegistrationNumber : CompanyIdentifier
{
    private LatvianRegistrationNumber(string value) : base("LV", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out LatvianRegistrationNumber? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Latvian registration number cannot be empty."; return false; }
        var s = raw.Trim().ToUpperInvariant();
        if (s.StartsWith("LV", StringComparison.Ordinal)) s = s[2..];
        var digits = new string(s.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) { error = "Latvian registration number must be 11 digits."; return false; }
        if (digits[0] is not ('4' or '5')) { error = "Latvian registration number must start with 4 or 5."; return false; }
        value = new LatvianRegistrationNumber(digits);
        error = null;
        return true;
    }

    public static LatvianRegistrationNumber Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public override string ToString() => Value;
}
