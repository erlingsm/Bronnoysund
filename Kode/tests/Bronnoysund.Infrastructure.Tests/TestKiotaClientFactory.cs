// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using WireMock.Server;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// Builds a Kiota <see cref="BrregClient"/> wired to a WireMock instance so tests can
/// stub Brreg's HTTP responses end-to-end through the Kiota stack (deserialization, error
/// translation, generated DTOs). Avoids each test having to hand-construct the adapter.
/// </summary>
internal static class TestKiotaClientFactory
{
    public static BrregClient ForWireMock(WireMockServer server)
    {
        // Mirror production DI: AutomaticDecompression on the primary handler so tests
        // exercise the same gzip path Brreg uses for endpoints like /roller/totalbestand.
        var handler = new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All };
        var http = new HttpClient(handler) { BaseAddress = new Uri(server.Url!) };
        var adapter = new HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(),
            httpClient: http)
        {
            BaseUrl = server.Url!.TrimEnd('/'),
        };
        return new BrregClient(adapter);
    }
}
