// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Speech;
using Microsoft.JSInterop;

namespace Bronnoysund.Lookup.Speech.Web;

/// <summary>
/// Browser adapter for <see cref="ISpeechToText"/>. Delegates to a small JS shim
/// (<c>wwwroot/speech.js</c>) that wraps <c>SpeechRecognition</c> (Chrome/Edge) or
/// <c>webkitSpeechRecognition</c> (Safari). All capture and recognition happens in the
/// browser — no audio leaves the user's machine.
/// </summary>
/// <remarks>
/// <para>
/// Registered as scoped because Blazor Server gives each circuit (browser tab) its own DI
/// scope. The <see cref="IsAvailable"/> probe is intentionally synchronous and always returns
/// <c>true</c>: the JS shim itself surfaces the "no SpeechRecognition object in this browser"
/// case by returning <c>null</c> from <see cref="ListenAsync"/>, and the UI hides the mic
/// button when a listen call comes back empty. Doing a proactive feature probe would require
/// an extra async round-trip on every page render.
/// </para>
/// <para>
/// Web Speech API requires HTTPS in production. <c>localhost</c> is exempt during development.
/// Azure Container Apps already terminates HTTPS in front of the BlazorWeb host.
/// </para>
/// </remarks>
public sealed class WebSpeechToText : ISpeechToText
{
    private readonly IJSRuntime _js;

    public WebSpeechToText(IJSRuntime js) => _js = js;

    public bool IsAvailable => true;

    public async Task<string?> ListenAsync(string languageCode, CancellationToken ct)
    {
        try
        {
            return await _js.InvokeAsync<string?>(
                "bronnoysundSpeech.listen", ct, languageCode).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { return null; }
        catch (JSException) { return null; }
        catch (TaskCanceledException) { return null; }
    }
}

/// <summary>
/// Browser adapter for <see cref="ITextToSpeech"/>. Uses the Web Speech Synthesis API via the
/// same JS shim. Voice selection picks the first matching <see cref="languageCode"/> voice in
/// the browser's voice list — typically a built-in OS voice on macOS/Windows, the Google TTS
/// voice on Chrome desktop.
/// </summary>
public sealed class WebTextToSpeech : ITextToSpeech
{
    private readonly IJSRuntime _js;

    public WebTextToSpeech(IJSRuntime js) => _js = js;

    public bool IsAvailable => true;

    public async Task SpeakAsync(string text, string languageCode, CancellationToken ct)
    {
        try
        {
            await _js.InvokeVoidAsync(
                "bronnoysundSpeech.speak", ct, text, languageCode).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        catch (TaskCanceledException) { }
    }
}
