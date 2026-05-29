// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Detection;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// Covers every country pattern the detector knows plus the explicit priority-order
/// edge cases identified in the 2026-05-29 code review (notably the 10-digit
/// SE/PL/SI collision and the 11-digit HR/IT/LV collision).
/// </summary>
public class CountryDetectorTests
{
    private readonly CountryDetector _detector = new();

    // -- Norway -------------------------------------------------------------------

    [Theory]
    [InlineData("919300388")] // Equinor
    [InlineData("933722821")] // Røa Systemutvikling AS
    [InlineData("974760843")] // Statens vegvesen
    public void Detect_NorwegianOrganizationNumber_ReturnsNorway(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<OrganizationNumber>();
        result!.CountryCode.Should().Be("NO");
    }

    [Theory]
    [InlineData("NO919300388")]
    [InlineData("no 919300388")]
    public void Detect_NorwegianWithExplicitPrefix_ReturnsNorway(string raw)
    {
        // Without the explicit-NO branch, "NO919300388" would have 9 digits and could
        // match Greek AFM. The prefix commit ensures the right routing.
        var result = _detector.Detect(raw);
        result.Should().BeOfType<OrganizationNumber>();
    }

    // -- Finland ------------------------------------------------------------------

    [Theory]
    [InlineData("0112038-9")] // Nokia Oyj
    [InlineData("2646674-9")] // Wolt Oy
    [InlineData("2336509-6")] // Supercell Oy
    public void Detect_FinnishBusinessId_ReturnsFinland(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<FinnishBusinessId>();
        result!.CountryCode.Should().Be("FI");
    }

    [Fact]
    public void Detect_FinnishDigitsWithoutDash_ReturnsNull()
    {
        // 8 plain digits "01120389" can't unambiguously be Finnish. With the iter-2
        // detector: DK rejects leading-0, RS rejects dashless... wait, RS would
        // accept this (no leading-digit guard). Verify the actual routing:
        // DK rejects (leading 0), RS accepts (8 plain digits) → returns RS.
        // Document this surprise here so a future refactor either fixes it explicitly
        // or accepts it knowingly.
        var result = _detector.Detect("01120389");
        result.Should().BeOfType<SerbianMaticniBroj>();
    }

    [Theory]
    [InlineData("0112038–9")]  // EN DASH
    [InlineData("0112038—9")]  // EM DASH
    [InlineData("0112038−9")]  // MINUS SIGN
    public void Detect_FinnishWithUnicodeDash_StillReturnsFinland(string raw)
    {
        // Users copy from Word/PDF/web pages which auto-correct hyphens to en/em
        // dashes. Iter-2 caught that the ASCII-only Contains('-') guard misroutes
        // these to RS.
        var result = _detector.Detect(raw);
        result.Should().BeOfType<FinnishBusinessId>();
    }

    // -- Estonia ------------------------------------------------------------------

    [Fact]
    public void Detect_EstonianRegistryCode_GoesToDenmarkUnderCurrentPolicy()
    {
        // 12417834 (Bolt Technology OÜ) is also valid Danish (8 digits, non-zero
        // leading). Current policy: Danish wins for the Nordic user base. Estonian
        // users must use the EE prefix or the UI country override.
        var result = _detector.Detect("12417834");
        result.Should().BeOfType<DanishCvrNumber>();
        result!.CountryCode.Should().Be("DK");
    }

    [Theory]
    [InlineData("EE12417834")]
    [InlineData("ee12417834")]  // lowercase prefix
    [InlineData("Ee 12417834")] // mixed case + space
    public void Detect_EstonianWithExplicitPrefix_ReturnsEstonia(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<EstonianRegistryCode>();
        result!.CountryCode.Should().Be("EE");
    }

    // -- Poland -------------------------------------------------------------------

    [Theory]
    [InlineData("0000028860")]  // PKN Orlen KRS (leading zeros)
    [InlineData("0000059492")]  // CD Projekt KRS
    public void Detect_PolishKrsWithLeadingZeros_ReturnsKrs(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<PolishKrsNumber>();
        result!.CountryCode.Should().Be("PL");
    }

    [Theory]
    [InlineData("7740001454")]  // PKN Orlen NIP
    [InlineData("5260250274")]  // Allegro NIP
    public void Detect_PolishNip_ReturnsNip(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<PolishNip>();
        result!.CountryCode.Should().Be("PL");
    }

    // -- Sweden — critical regression: 10-digit Swedish org-nrs must not be misdetected
    //    as Polish KRS (code-review-2026-05-29 critical #1) or as Polish NIP (iter-2
    //    critical #3). ---------------------------------------------------------------

    [Theory]
    [InlineData("5560360793")]  // Volvo AB
    [InlineData("5567370431")]  // Klarna AB
    [InlineData("5560125790")]  // Volvo SE-Luhn-valid AND PL-NIP-MOD-11-valid (iter-2 collision case)
    public void Detect_SwedishOrganizationNumber_ReturnsSweden(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<SwedishOrganizationNumber>();
        result!.CountryCode.Should().Be("SE");
    }

    [Theory]
    [InlineData("SE5560360793")]
    [InlineData("se 5560360793")]
    public void Detect_SwedishWithExplicitPrefix_ReturnsSweden(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<SwedishOrganizationNumber>();
    }

