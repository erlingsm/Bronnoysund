// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Domain;
using Bronnoysund.Lookup.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Lookup.Infrastructure.Brreg;

/// <summary>
/// IRolesProvider adapter for Brreg's open /enheter/{orgnr}/roller endpoint (the
/// "variant A" without national ID number — names and birth dates only). Both person and
/// entity roles are surfaced; the consumer can tell them apart via the OrgNumber on Role.
/// </summary>
internal sealed class BrregRolesProvider(
    BrregHttpClient http,
    ILogger<BrregRolesProvider> logger) : IRolesProvider
{
    public async Task<RolesResponse?> GetRolesAsync(OrganizationNumber org, CancellationToken ct)
    {
        try
        {
            var dto = await http.GetRollerAsync(org, ct);
            if (dto is null)
            {
                return null;
            }

            var roles = new List<Role>();
            foreach (var group in dto.Rollegrupper ?? [])
            {
                foreach (var role in group.Roller ?? [])
                {
                    if (role.Fratraadt || role.Avregistrert)
                    {
                        continue;
                    }

                    var roleDescription = role.Type?.Beskrivelse ?? role.Type?.Kode ?? "Unknown";

                    if (role.Person is { } person)
                    {
                        roles.Add(new Role(
                            PersonName: person.Navn?.FullName() ?? "(unnamed)",
                            DateOfBirth: ParseDateOrNull(person.Fodselsdato),
                            RoleType: roleDescription));
                    }
                    else if (role.Enhet is { } enhet)
                    {
                        var label = enhet.FullName();
                        if (!string.IsNullOrWhiteSpace(enhet.Organisasjonsnummer))
                        {
                            label = $"{label} ({enhet.Organisasjonsnummer})";
                        }
                        roles.Add(new Role(
                            PersonName: label,
                            DateOfBirth: null,
                            RoleType: roleDescription));
                    }
                }
            }

            return new RolesResponse(roles);
        }
        catch (BrregUnavailableException ex)
        {
            logger.LogWarning(ex, "Brreg Roller unavailable for {OrgNumber}", org.Value);
            throw;
        }
    }

    private static DateOnly? ParseDateOrNull(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
        {
            return null;
        }
        return DateOnly.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d) ? d : null;
    }
}
