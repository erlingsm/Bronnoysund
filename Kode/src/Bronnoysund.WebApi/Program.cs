// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Application.UseCases.LookupCompany;
using Bronnoysund.Application.UseCases.SearchCompaniesByName;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure;
using Bronnoysund.Infrastructure.Persistence;
using Bronnoysund.Infrastructure.Persistence.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var dbPathProvider = new DefaultDatabasePathProvider();
    builder.Configuration.AddSqliteSettings(() => dbPathProvider.GetDatabaseFilePath());

    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/bronnoysund-.log", rollingInterval: RollingInterval.Day));

    // Application Insights — opt-in: only wires up if APPLICATIONINSIGHTS_CONNECTION_STRING is set.
    // Locally and in tests it stays inactive; Container Apps env-var enables it in production.
    var aiConnection = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
        ?? builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(aiConnection))
    {
        builder.Services.AddApplicationInsightsTelemetry(opts => opts.ConnectionString = aiConnection);
    }

    builder.Services.AddBronnoysundApplication();
    builder.Services.AddBronnoysundInfrastructure(builder.Configuration);
    builder.Services.AddLocalization();
    builder.Services.AddSingleton<IDatabasePathProvider>(dbPathProvider);
    builder.Services.AddBronnoysundPersistence(builder.Configuration);
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    await app.Services.InitializeBronnoysundPersistenceAsync();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    // All non-2xx responses use RFC 7807 ProblemDetails via Results.Problem(...).
    // Stable URIs for `type` fields point at the relevant RFC sections so API consumers can branch
    // on a fixed identifier rather than parsing the human-readable title/detail.
    const string InvalidInputType = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
    const string NotFoundType = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4";
    const string UnavailableType = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4";
    const string BrregUnavailableTitle = "Brønnøysundregistrene (the Brønnøysund Register Centre) is temporarily unavailable";

    app.MapGet("/companies/{orgnr}", async (
        string orgnr,
        LookupCompanyHandler handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new LookupCompanyQuery(orgnr), ct);
        return result switch
        {
            CompanyLookupResult.Found f => Results.Ok(f.Company),
            CompanyLookupResult.NotFound nf => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Company not found",
                detail: $"No company with organization number {nf.OrganizationNumber} was found.",
                type: NotFoundType),
            CompanyLookupResult.InvalidInput inv => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid organization number",
                detail: inv.Message,
                type: InvalidInputType),
            CompanyLookupResult.Unavailable unav => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: unav.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/companies", async (
        string? name,
        int? size,
        SearchCompaniesByNameHandler handler,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Missing name",
                detail: "Query parameter 'name' is required.",
                type: InvalidInputType);
        }

        var result = await handler.HandleAsync(new SearchCompaniesByNameQuery(name, size), ct);
        return result switch
        {
            SearchCompaniesByNameResult.Found f => Results.Ok(f.Result),
            SearchCompaniesByNameResult.InvalidInput inv => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid input",
                detail: inv.Message,
                type: InvalidInputType),
            SearchCompaniesByNameResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type."),
        };
    });

    app.MapGet("/companies/{orgnr}/aggregated", async (
        string orgnr,
        string? include,
        ICompanyDataAggregator aggregator,
        CancellationToken ct) =>
    {
        if (!OrganizationNumber.TryCreate(orgnr, out var org, out var error))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid organization number",
                detail: error,
                type: InvalidInputType);
        }

        var core = await aggregator.CoreOnlyAsync(org, ct);
        switch (core)
        {
            case CompanyLookupResult.NotFound nf:
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Company not found",
                    detail: $"No company with organization number {nf.OrganizationNumber} was found.",
                    type: NotFoundType);
            case CompanyLookupResult.Unavailable u:
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: BrregUnavailableTitle,
                    detail: u.Message,
                    type: UnavailableType);
            case CompanyLookupResult.InvalidInput inv:
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid organization number",
                    detail: inv.Message,
                    type: InvalidInputType);
        }

        // ?include=enrichment opts into the four lazy-loaded providers (sub-unit details,
        // legal roles, voluntary status, change feed). Default stays core-only to preserve
        // the existing contract — callers that need the heavier payload must ask for it.
        var scope = AggregatedScopeParser.Parse(include);
        var aggregated = await aggregator.AggregateAsync(org, scope, ct);
        return Results.Ok(aggregated);
    });

    app.MapGet("/sub-units/{orgnr}", async (
        string orgnr,
        ISubUnitDetailsProvider provider,
        CancellationToken ct) =>
    {
        if (!OrganizationNumber.TryCreate(orgnr, out var org, out var error))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid organization number",
                detail: error,
                type: InvalidInputType);
        }

        var result = await provider.LookupAsync(org, ct);
        return result switch
        {
            SubUnitLookupResult.Found f => Results.Ok(f.SubUnit),
            SubUnitLookupResult.NotFound nf => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Sub-unit not found",
                detail: $"No sub-unit with organization number {nf.OrganizationNumber} was found.",
                type: NotFoundType),
            SubUnitLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/companies/{orgnr}/legal-roles", async (
        string orgnr,
        ILegalRolesProvider provider,
        CancellationToken ct) =>
    {
        if (!OrganizationNumber.TryCreate(orgnr, out var org, out var error))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid organization number",
                detail: error,
                type: InvalidInputType);
        }

        var result = await provider.GetLegalRolesAsync(org, ct);
        return result switch
        {
            LegalRolesLookupResult.Found f => Results.Ok(f.Roles),
            LegalRolesLookupResult.NotFound nf => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Legal roles not found",
                detail: $"No legal roles found for organization number {nf.OrganizationNumber}.",
                type: NotFoundType),
            LegalRolesLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/companies/{orgnr}/changes", async (
        string orgnr,
        int? size,
        ICompanyProvider companyProvider,
        IEntityChangesProvider provider,
        CancellationToken ct) =>
    {
        if (!OrganizationNumber.TryCreate(orgnr, out var org, out var error))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid organization number",
                detail: error,
                type: InvalidInputType);
        }

        // The Brreg /oppdateringer/* feeds return 200 OK with empty lists for unknown orgnrs,
        // so we cannot distinguish "no changes" from "no such entity" by hitting them directly.
        // Validate existence against ICompanyProvider first — CachingCompanyProvider already
        // caches lookups so this is normally free on a cache-hit path.
        var existence = await companyProvider.LookupAsync(org, ct);
        switch (existence)
        {
            case CompanyLookupResult.NotFound nf:
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Company not found",
                    detail: $"No company with organization number {nf.OrganizationNumber} was found.",
                    type: NotFoundType);
            case CompanyLookupResult.Unavailable u:
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: BrregUnavailableTitle,
                    detail: u.Message,
                    type: UnavailableType);
            case CompanyLookupResult.InvalidInput inv:
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid organization number",
                    detail: inv.Message,
                    type: InvalidInputType);
        }

        var pageSize = Math.Clamp(size ?? 20, 1, 100);
        var result = await provider.GetChangesAsync(org, pageSize, ct);
        return Results.Ok(result);
    });

    app.MapGet("/voluntary-organizations/{orgnr}", async (
        string orgnr,
        IVoluntaryOrganizationProvider provider,
        CancellationToken ct) =>
    {
        if (!OrganizationNumber.TryCreate(orgnr, out var org, out var error))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid organization number",
                detail: error,
                type: InvalidInputType);
        }

        var result = await provider.LookupAsync(org, ct);
        return result switch
        {
            VoluntaryOrganizationLookupResult.Found f => Results.Ok(f.Organization),
            VoluntaryOrganizationLookupResult.NotRegistered nr => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not registered in Frivillighetsregisteret",
                detail: $"Organization {nr.OrganizationNumber} is not registered in Frivillighetsregisteret.",
                type: NotFoundType),
            VoluntaryOrganizationLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Frivillighetsregisteret is temporarily unavailable",
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/voluntary-organizations", async (
        int? size,
        string? searchAfter,
        IVoluntaryOrganizationSearchProvider provider,
        CancellationToken ct) =>
    {
        // M10: defensively validate searchAfter so obviously-bad cursors (control chars,
        // excessive length, unexpected characters) fail at our edge with 400 instead of
        // being forwarded to Brreg. Brreg's own cursor is the last orgnr seen which is
        // 9 digits, but we leave room for base64 padding chars so future cursor shapes
        // (or URL-encoded round-trips) still pass.
        if (!SearchAfterValidator.IsValid(searchAfter))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid input",
                detail: "searchAfter must be 1-64 characters of [A-Za-z0-9+/=_-].",
                type: InvalidInputType);
        }

        var query = new VoluntaryOrganizationSearchQuery(
            Size: size ?? 20,
            SearchAfter: searchAfter);

        var result = await provider.SearchAsync(query, ct);
        return result switch
        {
            VoluntaryOrganizationSearchResult.Found f => Results.Ok(f.Result),
            VoluntaryOrganizationSearchResult.InvalidInput inv => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid input",
                detail: inv.Message,
                type: InvalidInputType),
            VoluntaryOrganizationSearchResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Frivillighetsregisteret is temporarily unavailable",
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/icnpo-kategorier", async (
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        var result = await provider.GetIcnpoCategoriesAsync(ct);
        return result switch
        {
            KodeverkLookupResult.Found f => Results.Ok(f.Entries),
            KodeverkLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Frivillighetsregisteret is temporarily unavailable",
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/informasjonstyper", async (
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        var result = await provider.GetVoluntaryInformationTypesAsync(ct);
        return result switch
        {
            KodeverkLookupResult.Found f => Results.Ok(f.Entries),
            KodeverkLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Frivillighetsregisteret is temporarily unavailable",
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/kommuner", async (
        int? page,
        int? size,
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        var pageValue = Math.Max(0, page ?? 0);
        var sizeValue = Math.Clamp(size ?? 100, 1, 100);
        var result = await provider.GetKommunerAsync(pageValue, sizeValue, ct);
        return result switch
        {
            KodeverkPagedResult.Found f => Results.Ok(new
            {
                entries = f.Entries,
                page = f.Page,
                size = f.Size,
                totalElements = f.TotalElements,
                totalPages = f.TotalPages,
            }),
            KodeverkPagedResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/kommuner/{kommunenummer}", async (
        string kommunenummer,
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        // H11: Reject obviously-malformed kommunenummer at our edge so Brreg is not contacted
        // for inputs that cannot possibly match. Today's catalogue is 4 digits (e.g. 0301),
        // but historic codes have used 2 digits, so we accept 2-4 digits.
        if (!KodeverkRouteValidator.IsValidKommunenummer(kommunenummer))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid input",
                detail: "kommunenummer must be 2-4 digits.",
                type: InvalidInputType);
        }

        var result = await provider.GetKommuneAsync(kommunenummer, ct);
        return result switch
        {
            KodeverkSingleResult.Found f => Results.Ok(f.Entry),
            KodeverkSingleResult.NotFound nf => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kommune not found",
                detail: $"No Kommune with number {nf.Code} was found.",
                type: NotFoundType),
            KodeverkSingleResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/rolletyper", async (
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        var result = await provider.GetRolletyperAsync(ct);
        return result switch
        {
            KodeverkLookupResult.Found f => Results.Ok(f.Entries),
            KodeverkLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/rollegruppetyper", async (
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        var result = await provider.GetRollegruppetyperAsync(ct);
        return result switch
        {
            KodeverkLookupResult.Found f => Results.Ok(f.Entries),
            KodeverkLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/representanter", async (
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        var result = await provider.GetRepresentanterAsync(ct);
        return result switch
        {
            KodeverkLookupResult.Found f => Results.Ok(f.Entries),
            KodeverkLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/organisasjonsformer/{kode}", async (
        string kode,
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        // H11: Organisasjonsform codes are short alphanumeric tokens (e.g. AS, ENK, FLI,
        // BEDR). Reject lower-case / punctuation / overlong inputs at the edge so we do not
        // forward malformed paths to Brreg.
        if (!KodeverkRouteValidator.IsValidOrganisasjonsformKode(kode))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid input",
                detail: "organisasjonsform code must be 2-6 upper-case letters or digits.",
                type: InvalidInputType);
        }

        var result = await provider.GetOrganisasjonsformAsync(kode, ct);
        return result switch
        {
            KodeverkSingleResult.Found f => Results.Ok(f.Entry),
            KodeverkSingleResult.NotFound nf => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Organisasjonsform not found",
                detail: $"No Organisasjonsform with code {nf.Code} was found.",
                type: NotFoundType),
            KodeverkSingleResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/organisasjonsformer-med-enheter", async (
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        var result = await provider.GetOrganisasjonsformerWithEnheterAsync(ct);
        return result switch
        {
            KodeverkLookupResult.Found f => Results.Ok(f.Entries),
            KodeverkLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/kodeverk/organisasjonsformer-med-underenheter", async (
        IKodeverkProvider provider,
        CancellationToken ct) =>
    {
        var result = await provider.GetOrganisasjonsformerWithUnderenheterAsync(ct);
        return result switch
        {
            KodeverkLookupResult.Found f => Results.Ok(f.Entries),
            KodeverkLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    // H12: The Found arm wraps the scalar in { totalCount: <long> } so the response stays a
    // JSON object (consistent with the rest of the API). Raw scalars at the top level are valid
    // JSON but harder to consume from typed clients (System.Text.Json deserialises them as a
    // root long, which makes adding sibling fields later a breaking change). Wrapping leaves
    // the door open for adding metadata (e.g. lastUpdated) without versioning.
    app.MapGet("/statistics/roles-total-count", async (
        IBrregStatisticsProvider provider,
        CancellationToken ct) =>
    {
        var result = await provider.GetRolesTotalCountAsync(ct);
        return result switch
        {
            RolesTotalCountResult.Found f => Results.Ok(new { totalCount = f.TotalCount }),
            RolesTotalCountResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    // D1: Bulk-download catalogue. Metadata only — clients download bytes directly from
    // Brreg, since our WebApi has no business proxying 10s-of-megabyte dumps.
    app.MapGet("/downloads", (IBulkDownloadCatalog catalog) => Results.Ok(catalog.GetAll()));

    // D2: Matrikkelenhet lookup. Caller supplies exactly one of matrikkelenhetid or
    // matrikkelnummer; the adapter enforces that and surfaces an early 400 via
    // MatrikkelenhetLookupResult.InvalidInput.
    app.MapGet("/matrikkelenhet", async (
        string? matrikkelenhetid,
        string? matrikkelnummer,
        IMatrikkelenhetProvider provider,
        CancellationToken ct) =>
    {
        // I2: edge-level format validation (mirrors H11 for /kodeverk). Reject malformed
        // values before the both-or-neither check in the adapter so callers get a precise
        // 400 ("must be N digits") rather than the generic "provide exactly one" message
        // when the bad input also happens to be the only filter set.
        if (!MatrikkelenhetQueryValidator.IsValidMatrikkelenhetId(matrikkelenhetid))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid input",
                detail: "matrikkelenhetid must be 1-64 characters of [A-Za-z0-9_-].",
                type: InvalidInputType);
        }
        if (!MatrikkelenhetQueryValidator.IsValidMatrikkelnummer(matrikkelnummer))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid input",
                detail: "matrikkelnummer must match the format kommune-gnr/bnr or kommune-gnr/bnr/fnr (e.g. 0301-1/1).",
                type: InvalidInputType);
        }

        var query = new MatrikkelenhetQuery(
            MatrikkelenhetId: matrikkelenhetid,
            Matrikkelnummer: matrikkelnummer);

        var result = await provider.LookupAsync(query, ct);
        return result switch
        {
            MatrikkelenhetLookupResult.Found f => Results.Ok(f.Matrikkelenheter),
            MatrikkelenhetLookupResult.NotFound nf => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Matrikkelenhet not found",
                detail: $"No matrikkelenhet matches query {nf.Query}.",
                type: NotFoundType),
            MatrikkelenhetLookupResult.InvalidInput inv => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid input",
                detail: inv.Message,
                type: InvalidInputType),
            MatrikkelenhetLookupResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: BrregUnavailableTitle,
                detail: u.Message,
                type: UnavailableType),
            _ => Results.Problem("Unexpected result type.")
        };
    });

    app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Bronnoysund.WebApi" }));

    // Per Plan 21, a single endpoint that enumerates every ICompanyProvider currently wired
    // up. Plan 26 trinn B2 (UI status-prikk grønn/grå/rød) reads ConfigurationStatus so it
    // can tell which countries are wired-and-configured (green) vs wired-but-not-configured
    // (grey) without resolving every per-country Options class itself. Plan 26 Fase 4
    // extends each country with the last-success/last-failure timestamps so the UI can
    // turn the dot red when a configured adapter is currently failing.
    app.MapGet("/health/providers", (ICompanyProviderRegistry registry, IProviderHealthTracker health) =>
    {
        var status = registry.ConfigurationStatus;
        var snapshot = health.Snapshot;
        return Results.Ok(new
        {
            supportedCountries = registry.SupportedCountries.OrderBy(c => c).ToArray(),
            countries = registry.SupportedCountries
                .OrderBy(c => c)
                .Select(c =>
                {
                    snapshot.TryGetValue(c, out var hs);
                    return new
                    {
                        countryCode = c,
                        isConfigured = status.TryGetValue(c, out var configured) && configured,
                        lastSuccess = hs?.LastSuccess,
                        lastFailure = hs?.LastFailure,
                        lastFailureReason = hs?.LastFailureReason,
                        isFailingNow = hs?.IsFailingNow ?? false,
                    };
                })
                .ToArray(),
        });
    });

    Log.Information("Bronnoysund.WebApi starting");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bronnoysund.WebApi crashed during startup");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Maps the optional <c>?include=</c> query parameter on the aggregated endpoint to an
