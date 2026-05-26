// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using Bronnoysund.Speech;
using CommunityToolkit.Maui.Media;

namespace Bronnoysund.MauiDesktop;

/// <summary>
/// MAUI Desktop <see cref="ISpeechToText"/> adapter backed by
/// <c>CommunityToolkit.Maui.Media</c>. On Mac Catalyst this uses Apple's
/// <c>SFSpeechRecognizer</c>; on Windows it uses <c>Windows.Media.SpeechRecognition</c>.
/// The code path is identical to <c>MauiMobile</c> — duplicated here rather than shared via
/// a separate Speech.Maui project because both host projects already pay for the MAUI
/// platform packages and adding a new multi-TFM project would multiply build time.
/// </summary>
internal sealed class MauiSpeechToText : ISpeechToText
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

internal sealed class MauiTextToSpeech : ITextToSpeech
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
