// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

public sealed class FavoritesRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_legger_til_ny_og_oppdaterer_eksisterende()
    {
        using var test = new TestDb();
        var repo = new FavoritesRepository(test.Db);

        await repo.UpsertAsync("974760843", "Riksrevisjonen", "Konsulent-kontrakt", default);
        var first = await repo.FindAsync("974760843", default);
        first.Should().NotBeNull();
        first!.Name.Should().Be("Riksrevisjonen");
        first.Note.Should().Be("Konsulent-kontrakt");

        await repo.UpsertAsync("974760843", "Riksrevisjonen AS", null, default);
        var second = await repo.FindAsync("974760843", default);
        second!.Name.Should().Be("Riksrevisjonen AS");
        second.Note.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_sletter_favoritt()
    {
        using var test = new TestDb();
        var repo = new FavoritesRepository(test.Db);

        await repo.UpsertAsync("974760843", "Riksrevisjonen", null, default);
        await repo.RemoveAsync("974760843", default);

        (await repo.FindAsync("974760843", default)).Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_returnerer_sortert_pa_navn()
    {
        using var test = new TestDb();
        var repo = new FavoritesRepository(test.Db);

        await repo.UpsertAsync("111111111", "Zulu AS", null, default);
        await repo.UpsertAsync("222222222", "Alpha AS", null, default);

        var all = await repo.ListAsync(default);
        all.Select(f => f.Name).Should().Equal(["Alpha AS", "Zulu AS"]);
    }
}