/// <see cref="AggregatedScope"/>. Unknown values fall back to <see cref="AggregatedScope.CoreOnly"/>
/// to avoid breaking older clients sending stale values.
/// </summary>
internal static class AggregatedScopeParser
{
    public static AggregatedScope Parse(string? include)
    {
        if (string.IsNullOrWhiteSpace(include))
        {
            return AggregatedScope.CoreOnly;
        }
        return include.Trim().ToLowerInvariant() switch
        {
            "enrichment" => AggregatedScope.IncludeEnrichment,
            "full" => AggregatedScope.Full,
            _ => AggregatedScope.CoreOnly,
        };
    }
}

/// <summary>
/// Defensive validation for the <c>searchAfter</c> cursor on the voluntary-organizations
/// search endpoint. Brreg's documented cursor shape is the previous page's last orgnr
/// (9 digits) but we widen to base64-safe characters so the validator stays forward-compatible
/// if the cursor shape changes upstream.
/// </summary>
internal static class SearchAfterValidator
{
    public static bool IsValid(string? value)
    {
        if (value is null)
        {
            return true;
        }
        if (value.Length is 0 or > 64)
        {
            return false;
        }
        foreach (var c in value)
        {
            var ok = (c >= '0' && c <= '9')
                || (c >= 'A' && c <= 'Z')
                || (c >= 'a' && c <= 'z')
                || c == '+' || c == '/' || c == '=' || c == '_' || c == '-';
            if (!ok)
            {
                return false;
            }
        }
        return true;
    }
}

