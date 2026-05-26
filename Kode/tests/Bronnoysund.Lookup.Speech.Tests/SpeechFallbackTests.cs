// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Speech;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Bronnoysund.Lookup.Speech.Tests;

public class SpeechFallbackTests
{
    [Fact]
    public async Task Default_speech_to_text_reports_unavailable_and_returns_null()
    {
        var services = new ServiceCollection();
        services.AddBronnoysundSpeech();

        using var provider = services.BuildServiceProvider();
        var stt = provider.GetRequiredService<ISpeechToText>();

        stt.IsAvailable.Should().BeFalse();
        var result = await stt.ListenAsync("nb-NO", CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Default_text_to_speech_reports_unavailable_and_does_not_throw()
    {
        var services = new ServiceCollection();
        services.AddBronnoysundSpeech();

        using var provider = services.BuildServiceProvider();
        var tts = provider.GetRequiredService<ITextToSpeech>();

        tts.IsAvailable.Should().BeFalse();
        // The fallback explicitly accepts calls without throwing so the calling component
        // can be naive — IsAvailable should hide the UI, but a stray call is harmless.
        var act = async () => await tts.SpeakAsync("hello", "nb-NO", CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Host_registration_wins_over_fallback()
    {
        var services = new ServiceCollection();
        services.AddBronnoysundSpeech();

        services.AddSingleton<ISpeechToText>(new StubSpeechToText());

        using var provider = services.BuildServiceProvider();
        var stt = provider.GetRequiredService<ISpeechToText>();

        stt.IsAvailable.Should().BeTrue();
    }

    private sealed class StubSpeechToText : ISpeechToText
    {
        public bool IsAvailable => true;
        public Task<string?> ListenAsync(string languageCode, CancellationToken ct)
            => Task.FromResult<string?>("ni en ni tre null null tre åtte åtte");
    }
}
