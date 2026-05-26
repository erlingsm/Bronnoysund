// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bronnoysund.Lookup.Speech;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers null-fallback implementations for <see cref="ISpeechToText"/> and
    /// <see cref="ITextToSpeech"/>. Hosts call this once and then optionally register a
    /// platform adapter (MAUI / Web) afterwards — the host registration runs last and wins
    /// the container slot, while the fallback covers hosts that never register an adapter
    /// (e.g. the headless WebApi) without forcing every consumer to null-check the port.
    /// </summary>
    public static IServiceCollection AddBronnoysundSpeech(this IServiceCollection services)
    {
        services.TryAddSingleton<ISpeechToText, NullSpeechToText>();
        services.TryAddSingleton<ITextToSpeech, NullTextToSpeech>();
        return services;
    }
}
