// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application;
using Bronnoysund.Lookup.BlazorWeb.Components;
using Bronnoysund.Lookup.Infrastructure;
using Bronnoysund.Lookup.Infrastructure.Persistence;
using Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;
using Bronnoysund.Lookup.ViewModels;
using MudBlazor.Services;
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
        .WriteTo.File("logs/bronnoysund-lookup-web-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddLocalization();
    var supportedCultures = new[] { "en", "nb-NO", "nn-NO" };
    builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(opts =>
    {
        opts.SetDefaultCulture("en")
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
    });

    builder.Services.AddMudServices();

    builder.Services.AddBronnoysundApplication();
    builder.Services.AddBronnoysundInfrastructure(builder.Configuration);
    builder.Services.AddSingleton<IDatabasePathProvider>(dbPathProvider);
    builder.Services.AddBronnoysundPersistence(builder.Configuration);

    // ViewModels — transient (en per komponent-instans)
    builder.Services.AddTransient<CompanyLookupViewModel>();

    var app = builder.Build();

    await app.Services.InitializeBronnoysundPersistenceAsync();

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseRequestLocalization();
    app.UseAntiforgery();
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddAdditionalAssemblies(typeof(Bronnoysund.Lookup.Components.Pages.Lookup).Assembly);

    Log.Information("Bronnoysund.Lookup.BlazorWeb starter på {Urls}", string.Join(", ", app.Urls));
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bronnoysund.Lookup.BlazorWeb krasjet ved oppstart");
}
finally
{
    Log.CloseAndFlush();
}
