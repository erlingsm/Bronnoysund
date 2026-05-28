// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Diagnostics.CodeAnalysis;

namespace Bronnoysund.Domain;

/// <summary>
/// Swedish organisationsnummer — 10 digits with Luhn (MOD-10) checksum on the 10th digit.
/// Bolagsverket accepts both NNNNNNNNNN and NNNNNN-NNNN; we normalise to the dashless form.
/// </summary>
public sealed record SwedishOrganizationNumber : CompanyIdentifier
{
    private SwedishOrganizationNumber(string value) : base("SE", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out SwedishOrganizationNumber? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Swedish organisationsnummer cannot be empty."; return false; }
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 10) { error = "Swedish organisationsnummer must be 10 digits."; return false; }
        if (!IsValidLuhn(digits)) { error = "Swedish organisationsnummer has an invalid Luhn checksum."; return false; }
        value = new SwedishOrganizationNumber(digits);
        error = null;
        return true;
    }

    public static SwedishOrganizationNumber Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    private static bool IsValidLuhn(string s)
    {
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            var d = s[i] - '0';
            if (i % 2 == 0) { d *= 2; if (d > 9) d -= 9; }
            sum += d;
        }
        return sum % 10 == 0;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Danish CVR-nummer — 8 digits. Although some references claim a MOD-11 checksum exists,
/// real CVRs assigned by Erhvervsstyrelsen (e.g. Maersk 28856713, Carlsberg 33063295) do
/// not satisfy any published formula, so validation here is format-only. Authoritative
/// confirmation happens at lookup time against virk.dk.
/// </summary>
public sealed record DanishCvrNumber : CompanyIdentifier
{
    private DanishCvrNumber(string value) : base("DK", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out DanishCvrNumber? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Danish CVR number cannot be empty."; return false; }
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 8) { error = "Danish CVR number must be 8 digits."; return false; }
        // CVRs are issued sequentially from the 10-millions; a leading zero would never
        // appear in a real CVR. We use that as the discriminator that lets us avoid
        // claiming Finnish-looking dashless 8-digit codes (e.g. "01120389").
        if (digits[0] == '0') { error = "Danish CVR number cannot start with 0."; return false; }
        value = new DanishCvrNumber(digits);
        error = null;
        return true;
    }

    public static DanishCvrNumber Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Slovenian matična številka — 10 digits, no checksum. Entity main records have suffix
/// "000"; sub-units use other 3-digit suffixes (e.g. 5043611001). NormalizedRoot returns
/// the first 7 digits.
/// </summary>
public sealed record SlovenianMaticnaStevilka : CompanyIdentifier
{
    private SlovenianMaticnaStevilka(string value) : base("SI", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out SlovenianMaticnaStevilka? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Slovenian matična številka cannot be empty."; return false; }
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 10) { error = "Slovenian matična številka must be 10 digits."; return false; }
        value = new SlovenianMaticnaStevilka(digits);
        error = null;
        return true;
    }

    public static SlovenianMaticnaStevilka Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    public string NormalizedRoot => Value[..7];

    public override string ToString() => Value;
}

/// <summary>
/// Lithuanian įmonės kodas — 9 digits with two-step MOD-11 checksum. Step 1 uses weights
/// [1,2,3,4,5,6,7,8]; if remainder == 10, step 2 uses weights [3,4,5,6,7,8,9,1] and a
/// remainder of 10 there means the check digit is 0.
/// </summary>
public sealed record LithuanianCompanyCode : CompanyIdentifier
{
    private static readonly int[] Step1Weights = [1, 2, 3, 4, 5, 6, 7, 8];
    private static readonly int[] Step2Weights = [3, 4, 5, 6, 7, 8, 9, 1];

    private LithuanianCompanyCode(string value) : base("LT", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out LithuanianCompanyCode? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = "Lithuanian įmonės kodas cannot be empty."; return false; }
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 9) { error = "Lithuanian įmonės kodas must be 9 digits."; return false; }
        if (!IsValidChecksum(digits)) { error = "Lithuanian įmonės kodas has an invalid MOD-11 checksum."; return false; }
        value = new LithuanianCompanyCode(digits);
        error = null;
        return true;
    }

    public static LithuanianCompanyCode Create(string raw)
    {
        if (!TryCreate(raw, out var v, out var e)) throw new ArgumentException(e, nameof(raw));
        return v;
    }

    private static bool IsValidChecksum(string s)
    {
        var arr = s.Select(c => c - '0').ToArray();
        var sum = 0;
        for (var i = 0; i < 8; i++) sum += arr[i] * Step1Weights[i];
        var check = sum % 11;
        if (check == 10)
        {
            sum = 0;
            for (var i = 0; i < 8; i++) sum += arr[i] * Step2Weights[i];
            check = sum % 11;
            if (check == 10) check = 0;
        }
        return check == arr[8];
    }

    public override string ToString() => Value;
}
