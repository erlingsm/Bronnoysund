// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Brreg.Generated.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="ILegalRolesProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Maps <c>RolleJuridiskeroller</c> to <see cref="LegalRolesResponse"/>.
/// </summary>
internal sealed class BrregLegalRolesProvider(
    BrregClient client,
    ILogger<BrregLegalRolesProvider> logger) : ILegalRolesProvider
{
    public async Task<LegalRolesLookupResult> GetLegalRolesAsync(OrganizationNumber org, CancellationToken ct)
    {
        try
        {
            var response = await client.Enhetsregisteret.Api.Roller.Enheter[org.Value].Juridiskeroller
                .GetAsync(cancellationToken: ct).ConfigureAwait(false);

            // Brreg returns 404 (not 200 + null) when the org has no roles registered
            // elsewhere — that path is handled by the ApiException-catch below. A null
            // response here means Kiota saw 200 OK but failed to parse the body into the
            // expected composed type, which is an upstream-shape problem, not "not found".
            if (response is null)
            {
                return new LegalRolesLookupResult.Unavailable("Brreg returned an unexpected response structure.");
            }

            return new LegalRolesLookupResult.Found(MapToResponse(response, org.Value));
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound ||
                                       ex.ResponseStatusCode == (int)HttpStatusCode.Gone)
        {
            logger.LogInformation("Brreg returned {Status} for legal roles {OrgNumber}", ex.ResponseStatusCode, org.Value);
            return new LegalRolesLookupResult.NotFound(org.Value);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg upstream error for legal roles {OrgNumber}", org.Value);
            return new LegalRolesLookupResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for /roller/enheter/{org.Value}/juridiskeroller");
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Brreg circuit open for legal roles {OrgNumber}", org.Value);
            return new LegalRolesLookupResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg transport error for legal roles {OrgNumber}", org.Value);
            return new LegalRolesLookupResult.Unavailable(
                $"Could not contact Brreg for /roller/enheter/{org.Value}/juridiskeroller: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg timeout for legal roles {OrgNumber}", org.Value);
            return new LegalRolesLookupResult.Unavailable(
                $"Brreg did not respond within the timeout for /roller/enheter/{org.Value}/juridiskeroller");
        }
    }

    private static LegalRolesResponse MapToResponse(RolleJuridiskeroller r, string fallbackOrg)
    {
        var holdings = (r.Enheter ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e.Organisasjonsnummer))
            .Select(e => new LegalRoleHolding(
                OrganizationNumber: e.Organisasjonsnummer!,
                Name: e.Navn ?? string.Empty,
                Roles: (e.Roller ?? [])
                    .Select(role => new LegalRoleAssignment(
                        TypeCode: role.Type?.Kode ?? string.Empty,
                        TypeDescription: role.Type?.Beskrivelse ?? string.Empty,
                        IsResigned: role.Fratraadt == true,
                        IsDeregistered: role.Avregistrert == true,
                        Order: role.Rekkefolge is { } o ? (int)o : null))
                    .ToList()))
            .ToList();

        return new LegalRolesResponse(
            OrganizationNumber: r.Organisasjonsnummer ?? fallbackOrg,
            IsDeleted: r.ErSlettet == true,
            Holdings: holdings);
    }
}
