// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Finland.Generated;
using Bronnoysund.Infrastructure.Finland.Prh;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// Opt-in smoke test that hits the real PRH/YTJ open-data API. Skipped by default so CI and
/// local <c>dotnet test</c> do not call the upstream service on every run. To execute:
/// remove the <c>Skip = </c> argument and run the single test, e.g.
/// <c>dotnet test --filter FullyQualifiedName~PrhCompanyProviderLiveSmokeTests</c>.
/// The Trait makes it filterable once CI grows a "Category!=LiveSmoke" exclusion.
/// </summary>
[Trait("Category", "LiveSmoke")]
public class PrhCompanyProviderLiveSmokeTests
{
    [Fact(Skip = "Live PRH call — remove Skip to run manually")]
    public async Task Lookup_RealNokia_ReturnsFoundWithExpectedName()
    {
        using var http = new HttpClient { BaseAddress = new Uri("https://avoindata.prh.fi/opendata-ytj-api/v3/") };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)");
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http)
        {
            BaseUrl = "https://avoindata.prh.fi/opendata-ytj-api/v3",
        };
        var sut = new PrhCompanyProvider(new PrhClient(adapter), NullLogger<PrhCompanyProvider>.Instance);

        var result = await sut.LookupAsync(FinnishBusinessId.Create("0112038-9"), CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Found>()
            .Which.Company.OrganizationName.Should().Contain("Nokia");
    }
}
