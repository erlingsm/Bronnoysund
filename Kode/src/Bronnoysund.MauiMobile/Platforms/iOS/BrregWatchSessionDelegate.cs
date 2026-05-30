// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Watch;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WatchConnectivity;

namespace Bronnoysund.MauiMobile.Platforms.iOS;

/// <summary>
/// WCSession delegate for the iPhone host. Receives binary payloads from the paired Watch
/// running BrregWatch, hands them to <see cref="WatchLookupService"/>, and replies on the
/// same channel. Activation happens from <see cref="AppDelegate.FinishedLaunching"/>.
/// </summary>
public sealed class BrregWatchSessionDelegate : WCSessionDelegate
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BrregWatchSessionDelegate> _logger;

    public BrregWatchSessionDelegate(IServiceProvider services)
    {
        _services = services;
        _logger = services.GetRequiredService<ILogger<BrregWatchSessionDelegate>>();
    }

    public override void ActivationDidComplete(WCSession session, WCSessionActivationState activationState, NSError? error)
    {
        if (error != null)
        {
            _logger.LogWarning("WCSession activation error: {Error}", error.LocalizedDescription);
        }
        else
        {
            _logger.LogInformation("WCSession activated. Paired={Paired} Reachable={Reachable}",
                session.Paired, session.Reachable);
        }
    }

    public override void SessionReachabilityDidChange(WCSession session) =>
        _logger.LogInformation("Watch reachability changed: {Reachable}", session.Reachable);

    public override void DidReceiveMessageData(WCSession session, NSData messageData, WCSessionReplyDataHandler replyHandler)
    {
        var bytes = messageData.ToArray();
        var request = WatchProtocol.TryDecode(bytes);
        if (request is null)
        {
            var bad = new LookupResponse(
                Version: WatchProtocol.CurrentVersion,
                Result: "invalid",
                Code: "unrecognizedFormat",
                Message: "Ugyldig forespørsel.");
            replyHandler(NSData.FromArray(WatchProtocol.Encode(bad)));
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var lookup = _services.GetRequiredService<WatchLookupService>();
                var response = await lookup.HandleAsync(request, CancellationToken.None).ConfigureAwait(false);
                replyHandler(NSData.FromArray(WatchProtocol.Encode(response)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Watch lookup failed");
                var err = new LookupResponse(
                    Version: WatchProtocol.CurrentVersion,
                    Result: "unavailable",
                    Message: ex.Message);
                replyHandler(NSData.FromArray(WatchProtocol.Encode(err)));
            }
        });
    }

    // iOS-only protocol methods that must be implemented to satisfy WCSessionDelegate
    // on the phone side. No-op overrides are required by the binding.
    public override void DidBecomeInactive(WCSession session) { }
    public override void DidDeactivate(WCSession session) { }
}
