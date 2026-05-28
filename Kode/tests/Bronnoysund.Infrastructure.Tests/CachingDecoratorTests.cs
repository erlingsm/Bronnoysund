// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// Smoke tests for the four HybridCache decorators added per F5. Each test verifies that a
/// repeated call with the same key hits the cache rather than the inner provider, which is
/// the only behavior worth asserting for a thin decorator — actual TTL expiry is HybridCache's
/// responsibility, not ours.
/// </summary>
public class CachingDecoratorTests
{
    private static readonly OrganizationNumber Org = OrganizationNumber.Create("974760843");

    private static HybridCache CreateCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<HybridCache>();
    }

    [Fact]
    public async Task CachingSubUnitDetailsProvider_OnlyCallsInnerOnce_ForSameOrgnr()
    {
        var inner = Substitute.For<ISubUnitDetailsProvider>();
        inner.LookupAsync(Org, Arg.Any<CancellationToken>())
            .Returns(new SubUnitLookupResult.Found(SampleSubUnit()));
        var sut = new CachingSubUnitDetailsProvider(inner, CreateCache(),
            NullLogger<CachingSubUnitDetailsProvider>.Instance);

        await sut.LookupAsync(Org, CancellationToken.None);
        await sut.LookupAsync(Org, CancellationToken.None);
        await sut.LookupAsync(Org, CancellationToken.None);

        await inner.Received(1).LookupAsync(Org, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CachingLegalRolesProvider_OnlyCallsInnerOnce_ForSameOrgnr()
    {
        var inner = Substitute.For<ILegalRolesProvider>();
        inner.GetLegalRolesAsync(Org, Arg.Any<CancellationToken>())
            .Returns(new LegalRolesLookupResult.Found(new LegalRolesResponse(Org.Value, false, [])));
        var sut = new CachingLegalRolesProvider(inner, CreateCache(),
            NullLogger<CachingLegalRolesProvider>.Instance);

        await sut.GetLegalRolesAsync(Org, CancellationToken.None);
        await sut.GetLegalRolesAsync(Org, CancellationToken.None);

        await inner.Received(1).GetLegalRolesAsync(Org, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CachingVoluntaryOrganizationProvider_OnlyCallsInnerOnce_ForSameOrgnr()
    {
        var inner = Substitute.For<IVoluntaryOrganizationProvider>();
        inner.LookupAsync(Org, Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationLookupResult.NotRegistered(Org.Value));
        var sut = new CachingVoluntaryOrganizationProvider(inner, CreateCache(),
            NullLogger<CachingVoluntaryOrganizationProvider>.Instance);

        await sut.LookupAsync(Org, CancellationToken.None);
        await sut.LookupAsync(Org, CancellationToken.None);

        await inner.Received(1).LookupAsync(Org, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CachingEntityChangesProvider_OnlyCallsInnerOnce_ForSameOrgnrAndPageSize()
    {
        var inner = Substitute.For<IEntityChangesProvider>();
        inner.GetChangesAsync(Org, 20, Arg.Any<CancellationToken>())
            .Returns(new EntityChangesResponse(
                Org.Value,
                new EntityChangesFeed([]),
                new EntityChangesFeed([]),
                new EntityChangesFeed([])));
        var sut = new CachingEntityChangesProvider(inner, CreateCache(),
            NullLogger<CachingEntityChangesProvider>.Instance);

        await sut.GetChangesAsync(Org, 20, CancellationToken.None);
        await sut.GetChangesAsync(Org, 20, CancellationToken.None);

        await inner.Received(1).GetChangesAsync(Org, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CachingEntityChangesProvider_SeparatesCacheEntries_ByPageSize()
    {
        // Different page-size → different cache key → second call must still hit the inner.
        var inner = Substitute.For<IEntityChangesProvider>();
        inner.GetChangesAsync(Org, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => new EntityChangesResponse(
                Org.Value,
                new EntityChangesFeed([]),
                new EntityChangesFeed([]),
                new EntityChangesFeed([])));
        var sut = new CachingEntityChangesProvider(inner, CreateCache(),
            NullLogger<CachingEntityChangesProvider>.Instance);

        await sut.GetChangesAsync(Org, 20, CancellationToken.None);
        await sut.GetChangesAsync(Org, 50, CancellationToken.None);

        await inner.Received(1).GetChangesAsync(Org, 20, Arg.Any<CancellationToken>());
        await inner.Received(1).GetChangesAsync(Org, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CachingKodeverkProvider_GetOrganisasjonsformer_OnlyCallsInnerOnce()
    {
        var inner = Substitute.For<IKodeverkProvider>();
        inner.GetOrganisasjonsformerAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Found([new KodeverkEntry("AS", "Aksjeselskap")]));
        var sut = new CachingKodeverkProvider(inner, CreateCache(),
            NullLogger<CachingKodeverkProvider>.Instance);

        await sut.GetOrganisasjonsformerAsync(CancellationToken.None);
        await sut.GetOrganisasjonsformerAsync(CancellationToken.None);
        await sut.GetOrganisasjonsformerAsync(CancellationToken.None);

        await inner.Received(1).GetOrganisasjonsformerAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CachingKodeverkProvider_GetIcnpoCategories_OnlyCallsInnerOnce()
    {
        var inner = Substitute.For<IKodeverkProvider>();
        inner.GetIcnpoCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Found([new KodeverkEntry("01100", "Kultur")]));
        var sut = new CachingKodeverkProvider(inner, CreateCache(),
            NullLogger<CachingKodeverkProvider>.Instance);

        await sut.GetIcnpoCategoriesAsync(CancellationToken.None);
        await sut.GetIcnpoCategoriesAsync(CancellationToken.None);

        await inner.Received(1).GetIcnpoCategoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CachingKodeverkProvider_GetVoluntaryInformationTypes_OnlyCallsInnerOnce()
    {
        var inner = Substitute.For<IKodeverkProvider>();
        inner.GetVoluntaryInformationTypesAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Found([new KodeverkEntry("VEDTEKTER", "Vedtekter")]));
        var sut = new CachingKodeverkProvider(inner, CreateCache(),
            NullLogger<CachingKodeverkProvider>.Instance);

        await sut.GetVoluntaryInformationTypesAsync(CancellationToken.None);
        await sut.GetVoluntaryInformationTypesAsync(CancellationToken.None);

        await inner.Received(1).GetVoluntaryInformationTypesAsync(Arg.Any<CancellationToken>());
    }

    private static SubUnitDetailsResponse SampleSubUnit() => new(
        OrganizationNumber: Org.Value,
        OrganizationName: "Sample",
        ParentOrganizationNumber: null);
}
