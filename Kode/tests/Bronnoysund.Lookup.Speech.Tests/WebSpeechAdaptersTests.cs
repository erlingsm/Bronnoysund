// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Speech.Web;
using FluentAssertions;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using NSubstitute;

namespace Bronnoysund.Lookup.Speech.Tests;

/// <summary>
/// Contract tests for the Web-Speech JS-interop adapters. We don't run a real browser; the
/// IJSRuntime is mocked so we can lock in the expected function names ("bronnoysundSpeech.listen"
/// and ".speak") and the failure-handling behaviour (any JS exception or disconnect must come
/// back to the caller as null / silent no-op, never as a thrown exception).
/// </summary>
public class WebSpeechAdaptersTests
{
    [Fact]
    public async Task ListenAsync_invokes_bronnoysundSpeech_listen_with_language_code()
    {
        var js = Substitute.For<IJSRuntime>();
        // .Returns() with a factory delegate is required for ValueTask — a fresh instance per
        // call satisfies the "consume only once" rule that CA2012 enforces.
        js.InvokeAsync<string?>(
                Arg.Is("bronnoysundSpeech.listen"),
                Arg.Any<CancellationToken>(),
                Arg.Is<object?[]>(a => a.Length == 1 && (string)a[0]! == "nb-NO"))
            .Returns(_ => new ValueTask<string?>("ni en ni tre null null tre åtte åtte"));

        var stt = new WebSpeechToText(js);
        var result = await stt.ListenAsync("nb-NO", CancellationToken.None);

        result.Should().Be("ni en ni tre null null tre åtte åtte");
    }

    [Fact]
    public async Task ListenAsync_returns_null_when_browser_lacks_speech_recognition()
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>(
                Arg.Is("bronnoysundSpeech.listen"),
                Arg.Any<CancellationToken>(),
                Arg.Any<object?[]>())
            .Returns(_ => new ValueTask<string?>((string?)null));

        var stt = new WebSpeechToText(js);
        var result = await stt.ListenAsync("nb-NO", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListenAsync_swallows_JSDisconnectedException_during_circuit_teardown()
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>(
                Arg.Is("bronnoysundSpeech.listen"),
                Arg.Any<CancellationToken>(),
                Arg.Any<object?[]>())
            .Returns<ValueTask<string?>>(_ => throw new JSDisconnectedException("circuit gone"));

        var stt = new WebSpeechToText(js);
        var result = await stt.ListenAsync("nb-NO", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SpeakAsync_invokes_bronnoysundSpeech_speak_with_text_and_language()
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<IJSVoidResult>(
                Arg.Is("bronnoysundSpeech.speak"),
                Arg.Any<CancellationToken>(),
                Arg.Is<object?[]>(a => a.Length == 2
                    && (string)a[0]! == "Riksrevisjonen"
                    && (string)a[1]! == "nb-NO"))
            .Returns(_ => new ValueTask<IJSVoidResult>((IJSVoidResult)null!));

        var tts = new WebTextToSpeech(js);
        var act = async () =>
            await tts.SpeakAsync("Riksrevisjonen", "nb-NO", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
