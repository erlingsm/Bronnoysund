// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using CommunityToolkit.Maui.Media;
// Microsoft.Maui.Media (introduced as a top-level abstraction in Maui 10.0.60) ships its own
// ISpeechToText / ITextToSpeech that collide with our Bronnoysund.Speech port-names. Alias the
// app-side port through "Ports" so the class declarations can opt explicitly into our types.
using Ports = Bronnoysund.Speech;

namespace Bronnoysund.MauiMobile;

/// <summary>
/// MAUI <see cref="ISpeechToText"/> adapter backed by <c>CommunityToolkit.Maui.Media</c>,
/// which itself wraps the platform-native recognizers: <c>SFSpeechRecognizer</c> on
/// iOS/MacCatalyst, <c>android.speech.SpeechRecognizer</c> on Android,
/// <c>Windows.Media.SpeechRecognition</c> on Windows. All on-device, all free.
/// </summary>
/// <remarks>
/// Permission is requested lazily on the first <see cref="ListenAsync"/> call so a user who
/// never taps the mic button is never prompted. The permission dialog text lives in each
/// platform's manifest — see <c>Platforms/iOS/Info.plist</c> and
/// <c>Platforms/Android/AndroidManifest.xml</c>.
/// </remarks>
internal sealed class MauiSpeechToText : Ports.ISpeechToText
{
    public bool IsAvailable => SpeechToText.Default is not null;

    public async Task<string?> ListenAsync(string languageCode, CancellationToken ct)
    {
        var stt = SpeechToText.Default;
        if (stt is null)
        {
            return null;
        }

        try
        {
            var granted = await stt.RequestPermissions(ct).ConfigureAwait(false);
            if (!granted)
            {
                return null;
            }

            var culture = CultureInfo.GetCultureInfo(languageCode);
            var result = await stt.ListenAsync(
                culture,
                new SpeechToTextOptions { ShouldReportPartialResults = false },
                ct).ConfigureAwait(false);

            return result.IsSuccessful ? result.Text : null;
        }
        catch (TaskCanceledException) { return null; }
        catch (Exception) { return null; }
    }
}

/// <summary>
/// MAUI <see cref="ITextToSpeech"/> adapter backed by <c>Microsoft.Maui.Media.TextToSpeech</c>.
/// All four target platforms ship Norwegian voices out of the box.
/// </summary>
internal sealed class MauiTextToSpeech : Ports.ITextToSpeech
{
    public bool IsAvailable => true;

    public async Task SpeakAsync(string text, string languageCode, CancellationToken ct)
    {
        try
        {
            var locales = await Microsoft.Maui.Media.TextToSpeech.Default
                .GetLocalesAsync().ConfigureAwait(false);

            var locale = locales.FirstOrDefault(l => l.Language == languageCode)
                ?? locales.FirstOrDefault(l => l.Language.StartsWith(
                    languageCode.AsSpan(0, 2), StringComparison.OrdinalIgnoreCase));

            await Microsoft.Maui.Media.TextToSpeech.Default.SpeakAsync(
                text,
                new SpeechOptions { Locale = locale },
                ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException) { }
        catch (Exception) { }
    }
}
