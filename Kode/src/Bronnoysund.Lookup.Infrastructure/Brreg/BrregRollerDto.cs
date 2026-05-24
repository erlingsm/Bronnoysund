// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text.Json.Serialization;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

// Wire DTO for /enheter/{orgnr}/roller. A role group (rollegruppe) is e.g. "Styre" (board) or
// "Daglig leder" (CEO); each group holds one or more individual roles. A role can be held by
// either a person (most common) or another entity (typical for "Regnskapsfører").
internal sealed class BrregRollerDto
{
    [JsonPropertyName("rollegrupper")]
    public List<BrregRollegruppeDto>? Rollegrupper { get; set; }
}

internal sealed class BrregRollegruppeDto
{
    [JsonPropertyName("type")]
    public BrregKodeDto? Type { get; set; }

    [JsonPropertyName("sistEndret")]
    public string? SistEndret { get; set; }

    [JsonPropertyName("roller")]
    public List<BrregRolleDto>? Roller { get; set; }
}

internal sealed class BrregRolleDto
{
    [JsonPropertyName("type")]
    public BrregKodeDto? Type { get; set; }

    [JsonPropertyName("person")]
    public BrregPersonDto? Person { get; set; }

    [JsonPropertyName("enhet")]
    public BrregRolleEnhetDto? Enhet { get; set; }

    [JsonPropertyName("fratraadt")]
    public bool Fratraadt { get; set; }

    [JsonPropertyName("avregistrert")]
    public bool Avregistrert { get; set; }
}

internal sealed class BrregKodeDto
{
    [JsonPropertyName("kode")]
    public string? Kode { get; set; }

    [JsonPropertyName("beskrivelse")]
    public string? Beskrivelse { get; set; }
}

internal sealed class BrregPersonDto
{
    [JsonPropertyName("navn")]
    public BrregPersonNavnDto? Navn { get; set; }

    [JsonPropertyName("fodselsdato")]
    public string? Fodselsdato { get; set; }

    [JsonPropertyName("erDoed")]
    public bool ErDoed { get; set; }
}

internal sealed class BrregPersonNavnDto
{
    [JsonPropertyName("fornavn")]
    public string? Fornavn { get; set; }

    [JsonPropertyName("mellomnavn")]
    public string? Mellomnavn { get; set; }

    [JsonPropertyName("etternavn")]
    public string? Etternavn { get; set; }

    public string FullName()
    {
        // Skip nulls and collapse extra whitespace; Mellomnavn is optional.
        var parts = new[] { Fornavn, Mellomnavn, Etternavn }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());
        return string.Join(' ', parts);
    }
}

internal sealed class BrregRolleEnhetDto
{
    [JsonPropertyName("organisasjonsnummer")]
    public string? Organisasjonsnummer { get; set; }

    [JsonPropertyName("navn")]
    public List<string>? Navn { get; set; }

    public string FullName() =>
        Navn is null ? string.Empty : string.Join(' ', Navn.Select(n => n.Trim()));
}
