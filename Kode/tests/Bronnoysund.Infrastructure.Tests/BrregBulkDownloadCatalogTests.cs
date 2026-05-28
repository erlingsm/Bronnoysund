// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Infrastructure.Brreg;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

public class BrregBulkDownloadCatalogTests
{
    private readonly BrregBulkDownloadCatalog _sut = new();

    [Fact]
    public void GetAll_ReturnsAllKnownBulkUrls()
    {
        var entries = _sut.GetAll();

        // 3 Enheter (json/excel/csv) + 3 Underenheter + 1 Frivillighet + 1 Parti = 8
        entries.Should().HaveCount(8);
    }

    [Fact]
    public void GetAll_AllUrlsAreAbsoluteAndPointAtBrreg()
    {
        var entries = _sut.GetAll();

        entries.Should().AllSatisfy(e =>
        {
            e.Url.Should().StartWith("https://data.brreg.no/");
            Uri.TryCreate(e.Url, UriKind.Absolute, out _).Should().BeTrue();
        });
    }

    [Fact]
    public void GetAll_CoversAllThreeRegisters()
    {
        var entries = _sut.GetAll();

        entries.Should().Contain(e => e.Register == "Enhetsregisteret");
        entries.Should().Contain(e => e.Register == "Frivillighetsregisteret");
        entries.Should().Contain(e => e.Register == "Partiregisteret");
    }

    [Fact]
    public void GetAll_GzippedEntriesAreMarkedCompressed()
    {
        var entries = _sut.GetAll();

        var gzipEntries = entries.Where(e => e.Format.Contains("gzip", StringComparison.OrdinalIgnoreCase));
        gzipEntries.Should().AllSatisfy(e => e.Compressed.Should().BeTrue());

        var nonGzipEntries = entries.Where(e => !e.Format.Contains("gzip", StringComparison.OrdinalIgnoreCase));
        nonGzipEntries.Should().AllSatisfy(e => e.Compressed.Should().BeFalse());
    }
}
