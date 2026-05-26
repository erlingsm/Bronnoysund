// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text.Json.Serialization;

namespace Bronnoysund.Infrastructure.Brreg;

// Wire DTO for /underenheter?overordnetEnhet=. Brreg follows HAL+JSON: the actual list lives
// at _embedded.underenheter[]. The full response also carries pagination links and a "page"
// metadata block; we ignore both until we hit a parent with > 100 sub-units in practice.
internal sealed class BrregUnderenheterPageDto
{
    [JsonPropertyName("_embedded")]
    public BrregUnderenheterEmbeddedDto? Embedded { get; set; }

    [JsonPropertyName("page")]
    public BrregPageMetaDto? Page { get; set; }
}

internal sealed class BrregUnderenheterEmbeddedDto
{
    [JsonPropertyName("underenheter")]
    public List<BrregUnderenhetDto>? Underenheter { get; set; }
}

internal sealed class BrregUnderenhetDto
{
    [JsonPropertyName("organisasjonsnummer")]
    public string? Organisasjonsnummer { get; set; }

    [JsonPropertyName("navn")]
    public string? Navn { get; set; }
}

internal sealed class BrregPageMetaDto
{
    [JsonPropertyName("totalElements")]
    public int TotalElements { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}
