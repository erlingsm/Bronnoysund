using Bronnoysund.MauiMobile.Platforms.iOS;
using Foundation;
using UIKit;
using WatchConnectivity;

namespace Bronnoysund.MauiMobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	private BrregWatchSessionDelegate? _watchDelegate;

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		var started = base.FinishedLaunching(application, launchOptions);
		if (WCSession.IsSupported && IPlatformApplication.Current is { } platform)
		{
			_watchDelegate = new BrregWatchSessionDelegate(platform.Services);
			WCSession.DefaultSession.Delegate = _watchDelegate;
			WCSession.DefaultSession.ActivateSession();
		}
		return started;
	}
}