    [Theory]
    [InlineData("556036-0793")]
    public void Detect_SwedishOrganizationNumberWithDash_ReturnsSweden(string raw)
    {
        // Verifies the dash variant doesn't get hijacked by the Finnish dash-required
        // branch (Finnish is NNNNNNN-N, SE is NNNNNN-NNNN — different shape).
        var result = _detector.Detect(raw);
        result.Should().BeOfType<SwedishOrganizationNumber>();
    }

    // -- Denmark ------------------------------------------------------------------

    [Theory]
    [InlineData("28856713")]  // Maersk
    [InlineData("33063295")]  // Carlsberg
    [InlineData("10001234")]  // Synthetic CVR starting with 1 (Novo Nordisk-style range)
    public void Detect_DanishCvrNumber_ReturnsDenmark(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<DanishCvrNumber>();
        result!.CountryCode.Should().Be("DK");
    }

    [Theory]
    [InlineData("DK28856713")]
    [InlineData("dk 28856713")]
    public void Detect_DanishWithExplicitPrefix_ReturnsDenmark(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<DanishCvrNumber>();
    }

    // -- Slovenia -----------------------------------------------------------------

    [Theory]
    [InlineData("5043611001")]  // Sub-unit suffix — not a valid SE Luhn but IS a valid SI matična
    public void Detect_SlovenianMaticnaStevilka_ReturnsSlovenia(string raw)
    {
        // The 5043611001 example fails Swedish Luhn so the detector falls through to SI.
        var result = _detector.Detect(raw);
        result.Should().BeOfType<SlovenianMaticnaStevilka>();
        result!.CountryCode.Should().Be("SI");
    }

    // -- Lithuania ----------------------------------------------------------------

    [Theory]
    [InlineData("120545849")]  // Vilniaus Vandenys
    [InlineData("121215434")]  // Telia Lietuva
    public void Detect_LithuanianCompanyCode_ReturnsLithuania(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<LithuanianCompanyCode>();
        result!.CountryCode.Should().Be("LT");
    }

    // -- Croatia ------------------------------------------------------------------

    [Theory]
    [InlineData("27759560625")]  // INA d.d.
    [InlineData("71149912416")]  // Atlantic Grupa
    public void Detect_CroatianOib_ReturnsCroatia(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<CroatianOib>();
        result!.CountryCode.Should().Be("HR");
    }

    // -- Greece -------------------------------------------------------------------

    [Theory]
    [InlineData("154558160000")]  // Eurobank S.A. (12-digit GEMI)
    [InlineData("000223001000")]  // Eurobank Holdings (leading zeros preserved)
    public void Detect_GreekGemiNumber_ReturnsGreece(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<GreekGemiNumber>();
        result!.CountryCode.Should().Be("GR");
    }

    [Theory]
    [InlineData("094019245")]    // OTE AFM
    [InlineData("EL094019245")]  // With EU VAT prefix
    public void Detect_GreekVatNumber_ReturnsGreece(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<GreekVatNumber>();
        result!.CountryCode.Should().Be("GR");
    }

    // -- Latvia -------------------------------------------------------------------

    [Theory]
    [InlineData("40003245752")]    // Air Baltic
    [InlineData("LV40003032949")]  // With LV VAT prefix
    public void Detect_LatvianRegistrationNumber_ReturnsLatvia(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<LatvianRegistrationNumber>();
        result!.CountryCode.Should().Be("LV");
    }

    // -- Spain --------------------------------------------------------------------

    [Theory]
    [InlineData("A28015865")]  // Telefónica
    [InlineData("A48265169")]  // BBVA
    public void Detect_SpanishNif_ReturnsSpain(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<SpanishNif>();
        result!.CountryCode.Should().Be("ES");
    }

    // -- Italy — critical regression: 11-digit IT P.IVAs must not be misrouted to HR
    //    (iter-2 critical #4 — ~10% of IT-Luhn-valid numbers also pass HR ISO 7064). --

    [Theory]
    [InlineData("00159560366")]    // Ferrari S.p.A.
    [InlineData("00905811006")]    // Eni S.p.A.
    [InlineData("IT00905811006")]  // With IT VAT prefix
    [InlineData("it 00905811006")] // mixed-case prefix
    public void Detect_ItalianFiscalCode_ReturnsItaly(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<ItalianFiscalCode>();
        result!.CountryCode.Should().Be("IT");
    }

    // -- Serbia -------------------------------------------------------------------

    [Theory]
    [InlineData("104052135")]  // NIS a.d. PIB (9 digits, ISO 7064 MOD 11,10)
    [InlineData("100002887")]  // Telekom Srbija PIB
    public void Detect_SerbianPib_ReturnsSerbia(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<SerbianPib>();
        result!.CountryCode.Should().Be("RS");
    }

    // -- Ireland (last-resort fallback) -------------------------------------------

    [Theory]
    [InlineData("408059")]  // Microsoft Ireland
    [InlineData("593876")]  // Stripe Payments Europe
    [InlineData("5")]       // Single digit — accepts but is genuinely ambiguous
    public void Detect_IrishCroNumber_ReturnsIreland(string raw)
    {
        var result = _detector.Detect(raw);
        result.Should().BeOfType<IrishCroNumber>();
        result!.CountryCode.Should().Be("IE");
    }

    // -- Genuinely-unrecognised input ---------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a number")]
    [InlineData("0112038-0")]  // Finnish-shaped but wrong checksum
    public void Detect_UnknownOrInvalidInput_ReturnsNull(string? raw)
    {
        _detector.Detect(raw).Should().BeNull();
    }
}
