// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Bronnoysund.WebApi.Tests.Infrastructure;

/// <summary>
/// Test host for the WebApi. Swaps the real Brreg-backed providers for NSubstitute fakes that
/// tests can program per-case via <see cref="WithProviders"/>. Each factory instance gets its
/// own SQLite file in a tmp directory so EF Core <c>EnsureCreatedAsync</c> can run without
/// touching the developer's LocalApplicationData.
/// </summary>
/// <remarks>
/// We mark the factory as <see cref="IDisposable"/>-via-base; xUnit's <see cref="IClassFixture{T}"/>
/// pattern would let us share the host across tests, but each endpoint test wants its own fakes,
/// so individual tests construct the factory directly. <c>BRONNOYSUND_DB_PATH</c> is set via the
/// configuration override so <see cref="DefaultDatabasePathProvider"/> picks up a per-instance tmp file.
/// </remarks>
public sealed class WebApiFactory : WebApplicationFactory<Program>
{
    public ICompanyProvider Company { get; } = Substitute.For<ICompanyProvider>();
    public ICompanySearchProvider Search { get; } = Substitute.For<ICompanySearchProvider>();
    public ISubUnitDetailsProvider SubUnitDetails { get; } = Substitute.For<ISubUnitDetailsProvider>();
    public ILegalRolesProvider LegalRoles { get; } = Substitute.For<ILegalRolesProvider>();
    public IEntityChangesProvider EntityChanges { get; } = Substitute.For<IEntityChangesProvider>();
    public IVoluntaryOrganizationProvider Voluntary { get; } = Substitute.For<IVoluntaryOrganizationProvider>();
    public IVoluntaryOrganizationSearchProvider VoluntarySearch { get; } = Substitute.For<IVoluntaryOrganizationSearchProvider>();
    public IKodeverkProvider Kodeverk { get; } = Substitute.For<IKodeverkProvider>();
    public IBrregStatisticsProvider Statistics { get; } = Substitute.For<IBrregStatisticsProvider>();
    public IMatrikkelenhetProvider Matrikkelenhet { get; } = Substitute.For<IMatrikkelenhetProvider>();
    public IBulkDownloadCatalog BulkDownloads { get; } = Substitute.For<IBulkDownloadCatalog>();

    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"bronnoysund-webapi-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Force DataSource:Mode=Direct so all six provider ports are registered; we then swap
        // them out below. Pointing the DB at a unique tmp file keeps EnsureCreatedAsync happy
        // without sharing state across test classes.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataSource:Mode"] = "Direct",
                ["BRONNOYSUND_DB_PATH"] = _dbPath,
            });
        });

        // Mvc.Testing reads BRONNOYSUND_DB_PATH from the process env var via
        // DefaultDatabasePathProvider, but the WebApi Program.cs uses both env + config
        // (see DefaultDatabasePathProvider). Setting the env var ensures the path provider
        // sees the tmp file even before configuration is wired up.
        Environment.SetEnvironmentVariable("BRONNOYSUND_DB_PATH", _dbPath);

        builder.ConfigureServices(services =>
        {
            ReplaceSingleton<ICompanyProvider>(services, Company);
            ReplaceSingleton<ICompanySearchProvider>(services, Search);
            ReplaceSingleton<ISubUnitDetailsProvider>(services, SubUnitDetails);
            ReplaceSingleton<ILegalRolesProvider>(services, LegalRoles);
            ReplaceSingleton<IEntityChangesProvider>(services, EntityChanges);
            ReplaceSingleton<IVoluntaryOrganizationProvider>(services, Voluntary);
            ReplaceSingleton<IVoluntaryOrganizationSearchProvider>(services, VoluntarySearch);
            ReplaceSingleton<IKodeverkProvider>(services, Kodeverk);
            ReplaceSingleton<IBrregStatisticsProvider>(services, Statistics);
            ReplaceSingleton<IMatrikkelenhetProvider>(services, Matrikkelenhet);
            ReplaceSingleton<IBulkDownloadCatalog>(services, BulkDownloads);
        });
    }

    private static void ReplaceSingleton<TService>(IServiceCollection services, TService implementation)
        where TService : class
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(TService)).ToList();
        foreach (var d in descriptors)
        {
            services.Remove(d);
        }
        services.AddSingleton(implementation);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); }
            catch (IOException) { /* best-effort cleanup */ }
        }
    }
}
