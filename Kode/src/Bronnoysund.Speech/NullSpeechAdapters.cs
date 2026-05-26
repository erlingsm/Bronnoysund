// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Speech;

/// <summary>
/// Fallback <see cref="ISpeechToText"/> registered by
/// <see cref="ServiceCollectionExtensions.AddBronnoysundSpeech"/> via <c>TryAdd</c>. Reports
/// <see cref="IsAvailable"/> as <c>false</c> so the UI hides the microphone button on hosts
/// that have not registered a platform adapter (e.g. the headless WebApi, or a MAUI build
/// before the platform package was wired in). <see cref="ListenAsync"/> returns <c>null</c>
/// rather than throwing because the calling component is allowed to invoke it defensively
/// even though the button should already be hidden.
/// </summary>
internal sealed class NullSpeechToText : ISpeechToText
{
    public bool IsAvailable => false;

    public Task<string?> ListenAsync(string languageCode, CancellationToken ct) =>
        Task.FromResult<string?>(null);
}

/// <summary>
/// Fallback <see cref="ITextToSpeech"/>. Same rationale as <see cref="NullSpeechToText"/>:
/// reports unavailable, accepts calls without throwing so the caller can be naive.
/// </summary>
internal sealed class NullTextToSpeech : ITextToSpeech
{
    public bool IsAvailable => false;

    public Task SpeakAsync(string text, string languageCode, CancellationToken ct) =>
        Task.CompletedTask;
}
