// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application;
using Bronnoysund.Lookup.Infrastructure;
using Bronnoysund.Lookup.Infrastructure.Persistence;
using Bronnoysund.Lookup.ViewModels;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace Bronnoysund.Lookup.MauiDesktop;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddMudServices();
		builder.Services.AddBronnoysundApplication();
		builder.Services.AddBronnoysundInfrastructure(builder.Configuration);
		builder.Services.AddSingleton<IDatabasePathProvider, MauiDatabasePathProvider>();
		builder.Services.AddBronnoysundPersistence(builder.Configuration);
		builder.Services.AddTransient<CompanyLookupViewModel>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		// SQLite-fil + skjema opprettes blokkerende ved oppstart. Fil-IO går raskt;
		// alternativet (lazy i hver repository) gjør koden mer kompleks for marginal vinning.
		app.Services.InitializeBronnoysundPersistenceAsync().GetAwaiter().GetResult();
		return app;
	}
}
