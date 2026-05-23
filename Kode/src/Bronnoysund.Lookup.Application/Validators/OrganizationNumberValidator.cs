// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Domain;
using FluentValidation;

namespace Bronnoysund.Lookup.Application.Validators;

/// <summary>
/// Application-lag-validering av en orgnr-streng før vi forsøker å bygge en
/// <see cref="OrganizationNumber"/>. Gir lesbare feilmeldinger ved 9-siffer/8-eller-9-start-brudd.
/// Den endelige MOD11-sjekken gjøres i Value Objectet selv.
/// </summary>
public sealed class OrganizationNumberValidator : AbstractValidator<string>
{
    public OrganizationNumberValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .WithMessage("Organisasjonsnummer kan ikke være tomt.");

        RuleFor(x => x)
            .Must(s => s is not null && s.Where(char.IsDigit).Count() == 9)
            .WithMessage("Organisasjonsnummer må inneholde nøyaktig 9 siffer.");

        RuleFor(x => x)
            .Must(s => s is not null && s.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '-' || c == '.'))
            .WithMessage("Organisasjonsnummer kan kun inneholde tall (eventuelt med mellomrom).");

        RuleFor(x => x)
            .Must(s =>
            {
                if (string.IsNullOrWhiteSpace(s))
                {
                    return false;
                }
                var firstDigit = s.FirstOrDefault(char.IsDigit);
                return firstDigit is '8' or '9';
            })
            .WithMessage("Organisasjonsnummer må starte med 8 eller 9.");
    }
}
