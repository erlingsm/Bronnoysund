// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Buffers;
using System.Text;
using Bronnoysund.Lookup.Infrastructure.Persistence.Cache;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

public sealed class GzipHybridCacheSerializerTests
{
    private sealed record Sample(string Name, int Age, string[] Tags);

    [Fact]
    public void Roundtrip_ReturnsIdenticalObject()
    {
        var factory = new GzipHybridCacheSerializerFactory();
        factory.TryCreateSerializer<Sample>(out var serializer).Should().BeTrue();

        var original = new Sample("Riksrevisjonen", 207, ["statlig", "forvaltning", "tilsyn"]);
        var buffer = new ArrayBufferWriter<byte>();
        serializer!.Serialize(original, buffer);

        var restored = serializer.Deserialize(new ReadOnlySequence<byte>(buffer.WrittenSpan.ToArray()));
        restored.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void CompressesRepetitiveData_BelowUncompressedSize()
    {
        var factory = new GzipHybridCacheSerializerFactory();
        factory.TryCreateSerializer<string>(out var serializer).Should().BeTrue();

        var repeated = new string('a', 2000);

        var buffer = new ArrayBufferWriter<byte>();
        serializer!.Serialize(repeated, buffer);

        // 2000 identical characters should compress to far below 100 bytes.
        buffer.WrittenCount.Should().BeLessThan(100);
    }
}
