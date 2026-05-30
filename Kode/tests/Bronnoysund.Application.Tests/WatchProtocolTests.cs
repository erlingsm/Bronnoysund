// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text;
using System.Text.Json;
using Bronnoysund.Application.Watch;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Application.Tests;

public class WatchProtocolTests
{
    [Fact]
    public void Encode_FoundResponse_UsesLowerCamelCaseAndOmitsNulls()
    {
        var response = new LookupResponse(
            Version: 1,
            Result: "found",
            OrganizationNumber: "919300388",
            OrganizationName: "EQUINOR ASA",
            CompanyType: "ASA",
            LanguageForm: "Bokmål",
            CountryCode: "NO");

        var json = Encoding.UTF8.GetString(WatchProtocol.Encode(response));

        json.Should().Contain("\"organizationNumber\":\"919300388\"")
            .And.Contain("\"organizationName\":\"EQUINOR ASA\"")
            .And.Contain("\"companyType\":\"ASA\"")
            .And.Contain("\"languageForm\":\"Bokm")
            .And.Contain("\"countryCode\":\"NO\"")
            .And.NotContain("\"value\"")
            .And.NotContain("\"message\"")
            .And.NotContain("\"code\"");
    }

    [Fact]
    public void Encode_InvalidResponse_OmitsLookupFields()
    {
        var response = new LookupResponse(
            Version: 1,
            Result: "invalid",
            Code: "unrecognizedFormat",
            Message: "Ukjent format.");

        var json = Encoding.UTF8.GetString(WatchProtocol.Encode(response));

        json.Should().Contain("\"result\":\"invalid\"")
            .And.Contain("\"code\":\"unrecognizedFormat\"")
            .And.Contain("\"message\":\"Ukjent format.\"")
            .And.NotContain("\"organizationNumber\"")
            .And.NotContain("\"organizationName\"");
    }

    [Fact]
    public void TryDecode_ValidLookupRequest_ReturnsParsedRequest()
    {
        var payload = Encoding.UTF8.GetBytes(
            "{\"version\":1,\"action\":\"lookup\",\"value\":\"919300388\"}");

        var request = WatchProtocol.TryDecode(payload);

        request.Should().NotBeNull();
        request!.Version.Should().Be(1);
        request.Action.Should().Be("lookup");
        request.Value.Should().Be("919300388");
    }

    [Fact]
    public void TryDecode_GarbledPayload_ReturnsNull()
    {
        var payload = Encoding.UTF8.GetBytes("not json {{{ ");

        var request = WatchProtocol.TryDecode(payload);

        request.Should().BeNull();
    }

    [Fact]
    public void TryDecode_EmptyPayload_ReturnsNull()
    {
        var request = WatchProtocol.TryDecode(Array.Empty<byte>());

        request.Should().BeNull();
    }

    [Fact]
    public void TryDecode_ExtraFields_IgnoresThem()
    {
        var payload = Encoding.UTF8.GetBytes(
            "{\"version\":1,\"action\":\"lookup\",\"value\":\"x\",\"unknown\":42}");

        var request = WatchProtocol.TryDecode(payload);

        request.Should().NotBeNull();
        request!.Action.Should().Be("lookup");
    }

    [Fact]
    public void EncodeDecode_RoundTripsNotFoundResponse()
    {
        var original = new LookupResponse(
            Version: 1,
            Result: "notFound",
            Value: "999999999",
            Message: "Ingen treff.");

        var json = WatchProtocol.Encode(original);
        var roundTripped = JsonSerializer.Deserialize<LookupResponse>(json, WatchProtocol.JsonOptions);

        roundTripped.Should().NotBeNull();
        roundTripped!.Result.Should().Be("notFound");
        roundTripped.Value.Should().Be("999999999");
        roundTripped.Message.Should().Be("Ingen treff.");
        roundTripped.OrganizationNumber.Should().BeNull();
    }

    [Theory]
    [InlineData("lookup")]
    [InlineData("search")]
    public void LookupRequest_AllowsExpectedActionValues(string action)
    {
        var request = new LookupRequest(1, action, "x");
        var json = JsonSerializer.Serialize(request, WatchProtocol.JsonOptions);
        json.Should().Contain($"\"action\":\"{action}\"");
    }
}
