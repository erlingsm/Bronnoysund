// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Poland;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// Pins the P→S register fallback semantics added in commit fd00284:
///   * P=Found             → Found
///   * P=404 + S=Found     → Found
///   * P=404 + S=404       → NotFound
///   * P=transient + S=404 → Unavailable (we don't know whether P holds the company)
///   * P=transient + S=transient → Unavailable
/// </summary>
public class PolishCompanyProviderKrsFallbackTests : IDisposable
{
    private readonly QueueHttpHandler _krsHandler = new();
    private readonly HttpClient _krsHttp;
    private readonly HttpClient _ceidgHttp;
    private readonly PolishCompanyProvider _sut;
    private static readonly PolishKrsNumber Orlen = PolishKrsNumber.Create("0000028860");
    private const string FakeKrsPayload =
        "{\"odpis\":{\"dane\":{\"dzial1\":{\"danePodmiotu\":{\"nazwa\":\"ORLEN SPÓŁKA AKCYJNA\"}}}}}";

    public PolishCompanyProviderKrsFallbackTests()
    {
        _krsHttp = new HttpClient(_krsHandler) { BaseAddress = new Uri("https://api-krs.ms.gov.pl/") };
        _ceidgHttp = new HttpClient(new QueueHttpHandler()) { BaseAddress = new Uri("https://dane.biznes.gov.pl/") };
        var opts = Options.Create(new PolandOptions());
        _sut = new PolishCompanyProvider(_krsHttp, _ceidgHttp, opts, NullLogger<PolishCompanyProvider>.Instance);
    }

    [Fact]
    public async Task PFound_ReturnsFoundWithoutTryingS()
    {
        _krsHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(FakeKrsPayload) });

        var result = await _sut.LookupAsync(Orlen, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Found>()
            .Which.Company.OrganizationName.Should().Contain("ORLEN");
        _krsHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task P404_S404_ReturnsNotFound()
    {
        _krsHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));
        _krsHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _sut.LookupAsync(Orlen, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.NotFound>();
        _krsHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task P404_SFound_ReturnsFound()
    {
        _krsHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));
        _krsHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(FakeKrsPayload) });

        var result = await _sut.LookupAsync(Orlen, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Found>();
        _krsHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task PTransient_S404_ReturnsUnavailable()
    {
        // P transient → can't confirm "not in P" → S=404 alone isn't enough to claim
        // NotFound (the company might exist in P). This is the iter-2 review false-
        // positive; the documented semantics in PolishCompanyProvider lock this in.
        _krsHandler.EnqueueException(new HttpRequestException("dns blip"));
        _krsHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _sut.LookupAsync(Orlen, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>();
        _krsHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task PTransient_STransient_ReturnsUnavailable()
    {
        _krsHandler.EnqueueException(new HttpRequestException("dns blip P"));
        _krsHandler.EnqueueException(new HttpRequestException("dns blip S"));

        var result = await _sut.LookupAsync(Orlen, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>();
        _krsHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task PTransient_SFound_ReturnsFound()
    {
        _krsHandler.EnqueueException(new HttpRequestException("dns blip"));
        _krsHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(FakeKrsPayload) });

        var result = await _sut.LookupAsync(Orlen, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Found>();
        _krsHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task P404_STransient_ReturnsUnavailable()
    {
        // The missing seventh row of the truth table that iter-3 caught. P=404 says
        // "not in P-register"; S=transient says "we couldn't check S". Combined we
        // can't claim NotFound (the entity might be in S) — must Unavailable.
        _krsHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));
        _krsHandler.EnqueueException(new HttpRequestException("dns blip S"));

        var result = await _sut.LookupAsync(Orlen, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>();
        _krsHandler.RequestCount.Should().Be(2);
    }

    public void Dispose()
    {
        _krsHttp.Dispose();
        _ceidgHttp.Dispose();
        _krsHandler.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class QueueHttpHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses = new();
        public int RequestCount { get; private set; }

        public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(() => response);

        public void EnqueueException(Exception ex) =>
            _responses.Enqueue(() => throw ex);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
            return Task.FromResult(_responses.Dequeue()());
        }
    }
}
