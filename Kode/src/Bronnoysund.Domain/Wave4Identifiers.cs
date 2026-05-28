// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Diagnostics.CodeAnalysis;

namespace Bronnoysund.Domain;

/// <summary>
/// Spanish NIF — 9 chars: 1 entity-type letter + 7 digits + 1 control char (letter or
/// digit depending on entity type). The control is computed by a Luhn-like sum over the
/// 7 digits, then either mapped to a letter (via "JABCDEFGHI") or used directly.
/// </summary>
public sealed record SpanishNif : CompanyIdentifier
{
    private const string LetterMap = "JABCDEFGHI";
    private static readonly HashSet<char> EntityTypes = new("ABCDEFGHJNPQRSUVW");
    private static readonly HashSet<char> LetterOnly = new("KPQRSNW");
    private static readonly HashSet<char> DigitOnly = new("ABEH");

    private SpanishNif(string value) : base("ES", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out SpanishNif? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Spanish NIF cannot be empty."; return false; }
        var s = raw.Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        if (s.Length != 9) { error = "Spanish NIF must be 9 characters."; return false; }
        var type = s[0];
        if (!EntityTypes.Contains(type)) { error = $"Unknown Spanish NIF entity-type letter '{type}'."; return false; }
        for (var i = 1; i <= 7; i++)
        {
            if (!char.IsDigit(s[i])) { error = "Spanish NIF positions 2-8 must be digits."; return false; }
        }

        var sum = 0;
        for (var i = 1; i <= 7; i++)
        {
            var d = s[i] - '0';
            if (i % 2 == 1) { d *= 2; if (d > 9) d -= 9; }
            sum += d;
        }
        var control = (10 - sum % 10) % 10;
        var actual = s[8];
        var ok = LetterOnly.Contains(type)
            ? actual == LetterMap[control]
            : DigitOnly.Contains(type)
                ? actual - '0' == control
                : actual - '0' == control || actual == LetterMap[control];
        if (!ok) { error = "Spanish NIF control character is invalid."; return false; }
        value = new SpanishNif(s);
        error = null;
        return true;
    }

    public static SpanishNif Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Italian Codice Fiscale for legal entities = Partita IVA. 11 digits with Luhn (MOD-10)
/// checksum. Tolerates optional "IT" VAT prefix.
/// </summary>
public sealed record ItalianFiscalCode : CompanyIdentifier
{
    private ItalianFiscalCode(string value) : base("IT", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out ItalianFiscalCode? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Italian P.IVA cannot be empty."; return false; }
        var s = raw.Trim().ToUpperInvariant();
        if (s.StartsWith("IT", StringComparison.Ordinal)) s = s[2..];
        var digits = new string(s.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) { error = "Italian P.IVA must be 11 digits (16-char codice is for individuals)."; return false; }
        // Luhn from the right
        var sum = 0;
        for (var i = 0; i < 11; i++)
        {
            var d = digits[10 - i] - '0';
            if (i % 2 == 1) { d *= 2; if (d > 9) d -= 9; }
            sum += d;
        }
        if (sum % 10 != 0) { error = "Italian P.IVA has an invalid Luhn checksum."; return false; }
        value = new ItalianFiscalCode(digits);
        error = null;
        return true;
    }

    public static ItalianFiscalCode Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Serbian matični broj — 8 digits. The published MOD-11 algorithm (weights
/// [8,7,6,5,4,3,2]) doesn't actually validate real APR-issued numbers (NIS 20084693,
/// Telekom Srbija 07069461), so we validate format only. Authoritative confirmation
/// happens at lookup time against APR or OpenCorporates.
/// </summary>
public sealed record SerbianMaticniBroj : CompanyIdentifier
{
    private SerbianMaticniBroj(string value) : base("RS", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out SerbianMaticniBroj? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Serbian matični broj cannot be empty."; return false; }
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 8) { error = "Serbian matični broj must be 8 digits."; return false; }
        value = new SerbianMaticniBroj(digits);
        error = null;
        return true;
    }

    public static SerbianMaticniBroj Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Serbian PIB (tax identification number) — 9 digits with ISO 7064 MOD 11,10
/// (same algorithm as Croatian OIB, applied to 8 digits + 1 check).
/// </summary>
public sealed record SerbianPib : CompanyIdentifier
{
    private SerbianPib(string value) : base("RS", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out SerbianPib? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Serbian PIB cannot be empty."; return false; }
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 9) { error = "Serbian PIB must be 9 digits."; return false; }
        var r = 10;
        for (var i = 0; i < 8; i++)
        {
            r = (r + (digits[i] - '0')) % 10;
            if (r == 0) r = 10;
            r = (r * 2) % 11;
        }
        var check = (11 - r) % 10;
        if (check != digits[8] - '0') { error = "Serbian PIB has an invalid ISO 7064 MOD 11,10 checksum."; return false; }
        value = new SerbianPib(digits);
        error = null;
        return true;
    }

    public static SerbianPib Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public override string ToString() => Value;
}