/// <summary>
/// Edge-level format validation for the kodeverk single-entity endpoints. Both regexes are
/// generated at compile time (no per-request allocation) and stay deliberately liberal:
/// stricter validation lives upstream at Brreg, so this layer only blocks inputs that cannot
/// possibly match a real code. Used by <c>/kodeverk/kommuner/{kommunenummer}</c> and
/// <c>/kodeverk/organisasjonsformer/{kode}</c>.
/// </summary>
internal static partial class KodeverkRouteValidator
{
    [System.Text.RegularExpressions.GeneratedRegex("^\\d{2,4}$")]
    private static partial System.Text.RegularExpressions.Regex KommunenummerPattern();

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Z0-9]{2,6}$")]
    private static partial System.Text.RegularExpressions.Regex OrganisasjonsformKodePattern();

    public static bool IsValidKommunenummer(string? value)
        => !string.IsNullOrEmpty(value) && KommunenummerPattern().IsMatch(value);

    public static bool IsValidOrganisasjonsformKode(string? value)
        => !string.IsNullOrEmpty(value) && OrganisasjonsformKodePattern().IsMatch(value);
}

/// <summary>
/// I2: Edge-level format validation for the <c>/matrikkelenhet</c> query parameters. Both
/// validators return <c>true</c> for null/empty inputs so the begge-eller-ingen rule in the
/// adapter can still produce the canonical "provide exactly one" InvalidInput message —
/// these regexes only reject values that are present but malformed.
/// </summary>
internal static partial class MatrikkelenhetQueryValidator
{
    // Brreg's internal matrikkelenhetid is typically UUID-shaped but we stay liberal:
    // 1-64 characters of letters, digits, underscores, and hyphens covers UUIDs (with or
    // without dashes), opaque tokens, and short test ids.
    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial System.Text.RegularExpressions.Regex MatrikkelenhetIdPattern();

    // Matrikkelnummer is "kommune-gnr/bnr" or "kommune-gnr/bnr/fnr" — kommune is 2-4 digits
    // (current codes are zero-padded 4-digit, historic codes used 2 digits), gnr/bnr/fnr are
    // unbounded positive integers in the spec but reasonable in practice.
    [System.Text.RegularExpressions.GeneratedRegex(@"^\d{2,4}-\d+/\d+(/\d+)?$")]
    private static partial System.Text.RegularExpressions.Regex MatrikkelnummerPattern();

    public static bool IsValidMatrikkelenhetId(string? value)
        => string.IsNullOrWhiteSpace(value) || MatrikkelenhetIdPattern().IsMatch(value.Trim());

    public static bool IsValidMatrikkelnummer(string? value)
        => string.IsNullOrWhiteSpace(value) || MatrikkelnummerPattern().IsMatch(value.Trim());
}

/// <summary>Marker class so Microsoft.AspNetCore.Mvc.Testing can find the entry assembly.</summary>
public partial class Program;
