// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Infrastructure.Finland.Generated.Models;
using PrhCompany = Bronnoysund.Infrastructure.Finland.Generated.Models.Company;

namespace Bronnoysund.Infrastructure.Finland.Prh;

/// <summary>
/// Maps PRH's <see cref="PrhCompany"/> to the country-agnostic <see cref="CompanyResponse"/>.
/// Every field access uses <c>?.</c> so a future spec change that drops an optional field
/// becomes null rather than an exception — the same graceful-fallback policy as Brreg.
/// Some fields have no PRH equivalent (Norwegian-only concepts like målform, MVA-status,
/// phone numbers, employee count) and are left null.
/// </summary>
internal static class PrhMapper
{
    public static CompanyResponse? Map(PrhCompany company)
    {
        var businessId = company.BusinessId?.Value;
        var primaryName = PickPrimaryName(company.Names);

        if (string.IsNullOrWhiteSpace(businessId) || string.IsNullOrWhiteSpace(primaryName))
        {
            return null;
        }

        var streetAddress = PickAddress(company.Addresses, type: 1);
        var postalAddress = PickAddress(company.Addresses, type: 2);

        return new CompanyResponse(
            OrganizationNumber: businessId,
            OrganizationName: primaryName,
            CompanyType: PickCurrentCompanyForm(company.CompanyForms) ?? "UKJENT",
            // PRH does not expose a "målform" concept (Norwegian-specific). Identifier-level
            // language hint (FI vs SE name parallel) is in Names[].Source, not on Company.
            LanguageForm: "Unknown",
            Website: NullIfEmpty(company.Website?.Url),
            Email: null,
            Phone: null,
            MobilePhone: null,
            BusinessAddress: streetAddress,
            PostalAddress: postalAddress,
            PrimaryIndustry: MapIndustry(company.MainBusinessLine),
            EmployeeCount: null,
            SectorCode: null,
            SectorDescription: null,
            FoundingDate: ToDateOnly(company.RegistrationDate),
            RegisteredDate: ToDateOnly(company.RegistrationDate),
            RegisteredInVatRegistry: null,
            RegisteredInBusinessRegistry: null,
            IsBankrupt: IsBankrupt(company.CompanySituations),
            BankruptcyDate: BankruptcyDate(company.CompanySituations),
            DeletedDate: ToDateOnly(company.EndDate));
    }

    /// <summary>
    /// PRH returns multiple names: type "1" is the current registered name, type "2" is a
    /// parallel name (e.g. Swedish variant for bilingual companies), type "3" is an
    /// auxiliary trade name. We prefer the current registered name; if absent, fall back to
    /// any name (sorted by EndDate so we never pick an expired one when a live one exists).
    /// </summary>
    private static string? PickPrimaryName(IEnumerable<RegisterName>? names)
    {
        if (names is null) return null;
        var current = names.FirstOrDefault(n => n.Type == "1" && n.EndDate is null && !string.IsNullOrWhiteSpace(n.Name));
        if (current is not null) return current.Name;
        return names.Where(n => n.EndDate is null && !string.IsNullOrWhiteSpace(n.Name))
            .Select(n => n.Name).FirstOrDefault();
    }

    private static string? PickCurrentCompanyForm(IEnumerable<CompanyForm>? forms)
    {
        if (forms is null) return null;
        return forms
            .Where(f => f.EndDate is null && !string.IsNullOrWhiteSpace(f.Type))
            .Select(f => f.Type)
            .FirstOrDefault();
    }

    /// <summary>
    /// PRH's Address.Type is 1 = street address, 2 = postal address. Some companies only
    /// register one; both can be null.
    /// </summary>
    private static PostalAddress? PickAddress(IEnumerable<Address>? addresses, int type)
    {
        if (addresses is null) return null;
        var match = addresses.FirstOrDefault(a => a.Type == type);
        if (match is null) return null;

        // PostOffices[] holds the same city in multiple languages. Pick Finnish if present
        // (lang code "1" per PRH's KIELI codeset), else Swedish (2), else the first entry.
        var city = match.PostOffices?.FirstOrDefault(p => p.LanguageCode == "1")?.City
            ?? match.PostOffices?.FirstOrDefault(p => p.LanguageCode == "2")?.City
            ?? match.PostOffices?.FirstOrDefault()?.City;

        var street = ComposeStreet(match);

        return new PostalAddress(
            StreetAddress: NullIfEmpty(street),
            PostalCode: NullIfEmpty(match.PostCode),
            City: NullIfEmpty(city),
            // PRH does not expose a separate municipality field; the city is the closest
            // proxy and is already in the City slot.
            Municipality: null,
            Country: NullIfEmpty(match.Country));
    }

    private static string? ComposeStreet(Address address)
    {
        if (!string.IsNullOrWhiteSpace(address.FreeAddressLine))
        {
            // PRH replaces spaces with underscores inside FreeAddressLine ("Norgårdsvägen_3").
            return address.FreeAddressLine.Replace('_', ' ');
        }
        var street = address.Street ?? string.Empty;
        var building = address.BuildingNumber ?? string.Empty;
        var combined = $"{street} {building}".Trim();
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    /// <summary>
    /// Main line of business — PRH uses TOL 2008 (Finnish implementation of NACE Rev 2),
    /// so the codes are interoperable with the Brreg/EU NACE branch. Description picks
    /// English when present (lang "3"), otherwise Finnish, otherwise the first entry.
    /// </summary>
    private static IndustryCode? MapIndustry(Company_mainBusinessLine? line)
    {
        if (line is null || string.IsNullOrWhiteSpace(line.Type))
        {
            return null;
        }
        var description = line.Descriptions?.FirstOrDefault(d => d.LanguageCode == "3")?.Description
            ?? line.Descriptions?.FirstOrDefault(d => d.LanguageCode == "1")?.Description
            ?? line.Descriptions?.FirstOrDefault()?.Description
            ?? string.Empty;
        return new IndustryCode(line.Type, description);
    }

    private static bool IsBankrupt(IEnumerable<CompanySituation>? situations) =>
        situations?.Any(s => s.Type == CompanySituation_type.KONK && s.EndDate is null) == true;

    private static DateOnly? BankruptcyDate(IEnumerable<CompanySituation>? situations) =>
        ToDateOnly(situations?
            .Where(s => s.Type == CompanySituation_type.KONK && s.EndDate is null)
            .Select(s => s.RegistrationDate)
            .FirstOrDefault());

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static DateOnly? ToDateOnly(Microsoft.Kiota.Abstractions.Date? kiotaDate) =>
        kiotaDate is null ? null : new DateOnly(kiotaDate.Value.Year, kiotaDate.Value.Month, kiotaDate.Value.Day);
}
