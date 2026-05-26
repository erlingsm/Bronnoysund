// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Infrastructure;
using Bronnoysund.Lookup.Infrastructure.Persistence;
using Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;
using Bronnoysund.Lookup.Speech;
using Bronnoysund.Lookup.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace Bronnoysund.Lookup.MauiMobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
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
		builder.Services.AddBronnoysundSpeech();
		builder.Services.AddSingleton<IDeviceLayout, MauiDeviceLayout>();
		builder.Services.AddSingleton<ISpeechToText, MauiSpeechToText>();
		builder.Services.AddSingleton<ITextToSpeech, MauiTextToSpeech>();
		builder.Services.AddTransient<CompanyLookupViewModel>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		app.Services.InitializeBronnoysundPersistenceAsync().GetAwaiter().GetResult();
		return app;
	}
}
