// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text.Json;
using System.Xml;
using Bronnoysund.Application.International;
using Bronnoysund.Application.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace Bronnoysund.Application.Tests;

/// <summary>
/// Pins the exception → CompanyLookupResult policy. Every adapter delegates to this
/// helper, so a regression here cascades to all 11 country adapters.
/// </summary>
public class ProviderExceptionTranslatorTests
{
    [Fact]
    public async Task NoException_PassesResultThrough()
    {
        var found = new CompanyLookupResult.Found(new Application.Dtos.CompanyResponse("X", "Acme", "AS", "Unknown"));
        var result = await ProviderExceptionTranslator.CatchUpstreamAsync(
            () => Task.FromResult<CompanyLookupResult>(found),
            "Test", "id", NullLogger.Instance, CancellationToken.None);
        result.Should().BeSameAs(found);
    }

    [Fact]
    public async Task BrokenCircuitException_ReturnsUnavailable_WithoutLeakingInnerMessage()
    {
        var result = await ProviderExceptionTranslator.CatchUpstreamAsync(
            () => throw new BrokenCircuitException("inner detail"),
            "Sweden (Bolagsverket)", "5560360793", NullLogger.Instance, CancellationToken.None);

        var unavailable = result.Should().BeOfType<CompanyLookupResult.Unavailable>().Subject;
        unavailable.Message.Should().Contain("Sweden (Bolagsverket)");
        unavailable.Message.Should().Contain("temporarily unavailable");
        unavailable.Message.Should().NotContain("inner detail");
    }

    [Fact]
    public async Task TimeoutRejectedException_ReturnsUnavailable()
    {
        var result = await ProviderExceptionTranslator.CatchUpstreamAsync(
            () => throw new TimeoutRejectedException("Polly attempt timed out"),
            "Estonia (RIK)", "12417834", NullLogger.Instance, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>()
            .Which.Message.Should().Contain("resilience-handler timeout");
    }

    [Fact]
    public async Task JsonException_ReturnsUnavailable()
    {
        var result = await ProviderExceptionTranslator.CatchUpstreamAsync(
            () => throw new JsonException("malformed"),
            "Greece (GEMI)", "094019245", NullLogger.Instance, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>()
            .Which.Message.Should().Contain("malformed JSON");
    }

    [Fact]
    public async Task XmlException_ReturnsUnavailable()
    {
        var result = await ProviderExceptionTranslator.CatchUpstreamAsync(
            () => throw new XmlException("bad SOAP"),
            "Estonia (RIK)", "12417834", NullLogger.Instance, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>()
            .Which.Message.Should().Contain("malformed XML");
    }

    [Fact]
    public async Task HttpRequestException_ReturnsUnavailableWithMessage()
    {
        var result = await ProviderExceptionTranslator.CatchUpstreamAsync(
            () => throw new HttpRequestException("dns failure"),
            "Croatia (Sudski Registar)", "27759560625", NullLogger.Instance, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>()
            .Which.Message.Should().Contain("Croatia (Sudski Registar)");
    }

    [Fact]
    public async Task TaskCanceledException_WithoutCallerCancellation_ReturnsUnavailable()
    {
        var result = await ProviderExceptionTranslator.CatchUpstreamAsync(
            () => throw new TaskCanceledException(),
            "Slovenia (AJPES)", "5043611000", NullLogger.Instance, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>()
            .Which.Message.Should().Contain("Slovenia (AJPES)");
    }

    [Fact]
    public async Task TaskCanceledException_WithCallerCancellation_Propagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await ProviderExceptionTranslator.CatchUpstreamAsync(
            () => throw new TaskCanceledException(),
            "Ireland (CRO)", "408059", NullLogger.Instance, cts.Token);

        // Caller cancellations must propagate so upstream cooperative cancellation works.
        await act.Should().ThrowAsync<TaskCanceledException>();
    }
}
