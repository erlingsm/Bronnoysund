// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.WebApi.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace Bronnoysund.WebApi.Tests;

/// <summary>
/// D1: End-to-end coverage for <c>GET /downloads</c>. The endpoint is a thin wrapper around
/// <c>IBulkDownloadCatalog.GetAll()</c>; we wire the substitute in <see cref="WebApiFactory"/>
/// to a synthetic catalogue that mirrors the production shape (8 entries spanning the three
/// registers, both gzip and plain formats). The real <c>BrregBulkDownloadCatalog</c> is
/// internal to the Infrastructure assembly, and the static eight-entry contract is already
/// covered there by <c>BrregBulkDownloadCatalogTests</c>, so the WebApi tests focus on the
/// HTTP-layer wiring rather than the catalogue contents.
/// </summary>
public class DownloadsEndpointTests
{
    private static readonly IReadOnlyList<BulkDownload> SampleCatalogue =
    [
        new BulkDownload("Alle enheter (JSON)", "Bulk enheter dump", "Enhetsregisteret",
            "application/gzip+json", "https://data.brreg.no/enhetsregisteret/api/enheter/lastned", Compressed: true),
        new BulkDownload("Alle enheter (Excel)", "Bulk enheter Excel", "Enhetsregisteret",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "https://data.brreg.no/enhetsregisteret/api/enheter/lastned/regneark", Compressed: false),
        new BulkDownload("Alle enheter (CSV)", "Bulk enheter CSV", "Enhetsregisteret",
            "text/csv", "https://data.brreg.no/enhetsregisteret/api/enheter/lastned/csv-escaped", Compressed: false),
        new BulkDownload("Alle underenheter (JSON)", "Bulk underenheter dump", "Enhetsregisteret",
            "application/gzip+json", "https://data.brreg.no/enhetsregisteret/api/underenheter/lastned", Compressed: true),
        new BulkDownload("Alle underenheter (Excel)", "Bulk underenheter Excel", "Enhetsregisteret",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "https://data.brreg.no/enhetsregisteret/api/underenheter/lastned/regneark", Compressed: false),
        new BulkDownload("Alle underenheter (CSV)", "Bulk underenheter CSV", "Enhetsregisteret",
            "text/csv", "https://data.brreg.no/enhetsregisteret/api/underenheter/lastned/csv-escaped", Compressed: false),
        new BulkDownload("Alle frivillige (CSV)", "Bulk frivillighet CSV", "Frivillighetsregisteret",
            "text/csv", "https://data.brreg.no/frivillighetsregisteret/api/frivillige-organisasjoner/totalbestand/csv-escaped", Compressed: false),
        new BulkDownload("Alle partier (CSV)", "Bulk parti CSV", "Partiregisteret",
            "text/csv", "https://data.brreg.no/partiregisteret/api/lastned/csv-escaped", Compressed: false),
    ];

    [Fact]
    public async Task GetDownloads_ReturnsOkWithEightEntries()
    {
        using var factory = new WebApiFactory();
        factory.BulkDownloads.GetAll().Returns(SampleCatalogue);
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/downloads", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<BulkDownload>>();
        entries.Should().NotBeNull();
        // 3 Enheter (json/excel/csv) + 3 Underenheter + 1 Frivillighet + 1 Parti = 8
        entries!.Should().HaveCount(8);
    }

    [Fact]
    public async Task GetDownloads_EntriesExposeRequiredFields()
    {
        using var factory = new WebApiFactory();
        factory.BulkDownloads.GetAll().Returns(SampleCatalogue);
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/downloads", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<BulkDownload>>();
        entries.Should().NotBeNull();

        // Verify the record shape — every entry has the six fields populated. We assert on the
        // first entry rather than every entry to keep the contract explicit while letting the
        // catalogue grow without churning this test.
        var first = entries![0];
        first.Title.Should().NotBeNullOrWhiteSpace();
        first.Description.Should().NotBeNullOrWhiteSpace();
        first.Register.Should().NotBeNullOrWhiteSpace();
        first.Format.Should().NotBeNullOrWhiteSpace();
        first.Url.Should().NotBeNullOrWhiteSpace();
        // At least one entry must be gzip-compressed (the bulk Enheter/Underenheter JSON dumps).
        entries.Should().Contain(e => e.Compressed);
    }

    [Fact]
    public async Task GetDownloads_AllUrlsAreAbsoluteAndPointAtBrreg()
    {
        using var factory = new WebApiFactory();
        factory.BulkDownloads.GetAll().Returns(SampleCatalogue);
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/downloads", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<BulkDownload>>();
        entries.Should().NotBeNull();
        entries!.Should().AllSatisfy(e =>
        {
            Uri.TryCreate(e.Url, UriKind.Absolute, out var uri).Should().BeTrue();
            uri!.Host.Should().Be("data.brreg.no");
        });
    }
}
