// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IKodeverkProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Currently exposes Organisasjonsformer; Kommuner +
/// Rolletyper + Rollegruppetyper will land in a follow-up when the matching UI
/// pages are wired (see Plan 54 sections C and D).
/// </summary>
internal sealed class BrregKodeverkProvider(
    BrregClient client,
    ILogger<BrregKodeverkProvider> logger) : IKodeverkProvider
{
    public async Task<IReadOnlyList<KodeverkEntry>> GetOrganisasjonsformerAsync(CancellationToken ct)
    {
        try
        {
            var response = await client.Enhetsregisteret.Api.Organisasjonsformer
                .GetAsync(cancellationToken: ct).ConfigureAwait(false);

            return (response?.Embedded?.Organisasjonsformer ?? [])
                .Where(f => !string.IsNullOrWhiteSpace(f.Kode))
                .Select(f => new KodeverkEntry(
                    Code: f.Kode!,
                    Description: f.Beskrivelse ?? string.Empty,
                    IsRetired: !string.IsNullOrWhiteSpace(f.Utgaatt)))
                .ToList();
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsformer upstream error");
            throw new BrregUnavailableException(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for /organisasjonsformer", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg Organisasjonsformer transport error");
            throw new BrregUnavailableException(
                $"Could not contact Brreg for /organisasjonsformer: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new BrregUnavailableException(
                "Brreg did not respond within the timeout for /organisasjonsformer", ex);
        }
    }
}
