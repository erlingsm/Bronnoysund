// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.OpenCorporates;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// Pins the OpenCorporates outbound request shape: the api_token MUST travel as the
/// X-API-TOKEN header (not Authorization, not query string). Code-review-2026-05-29
/// iter-2 caught a regression where the Doorkeeper-style Authorization: Token token=…
/// scheme was used; OpenCorporates rejects that with 401. These tests prevent that
/// class of regression by asserting the actual HttpRequestMessage.
/// </summary>
public class OpenCorporatesAuthHeaderTests : IDisposable
{
    private readonly RecordingHttpHandler _handler = new();
    private readonly HttpClient _http;
    private readonly OpenCorporatesClient _sut;

    public OpenCorporatesAuthHeaderTests()
    {
        _http = new HttpClient(_handler) { BaseAddress = new Uri("https://api.opencorporates.com/") };
        var opts = Options.Create(new OpenCorporatesOptions { ApiToken = "test-token-abc" });
        _sut = new OpenCorporatesClient(_http, opts, NullLogger<OpenCorporatesClient>.Instance);
    }

    [Fact]
    public async Task Lookup_SendsXApiTokenHeader_NotAuthorization_NotQueryString()
    {
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        await _sut.LookupAsync("es", "A28015865", "ES", CancellationToken.None);

        _handler.LastRequest.Should().NotBeNull();
        _handler.LastRequest!.Headers.TryGetValues("X-API-TOKEN", out var tokenHeader).Should().BeTrue();
        tokenHeader!.Should().ContainSingle().Which.Should().Be("test-token-abc");

        _handler.LastRequest.Headers.Authorization.Should().BeNull();
        _handler.LastRequest.RequestUri!.Query.Should().NotContain("api_token");
        _handler.LastRequest.RequestUri.Query.Should().NotContain("test-token-abc");
    }

    [Fact]
    public async Task Lookup_PassesJurisdictionAndIdInPath()
    {
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        await _sut.LookupAsync("it", "00159560366", "IT", CancellationToken.None);

        _handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v0.4/companies/it/00159560366");
    }

    [Fact]
    public async Task Lookup_TokenNotConfigured_ReturnsUnavailableWithoutSendingRequest()
    {
        var opts = Options.Create(new OpenCorporatesOptions { ApiToken = string.Empty });
        var sut = new OpenCorporatesClient(_http, opts, NullLogger<OpenCorporatesClient>.Instance);

        var result = await sut.LookupAsync("es", "A28015865", "ES", CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>()
            .Which.Message.Should().Contain("OpenCorporates");
        _handler.LastRequest.Should().BeNull();
    }

    public void Dispose()
    {
        _http.Dispose();
        _handler.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class RecordingHttpHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public HttpResponseMessage NextResponse { get; set; } = new(HttpStatusCode.NotFound);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(NextResponse);
        }
    }
}
