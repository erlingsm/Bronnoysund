// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Diagnostics.CodeAnalysis;

namespace Bronnoysund.Domain;

/// <summary>
/// Irish CRO Company Number — 1 to 7 digits, no leading zero (per CRO convention), no
/// checksum. The variable length disambiguates Ireland from longer fixed-width identifiers
/// (Norwegian 9 digits, Finnish 8+dash, Estonian 8) but means the detector needs an
/// upper bound on what it accepts.
/// </summary>
public sealed record IrishCroNumber : CompanyIdentifier
{
    private IrishCroNumber(string value) : base("IE", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out IrishCroNumber? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Irish CRO number cannot be empty.";
            return false;
        }
        var trimmed = new string(raw.Where(char.IsDigit).ToArray());
        if (trimmed.Length is < 1 or > 7)
        {
            error = "Irish CRO number must be between 1 and 7 digits.";
            return false;
        }
        if (trimmed.Length > 1 && trimmed[0] == '0')
        {
            error = "Irish CRO number must not have a leading zero.";
            return false;
        }
        value = new IrishCroNumber(trimmed);
        error = null;
        return true;
    }

    public static IrishCroNumber Create(string raw)
    {
        if (!TryCreate(raw, out var value, out var error)) throw new ArgumentException(error, nameof(raw));
        return value;
    }

    public override string ToString() => Value;
}
