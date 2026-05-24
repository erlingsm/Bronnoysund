// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Application.UseCases.LookupCompany;
using Bronnoysund.Lookup.Infrastructure;
using Bronnoysund.Lookup.Infrastructure.Persistence;
using Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;
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
        .WriteTo.File("logs/bronnoysund-lookup-.log", rollingInterval: RollingInterval.Day));

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
                message = $"Fant ingen virksomhet med organisasjonsnummer {nf.OrganizationNumber}."
            }),
            CompanyLookupResult.InvalidInput inv => Results.BadRequest(new
            {
                error = "invalid_input",
                message = inv.Message
            }),
            CompanyLookupResult.Unavailable unav => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Brønnøysundregistrene er midlertidig utilgjengelig",
                detail: unav.Message),
            _ => Results.Problem("Uventet resultat-type.")
        };
    });

    app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Bronnoysund.Lookup.WebApi" }));

    Log.Information("Bronnoysund.Lookup.WebApi starter");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bronnoysund.Lookup.WebApi krasjet ved oppstart");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Markør-klasse så Microsoft.AspNetCore.Mvc.Testing kan finne entry-assembly.</summary>
public partial class Program;
