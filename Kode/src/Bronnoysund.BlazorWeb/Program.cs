// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application;
using Bronnoysund.Application.Ports;
using Bronnoysund.BlazorWeb.Adapters;
using Bronnoysund.BlazorWeb.Components;
using Bronnoysund.Infrastructure;
using Bronnoysund.Speech;
using Bronnoysund.Speech.Web;
using Bronnoysund.Infrastructure.Persistence;
using Bronnoysund.Infrastructure.Persistence.Configuration;
using Bronnoysund.ViewModels;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        .WriteTo.File("logs/bronnoysund-web-.log", rollingInterval: RollingInterval.Day));

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
    builder.Services.AddBronnoysundSpeech();

    // Per-circuit so two browser tabs at different sizes don't share layout state.
    // Replace() rather than Add(): AddBronnoysundApplication already TryAdd'd the Desktop
    // fallback, and a circuit-scoped service can't sit behind a singleton fallback in the
    // same container, so we swap the descriptor outright.
    builder.Services.RemoveAll<IDeviceLayout>();
    builder.Services.AddScoped<IDeviceLayout, WebDeviceLayout>();

    // Speech adapters are also per-circuit because IJSRuntime is scoped.
    builder.Services.RemoveAll<ISpeechToText>();
    builder.Services.RemoveAll<ITextToSpeech>();
    builder.Services.AddScoped<ISpeechToText, WebSpeechToText>();
    builder.Services.AddScoped<ITextToSpeech, WebTextToSpeech>();

    // ViewModels — transient (one per component instance)
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
        .AddAdditionalAssemblies(typeof(Bronnoysund.Components.Pages.Lookup).Assembly);

    Log.Information("Bronnoysund.BlazorWeb starting on {Urls}", string.Join(", ", app.Urls));
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bronnoysund.BlazorWeb crashed during startup");
}
finally
{
    Log.CloseAndFlush();
}
