// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Buffers;
using System.IO.Compression;
using Microsoft.Extensions.Caching.Hybrid;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Cache;

/// <summary>
/// Wrapper rundt en inner IHybridCacheSerializer som GZip-komprimerer payload. Brukes for
/// både L1 (in-memory) og L2 (SQLite via IDistributedCache) — sparer minne og diskplass
/// på Brreg-respons (~50 % reduksjon). CPU-koste &lt; 1 ms per entry.
/// </summary>
internal sealed class GzipHybridCacheSerializer<T>(IHybridCacheSerializer<T> inner) : IHybridCacheSerializer<T>
{
    public T Deserialize(ReadOnlySequence<byte> source)
    {
        using var input = new MemoryStream(source.ToArray());
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gz.CopyTo(output);
        return inner.Deserialize(new ReadOnlySequence<byte>(output.ToArray()));
    }

    public void Serialize(T value, IBufferWriter<byte> target)
    {
        var innerBuffer = new ArrayBufferWriter<byte>();
        inner.Serialize(value, innerBuffer);

        using var compressed = new MemoryStream();
        using (var gz = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            gz.Write(innerBuffer.WrittenSpan);
        }
        target.Write(compressed.ToArray());
    }
}

/// <summary>
/// Factory som HybridCache slår opp via DI for å finne en serializer for type T.
/// Wrapper alt i Gzip uavhengig av type.
/// </summary>
internal sealed class GzipHybridCacheSerializerFactory : IHybridCacheSerializerFactory
{
    public bool TryCreateSerializer<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer)
    {
        var jsonInner = new JsonHybridCacheSerializer<T>();
        serializer = new GzipHybridCacheSerializer<T>(jsonInner);
        return true;
    }
}

internal sealed class JsonHybridCacheSerializer<T> : IHybridCacheSerializer<T>
{
    public T Deserialize(ReadOnlySequence<byte> source)
    {
        var reader = new System.Text.Json.Utf8JsonReader(source);
        return System.Text.Json.JsonSerializer.Deserialize<T>(ref reader)!;
    }

    public void Serialize(T value, IBufferWriter<byte> target)
    {
        using var writer = new System.Text.Json.Utf8JsonWriter(target);
        System.Text.Json.JsonSerializer.Serialize(writer, value);
    }
}
