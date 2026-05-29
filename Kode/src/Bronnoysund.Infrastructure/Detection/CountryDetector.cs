// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;

namespace Bronnoysund.Infrastructure.Detection;

/// <summary>
/// Best-effort identifier-to-country detector. Tries each registered country pattern in
/// priority order and returns the first match. Where two countries can match the same raw
/// input (8-digit DK/EE, 11-digit IT/HR/LV, 9-digit GR/LT/PL/RS, 10-digit SE/PL/SI), the
/// detector commits to the most-likely candidate for our Nordic user base; the UI provides
/// an explicit country override (Plan 26 B2) for the rest.
/// </summary>
/// <remarks>
/// Detection priority hierarchy:
/// 1. Strongest checksum + most-specific prefix wins (Norwegian MOD-11 + leading 8/9).
/// 2. Mandatory separators / explicit country prefixes win next (Finnish dash, Spanish
///    entity-letter, EU VAT-style prefixes NO/SE/FI/DK/EE/LV/PL/EL/GR/IT).
/// 3. Unique-length identifiers (Greek 12-digit GEMI, Polish 14-digit REGON).
/// 4. For length-collision groups, Nordic user base wins (SE before PL NIP, DK before
///    EE/RS in the 8-digit bucket). Inside non-Nordic groups (HR/IT/LV at 11 digits),
///    bigger user base wins (IT before HR).
/// 5. Last-resort fallbacks (Irish 1-7 digits without prefix) come last.
///
/// Code-review-2026-05-29 iter-2 changes:
///   * 10-digit: SE moved BEFORE PL NIP (previous order misrouted ~9% of SE org-nrs that
///     also pass NIP MOD-11 — including Volvo 5560125790 — to Poland).
///   * 11-digit: IT moved BEFORE HR (~10% of IT P.IVAs also pass HR ISO 7064 MOD 11,10).
///   * 8-digit: dead EstonianRegistryCode call removed (DK accepted all non-zero 8-digit
///     inputs first, making EE unreachable). EE now requires explicit "EE" VAT prefix or
///     the UI country override to be selected.
///   * Explicit VAT-style prefix branch extended to NO/SE/FI/DK/EE/PL — pasting
///     "NO919300388" no longer falls through to Greek AFM length match.
/// </remarks>
internal sealed class CountryDetector : ICountryDetector
{
    public CompanyIdentifier? Detect(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return null;
        }

        // ---- Explicit country prefixes commit first ------------------------------------
        // The TryCreate methods of every value object strip non-digit characters, so
        // pasting "SE5560360793" or "NO919300388" works as long as the detector dispatches
        // to the right TryCreate based on the prefix. Without these short-circuits the
        // length-bucket dispatch below would misroute (e.g. "LV40003032949" passes IT
        // Luhn; "FI01120389" matches Danish CVR shape; "NO919300388" matches Greek AFM
        // length).
        var trimmed = rawInput.TrimStart();
        // OrganizationNumber.Normalize doesn't strip letters (the other 14 value objects
        // do via Where(char.IsDigit)), so we pre-strip the NO prefix before calling
        // TryCreate. Iter-2 caught this regression.
        if (StartsWithIgnoreCase(trimmed, "NO") &&
            OrganizationNumber.TryCreate(trimmed[2..], out var noPrefixed, out _))
        {
            return noPrefixed;
        }
        if (StartsWithIgnoreCase(trimmed, "SE") &&
            SwedishOrganizationNumber.TryCreate(rawInput, out var sePrefixed, out _))
        {
            return sePrefixed;
        }
        if (StartsWithIgnoreCase(trimmed, "FI") &&
            FinnishBusinessId.TryCreate(rawInput, out var fiPrefixed, out _))
        {
            return fiPrefixed;
        }
        if (StartsWithIgnoreCase(trimmed, "DK") &&
            DanishCvrNumber.TryCreate(rawInput, out var dkPrefixed, out _))
        {
            return dkPrefixed;
        }
        if (StartsWithIgnoreCase(trimmed, "EE") &&
            EstonianRegistryCode.TryCreate(rawInput, out var eePrefixed, out _))
        {
            return eePrefixed;
        }
        if (StartsWithIgnoreCase(trimmed, "LV") &&
            LatvianRegistrationNumber.TryCreate(rawInput, out var lvPrefixed, out _))
        {
            return lvPrefixed;
        }
        // PL prefix is the EU VAT convention = NIP. KRS / REGON aren't typically pasted
        // with a country prefix, so we don't try them here — they'll go through the
        // length-bucket dispatch below for unprefixed input.
        if (StartsWithIgnoreCase(trimmed, "PL") &&
            PolishNip.TryCreate(rawInput, out var plPrefixed, out _))
        {
            return plPrefixed;
        }
        if ((StartsWithIgnoreCase(trimmed, "EL") || StartsWithIgnoreCase(trimmed, "GR")) &&
            GreekVatNumber.TryCreate(rawInput, out var grPrefixed, out _))
        {
            return grPrefixed;
        }
        if (StartsWithIgnoreCase(trimmed, "IT") &&
            ItalianFiscalCode.TryCreate(rawInput, out var itPrefixed, out _))
        {
            return itPrefixed;
        }

