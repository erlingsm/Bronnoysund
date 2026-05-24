// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application;
using Bronnoysund.Lookup.Infrastructure;
using Bronnoysund.Lookup.Infrastructure.Persistence;
using Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;
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

		var dbPathProvider = new MauiDatabasePathProvider();
		builder.Configuration.AddSqliteSettings(() => dbPathProvider.GetDatabaseFilePath());

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddMudServices();
		builder.Services.AddLocalization();
		builder.Services.AddBronnoysundApplication();
		builder.Services.AddBronnoysundInfrastructure(builder.Configuration);
		builder.Services.AddSingleton<IDatabasePathProvider>(dbPathProvider);
		builder.Services.AddBronnoysundPersistence(builder.Configuration);
		builder.Services.AddTransient<CompanyLookupViewModel>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		// The SQLite file + schema is created blockingly at startup. File IO is fast;
		// the alternative (lazy in each repository) makes the code more complex for marginal gain.
		app.Services.InitializeBronnoysundPersistenceAsync().GetAwaiter().GetResult();
		return app;
	}
}
