// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using System.Net;
using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Brreg.Generated.Models;
using Bronnoysund.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IRolesProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Uses the open <c>/enheter/{orgnr}/roller</c> endpoint
/// — names + birth dates only, no personnummer. Variant B (with PII) requires
/// Maskinporten; see Plan 55 roadmap.
/// </summary>
internal sealed class BrregRolesProvider(
    BrregClient client,
    ILogger<BrregRolesProvider> logger) : IRolesProvider
{
    public async Task<RolesResponse?> GetRolesAsync(OrganizationNumber org, CancellationToken ct)
    {
        try
        {
            var dto = await client.Enhetsregisteret.Api.Enheter[org.Value].Roller
                .GetAsync(cancellationToken: ct).ConfigureAwait(false);
            if (dto is null)
            {
                return null;
            }

            var roles = new List<Role>();
            foreach (var group in dto.Rollegrupper ?? [])
            {
                foreach (var role in group.Roller ?? [])
                {
                    if (role.Fratraadt == true || role.Avregistrert == true)
                    {
                        continue;
                    }

                    var roleDescription = role.Type?.Beskrivelse ?? role.Type?.Kode ?? "Unknown";

                    if (role.Person is { } person)
                    {
                        roles.Add(new Role(
                            PersonName: BuildPersonName(person.Navn) ?? "(unnamed)",
                            DateOfBirth: ParseIsoDate(person.Fodselsdato),
                            RoleType: roleDescription));
                    }
                    else if (role.Enhet is { } enhet)
                    {
                        var label = JoinLines(enhet.Navn) ?? "(unnamed entity)";
                        if (!string.IsNullOrWhiteSpace(enhet.Organisasjonsnummer))
                        {
                            label = $"{label} ({enhet.Organisasjonsnummer})";
                        }
                        // PersonName doubles as display label since the Roles UI does not split
                        // persons vs entities — they show in one table with the entity name in
                        // the person column.
                        roles.Add(new Role(
                            PersonName: label,
                            DateOfBirth: null,
                            RoleType: roleDescription));
                    }
                }
            }

            return new RolesResponse(roles);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg Roller upstream error for {OrgNumber}", org.Value);
            throw new BrregUnavailableException(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for /enheter/{org.Value}/roller", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg Roller transport error for {OrgNumber}", org.Value);
            throw new BrregUnavailableException(
                $"Could not contact Brreg for /enheter/{org.Value}/roller: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new BrregUnavailableException(
                $"Brreg did not respond within the timeout for /enheter/{org.Value}/roller", ex);
        }
    }

    private static string? BuildPersonName(RollePerson_navn? n)
    {
        if (n is null) return null;
        var parts = new[] { n.Fornavn, n.Mellomnavn, n.Etternavn }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var joined = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }

    private static string? JoinLines(List<string>? lines) =>
        lines is { Count: > 0 }
            ? string.Join(' ', lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            : null;

    private static DateOnly? ParseIsoDate(string? iso) =>
        string.IsNullOrWhiteSpace(iso)
            ? null
            : DateOnly.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d
                : null;
}
