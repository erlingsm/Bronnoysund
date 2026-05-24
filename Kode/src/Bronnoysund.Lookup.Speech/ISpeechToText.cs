// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Speech;

/// <summary>Port for on-device tale-til-tekst. Plattform-spesifikke adapters i Speech.Maui/Speech.Web.</summary>
public interface ISpeechToText
{
    bool IsAvailable { get; }

    /// <summary>Lytt etter tale på språk (f.eks. "nb-NO") og returner gjenkjent tekst (null = avbrutt/feilet).</summary>
    Task<string?> ListenAsync(string languageCode, CancellationToken ct);
}

/// <summary>Port for on-device tekst-til-tale.</summary>
public interface ITextToSpeech
{
    bool IsAvailable { get; }

    Task SpeakAsync(string text, string languageCode, CancellationToken ct);
}
