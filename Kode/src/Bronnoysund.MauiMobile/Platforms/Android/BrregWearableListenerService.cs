// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
//
// Receives companion messages from the paired Wear OS watch running BrregWear.
// Android instantiates the service itself (zero-arg ctor), so we resolve the
// DI container from MauiApplication on each message.
//
// Requires the NuGet package `Xamarin.GooglePlayServices.Wearable` (registered
// conditionally for net*-android in Bronnoysund.MauiMobile.csproj). Until that
// package is added, this file compiles only when the corresponding usings are
// available — see csproj for the conditional include.

using Android.App;
using Android.Content;
using Android.Gms.Wearable;
using Bronnoysund.Application.Watch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.MauiMobile.Platforms.Android;

[Service(Exported = true)]
[IntentFilter(new[] { "com.google.android.gms.wearable.MESSAGE_RECEIVED" },
    DataScheme = "wear", DataHost = "*", DataPathPrefix = "/com.bronnoysund/lookup/")]
public sealed class BrregWearableListenerService : WearableListenerService
{
    // Parameter name must match the generated Android binding (p0) to satisfy CA1725;
    // we alias it locally so the logic stays readable.
    public override void OnMessageReceived(IMessageEvent p0)
    {
        var messageEvent = p0;
        base.OnMessageReceived(messageEvent);
        if (messageEvent.Path != WatchProtocol.AndroidMessagePath)
        {
            return;
        }

        // Capture managed values before returning from OnMessageReceived — the
        // IMessageEvent and its native backing can be released once Android sees
        // us return, so anything async must work from copies.
        var sourceNodeId = messageEvent.SourceNodeId;
        var data = messageEvent.GetData() ?? Array.Empty<byte>();

        var services = IPlatformApplication.Current?.Services;
        if (services is null)
        {
            return;
        }

        var logger = services.GetRequiredService<ILogger<BrregWearableListenerService>>();
        var request = WatchProtocol.TryDecode(data);
        if (request is null)
        {
            ReplyBack(sourceNodeId, new LookupResponse(
                Version: WatchProtocol.CurrentVersion,
                Result: "invalid",
                Code: "unrecognizedFormat",
                Message: "Ugyldig forespørsel."));
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var lookup = services.GetRequiredService<WatchLookupService>();
                var response = await lookup.HandleAsync(request, CancellationToken.None).ConfigureAwait(false);
                ReplyBack(sourceNodeId, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Watch lookup failed");
                ReplyBack(sourceNodeId, new LookupResponse(
                    Version: WatchProtocol.CurrentVersion,
                    Result: "unavailable",
                    Message: ex.Message));
            }
        });
    }

    private void ReplyBack(string nodeId, LookupResponse response)
    {
        var payload = WatchProtocol.Encode(response);
        WearableClass.GetMessageClient(this).SendMessage(nodeId, WatchProtocol.AndroidMessagePath, payload);
    }
}
