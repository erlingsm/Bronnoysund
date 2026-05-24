// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Speech;

/// <summary>Port for on-device speech-to-text. Platform-specific adapters in Speech.Maui/Speech.Web.</summary>
public interface ISpeechToText
{
    bool IsAvailable { get; }

    /// <summary>Listen for speech in language (e.g. "nb-NO") and return the recognized text (null = cancelled/failed).</summary>
    Task<string?> ListenAsync(string languageCode, CancellationToken ct);
}

/// <summary>Port for on-device text-to-speech.</summary>
public interface ITextToSpeech
{
    bool IsAvailable { get; }

    Task SpeakAsync(string text, string languageCode, CancellationToken ct);
}
