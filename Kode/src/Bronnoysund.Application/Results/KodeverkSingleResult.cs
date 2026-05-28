// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;

namespace Bronnoysund.Application.Results;

/// <summary>
/// Discriminated union for single-entity kodeverk lookups (e.g. one Kommune by
/// kommunenummer, or one Organisasjonsform by kode). Same shape as the list-item
/// payload — Brreg exposes both endpoints with identical fields, so we reuse
/// <see cref="KodeverkEntry"/> here.
/// </summary>
public abstract record KodeverkSingleResult
{
    private KodeverkSingleResult() { }

    public sealed record Found(KodeverkEntry Entry) : KodeverkSingleResult;

    public sealed record NotFound(string Code) : KodeverkSingleResult;

    public sealed record Unavailable(string Message) : KodeverkSingleResult;
}
