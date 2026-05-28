// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Infrastructure.Finland.Generated;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using WireMock.Server;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// Builds a Kiota <see cref="PrhClient"/> wired to a WireMock instance so Finland tests can
/// stub PRH/YTJ HTTP responses end-to-end through the Kiota stack. Mirrors
/// <see cref="TestKiotaClientFactory"/>'s Brreg version.
/// </summary>
internal static class TestPrhClientFactory
{
    public static PrhClient ForWireMock(WireMockServer server)
    {
        var http = new HttpClient { BaseAddress = new Uri(server.Url!) };
        var adapter = new HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(),
            httpClient: http)
        {
            BaseUrl = server.Url!.TrimEnd('/'),
        };
        return new PrhClient(adapter);
    }
}
