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

    app.MapGet("/companies/{orgnr}", async (
        string orgnr,
        LookupCompanyHandler handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new LookupCompanyQuery(orgnr), ct);
        return result switch
        {
            CompanyLookupResult.Found f => Results.Ok(f.Company),
            CompanyLookupResult.NotFound nf => Results.NotFound(new
            {
                error = "not_found",
                message = $"No company with organization number {nf.OrganizationNumber} was found."
            }),
            CompanyLookupResult.InvalidInput inv => Results.BadRequest(new
            {
                error = "invalid_input",
                message = inv.Message
            }),
            CompanyLookupResult.Unavailable unav => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Brønnøysundregistrene (the Brønnøysund Register Centre) is temporarily unavailable",
                detail: unav.Message),
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
            return Results.BadRequest(new { error = "missing_name", message = "Query parameter 'name' is required." });
        }

        var result = await handler.HandleAsync(new SearchCompaniesByNameQuery(name, size), ct);
        return result switch
        {
            SearchCompaniesByNameResult.Found f => Results.Ok(f.Result),
            SearchCompaniesByNameResult.InvalidInput inv => Results.BadRequest(new { error = "invalid_input", message = inv.Message }),
            SearchCompaniesByNameResult.Unavailable u => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Brønnøysundregistrene (the Brønnøysund Register Centre) is temporarily unavailable",
                detail: u.Message),
            _ => Results.Problem("Unexpected result type."),
        };
    });

    app.MapGet("/companies/{orgnr}/aggregated", async (
        string orgnr,
        ICompanyDataAggregator aggregator,
        CancellationToken ct) =>
    {
        if (!OrganizationNumber.TryCreate(orgnr, out var org, out var error))
        {
            return Results.BadRequest(new { error = "invalid_input", message = error });
        }

        var core = await aggregator.CoreOnlyAsync(org, ct);
        switch (core)
        {
            case CompanyLookupResult.NotFound nf:
                return Results.NotFound(new
                {
                    error = "not_found",
                    message = $"No company with organization number {nf.OrganizationNumber} was found."
                });
            case CompanyLookupResult.Unavailable u:
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Brønnøysundregistrene (the Brønnøysund Register Centre) is temporarily unavailable",
                    detail: u.Message);
            case CompanyLookupResult.InvalidInput inv:
                return Results.BadRequest(new { error = "invalid_input", message = inv.Message });
        }

        var aggregated = await aggregator.AggregateAsync(org, ct);
        return Results.Ok(aggregated);
    });

    app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Bronnoysund.WebApi" }));

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

/// <summary>Marker class so Microsoft.AspNetCore.Mvc.Testing can find the entry assembly.</summary>
public partial class Program;