        // ---- Highly-specific shapes (no prefix needed) ---------------------------------

        // Norwegian — 9 digits + MOD-11 + leading 8/9 is unambiguous.
        if (OrganizationNumber.TryCreate(rawInput, out var organizationNumber, out _))
        {
            return organizationNumber;
        }

        // Spanish NIF — starts with an entity-type letter (A-W minus a few). The letter
        // prefix eliminates any collision with pure-numeric IDs.
        if (SpanishNif.TryCreate(rawInput, out var spanish, out _))
        {
            return spanish;
        }

        // Finnish Y-tunnus requires the explicit dash to commit (the dashless form is
        // ambiguous with Estonian/Danish 8-digit codes). Both ASCII hyphen and the common
        // Unicode dash variants (U+2013 EN DASH, U+2014 EM DASH, U+2212 MINUS SIGN) count
        // — users routinely paste Finnish IDs that have been auto-corrected by Word/etc.
        if (ContainsDashLike(rawInput) &&
            FinnishBusinessId.TryCreate(NormaliseDashes(rawInput), out var finnishBusinessId, out _))
        {
            return finnishBusinessId;
        }

        // ---- Numeric-only IDs grouped by digit-count -----------------------------------

        var digits = new string(rawInput.Where(char.IsDigit).ToArray());

        // 14-digit: Polish REGON-14 is the only such pattern we know.
        if (digits.Length == 14 &&
            PolishRegon.TryCreate(rawInput, out var regon14, out _))
        {
            return regon14;
        }

        // 12-digit: Greek GEMI is unique.
        if (digits.Length == 12 &&
            GreekGemiNumber.TryCreate(rawInput, out var gemi, out _))
        {
            return gemi;
        }

        // 11-digit: IT (Luhn) → HR (ISO 7064 MOD 11,10) → LV (format-only, leading 4/5).
        // ~10% of IT-valid P.IVAs also pass HR ISO 7064 (independent algorithms, no
        // structural relationship). IT has the larger user base so wins the tie. HR's
        // valid set is similarly a subset that overlaps IT in places. LV's leading-4/5
        // guard catches almost everything else; explicit "LV" prefix commits above.
        if (digits.Length == 11)
        {
            if (ItalianFiscalCode.TryCreate(rawInput, out var italian, out _)) return italian;
            if (CroatianOib.TryCreate(rawInput, out var croatian, out _)) return croatian;
            if (LatvianRegistrationNumber.TryCreate(rawInput, out var latvian, out _)) return latvian;
        }

