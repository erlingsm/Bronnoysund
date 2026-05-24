// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text.Json.Serialization;
using Bronnoysund.Lookup.Domain;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

/// <summary>
/// JSON contract for a single entity from the Brreg Enhetsregisteret. Norwegian field names
/// match the /enhetsregisteret/api/enheter/{orgnr} response. We map to English-field DTOs in
/// the application layer.
/// </summary>
internal sealed class BrregEnhetDto
{
    [JsonPropertyName("organisasjonsnummer")]
    public string? Organisasjonsnummer { get; set; }

    [JsonPropertyName("navn")]
    public string? Navn { get; set; }

    [JsonPropertyName("organisasjonsform")]
    public BrregOrganisasjonsformDto? Organisasjonsform { get; set; }

    [JsonPropertyName("maalform")]
    public string? Maalform { get; set; }

    // Bankruptcy data lives on the entity record itself — Brreg flips `konkurs` to true and
    // populates `konkursdato` when the bankruptcy court has registered the case. There is no
    // separate "konkursregister" REST endpoint per orgnr; the entity record is the source.
    [JsonPropertyName("konkurs")]
    public bool Konkurs { get; set; }

    [JsonPropertyName("konkursdato")]
    public string? Konkursdato { get; set; }

    /// <summary>Map to our domain entity. Returns null if critical fields are missing.</summary>
    public Company? ToDomain()
    {
        if (string.IsNullOrWhiteSpace(Organisasjonsnummer) || string.IsNullOrWhiteSpace(Navn))
        {
            return null;
        }

        if (!OrganizationNumber.TryCreate(Organisasjonsnummer, out var orgNumber, out _))
        {
            return null;
        }

        var formCode = Organisasjonsform?.Kode ?? "UKJENT";
        var langForm = Maalform switch
        {
            "Bokmål" or "BOKM" or "NB" => LanguageForm.Bokmål,
            "Nynorsk" or "NYNO" or "NN" => LanguageForm.Nynorsk,
            _ => LanguageForm.Unknown,
        };

        return new Company(orgNumber, Navn, formCode, langForm);
    }
}

internal sealed class BrregOrganisasjonsformDto
{
    [JsonPropertyName("kode")]
    public string? Kode { get; set; }

    [JsonPropertyName("beskrivelse")]
    public string? Beskrivelse { get; set; }
}
