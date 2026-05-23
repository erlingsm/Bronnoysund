// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text.Json.Serialization;
using Bronnoysund.Lookup.Domain;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

/// <summary>
/// JSON-kontrakt for én enhet fra Brreg Enhetsregisteret. Norske feltnavn matcher
/// /enhetsregisteret/api/enheter/{orgnr}-responsen. Vi mapper til engelsk-felt DTOer i
/// applikasjonslaget.
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

    /// <summary>Map til vår domeneentitet. Returnerer null hvis kritiske felter mangler.</summary>
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