        // 10-digit: PL KRS-with-leading-0000 → SE Luhn → PL NIP MOD-11 → SI (format-only).
        // PL KRS-leading-0000 is the most-specific structural signal and must win first —
        // PKN Orlen's KRS "0000028860" also happens to be Luhn-valid (so SE would
        // preempt without the explicit leading-0000 guard). SE wins over PL NIP because
        // ~9% of SE-Luhn-valid org-nrs also pass NIP MOD-11 (brute-force-verified in the
        // 5560- range) and SE has the larger Nordic user base.
        if (digits.Length == 10)
        {
            if (digits.StartsWith("0000", StringComparison.Ordinal) &&
                PolishKrsNumber.TryCreate(rawInput, out var krs, out _))
            {
                return krs;
            }
            if (SwedishOrganizationNumber.TryCreate(rawInput, out var swedish, out _)) return swedish;
            if (PolishNip.TryCreate(rawInput, out var nip, out _)) return nip;
            if (SlovenianMaticnaStevilka.TryCreate(rawInput, out var slovenian, out _)) return slovenian;
        }

        // 9-digit: GR AFM (powers-of-2 MOD-11) → LT (two-step MOD-11) → PL REGON-9
        // (MOD-11 with [8,9,2,3,4,5,6,7] weights) → RS PIB (ISO 7064 MOD 11,10). The four
        // checksums rarely collide; for the cases where they do, we prefer the earliest
        // in the list. NO orgnr was already handled above.
        if (digits.Length == 9)
        {
            if (GreekVatNumber.TryCreate(rawInput, out var afm, out _)) return afm;
            if (LithuanianCompanyCode.TryCreate(rawInput, out var lithuanian, out _)) return lithuanian;
            if (PolishRegon.TryCreate(rawInput, out var regon9, out _)) return regon9;
            if (SerbianPib.TryCreate(rawInput, out var pib, out _)) return pib;
        }

        // 8-digit: DK CVR (format-only, non-zero leading) wins by default because the
        // Nordic user base is dominant. EE registrikood would be a subset of DK's match
        // set (DK accepts any non-zero 8-digit, EE accepts only 1/7/8/9-leading); the
        // earlier in-bucket EE call was dead code and is removed (iter-2). Estonian users
        // must paste "EE12417834" or use the UI country override to reach EE detection.
        // RS matični broj is 8-digit format-only with no leading-digit restriction; it
        // only catches plain digit input, never anything containing a dash-like character
        // (so a Finnish-shape input that failed MOD-11 doesn't silently leak to RS).
        if (digits.Length == 8)
        {
            if (DanishCvrNumber.TryCreate(rawInput, out var danish, out _)) return danish;
            if (!ContainsDashLike(rawInput) &&
                SerbianMaticniBroj.TryCreate(rawInput, out var mb, out _))
            {
                return mb;
            }
        }

        // Irish CRO (1-7 digits without prefix). Last resort — a bare "5" matches IE
        // but also matches many partial inputs. We accept the false-positive risk
        // because the alternative (no detection) is worse, and Plan 26's UI flag
        // indicator surfaces the (sometimes wrong) detection back to the user.
        if (IrishCroNumber.TryCreate(rawInput, out var irish, out _))
        {
            return irish;
        }

        return null;
    }

    private static bool StartsWithIgnoreCase(string s, string prefix) =>
        s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    // Matches ASCII hyphen plus the common Unicode dash variants users paste from Word,
    // PDFs and rich-text emails.
    private static bool ContainsDashLike(string s)
    {
        foreach (var c in s)
        {
            if (c is '-' or '‐' or '‑' or '‒' or '–' or '—' or '―' or '−') return true;
        }
        return false;
    }

    private static string NormaliseDashes(string s)
    {
        if (!ContainsDashLike(s)) return s;
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
        {
            sb.Append(c is '‐' or '‑' or '‒' or '–' or '—' or '―' or '−' ? '-' : c);
        }
        return sb.ToString();
    }
}
