// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Bronnoysund.Domain;

/// <summary>
/// Estonian Registry Code (registrikood) as a DDD value object. 8 digits where the first
/// digit indicates the entity type: 1 = commercial company (OÜ/AS/FIE), 7 = school or
/// public agency, 8 = non-profit association (MTÜ), 9 = foundation (SA). There is no
/// officially published checksum, so validation is format-only.
/// </summary>
public sealed partial record EstonianRegistryCode : CompanyIdentifier
{
    [GeneratedRegex("^[1789]\\d{7}$", RegexOptions.Compiled)]
    private static partial Regex Pattern();

    private EstonianRegistryCode(string value) : base("EE", value) { }

    public static bool TryCreate(string? raw, [NotNullWhen(true)] out EstonianRegistryCode? value, out string? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Estonian registry code cannot be empty.";
            return false;
        }
        var trimmed = new string(raw.Where(char.IsDigit).ToArray());
        if (!Pattern().IsMatch(trimmed))
        {
            error = "Estonian registry code must be 8 digits starting with 1, 7, 8 or 9.";
            return false;
        }
        value = new EstonianRegistryCode(trimmed);
        error = null;
        return true;
    }

    public static EstonianRegistryCode Create(string raw)
    {
        if (!TryCreate(raw, out var value, out var error)) throw new ArgumentException(error, nameof(raw));
        return value;
    }

    public EntityKind Kind => Value[0] switch
    {
        '1' => EntityKind.Company,
        '7' => EntityKind.PublicAgency,
        '8' => EntityKind.NonProfit,
        '9' => EntityKind.Foundation,
        _ => EntityKind.Unknown,
    };

    public override string ToString() => Value;
}

public enum EntityKind { Unknown, Company, PublicAgency, NonProfit, Foundation }
