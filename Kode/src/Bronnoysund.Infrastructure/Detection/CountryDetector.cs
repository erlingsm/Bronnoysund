// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;

namespace Bronnoysund.Infrastructure.Detection;

/// <summary>
/// Default <see cref="ICountryDetector"/>. Recognises the Norwegian 9-digit MOD11
/// organization number and the Finnish 7-digit + dash + check digit Y-tunnus; further
/// international identifier types are added here as their adapter packs come online.
/// Detection order matters: each new pattern must be discriminable from existing ones
/// (length, separator, or prefix) so the same raw input never matches two countries.
/// Today's two are unambiguous — NO is 9 plain digits, FI is 8 digits with a dash in
/// position 8 (and the dash is required to disambiguate from anything else that's exactly
/// 8 digits, e.g. Estonian registrikood).
/// </summary>
internal sealed class CountryDetector : ICountryDetector
{
    public CompanyIdentifier? Detect(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return null;
        }

        // 1. Norwegian — 9 digits + MOD11 + leading 8/9 is highly specific; try first.
        if (OrganizationNumber.TryCreate(rawInput, out var organizationNumber, out _))
        {
            return organizationNumber;
        }

        // 2. Finnish — NNNNNNN-N requires an explicit dash to commit (the dashless form
        //    would be ambiguous with Estonian/Danish 8-digit codes).
        if (rawInput.Contains('-') &&
            FinnishBusinessId.TryCreate(rawInput, out var finnishBusinessId, out _))
        {
            return finnishBusinessId;
        }

        // 3. Estonian — 8 digits starting with 1, 7, 8 or 9 (entity-type prefix) is
        //    unambiguous within the set we support today.
        if (EstonianRegistryCode.TryCreate(rawInput, out var estonian, out _))
        {
            return estonian;
        }

        // 4. Polish — try KRS (10 digits, leading zeros are common) before NIP (10 digits
        //    with MOD-11) because KRS values starting "0000" never validate as NIP.
        var digits = new string(rawInput.Where(char.IsDigit).ToArray());
        if (digits.Length == 10 && digits.StartsWith("0000", StringComparison.Ordinal) &&
            PolishKrsNumber.TryCreate(rawInput, out var krs, out _))
        {
            return krs;
        }
        if (PolishNip.TryCreate(rawInput, out var nip, out _))
        {
            return nip;
        }
        if (digits.Length == 10 && PolishKrsNumber.TryCreate(rawInput, out var krs2, out _))
        {
            return krs2;
        }
        if ((digits.Length == 9 || digits.Length == 14) &&
            PolishRegon.TryCreate(rawInput, out var regon, out _))
        {
            return regon;
        }

        // 5. Irish — 1-7 digits without prefix. Tried last because a bare "5" matches IE
        //    but would also match many partial inputs from other countries; we accept the
        //    false-positive risk because the alternative (no detection at all) is worse.
        if (IrishCroNumber.TryCreate(rawInput, out var irish, out _))
        {
            return irish;
        }

        return null;
    }
}
