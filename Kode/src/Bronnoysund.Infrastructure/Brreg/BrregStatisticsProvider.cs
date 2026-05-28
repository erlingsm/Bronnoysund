// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Polly.CircuitBreaker;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IBrregStatisticsProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Reads <c>/roller/totalbestand</c> as plain text (Brreg
/// returns a bare integer body — the OpenAPI spec types it as <c>Stream</c>).
/// </summary>
internal sealed class BrregStatisticsProvider(
    BrregClient client,
    ILogger<BrregStatisticsProvider> logger) : IBrregStatisticsProvider
{
    public async Task<RolesTotalCountResult> GetRolesTotalCountAsync(CancellationToken ct)
    {
        const string Path = "/roller/totalbestand";
        try
        {
            await using var stream = await client.Enhetsregisteret.Api.Roller.Totalbestand
                .GetAsync(cancellationToken: ct).ConfigureAwait(false);

            if (stream is null)
            {
                return new RolesTotalCountResult.Unavailable($"Brreg returned an empty body for {Path}");
            }

            using var reader = new StreamReader(stream);
            var body = (await reader.ReadToEndAsync(ct).ConfigureAwait(false)).Trim();

            if (!long.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out var total))
            {
                logger.LogWarning("Brreg {Path} returned non-integer body: {Body}", Path, body);
                return new RolesTotalCountResult.Unavailable(
                    $"Brreg returned an unexpected response structure for {Path}.");
            }

            return new RolesTotalCountResult.Found(total);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Brreg {Path} upstream error", Path);
            return new RolesTotalCountResult.Unavailable(
                $"Brreg returned HTTP {ex.ResponseStatusCode} for {Path}");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Brreg {Path} circuit open", Path);
            return new RolesTotalCountResult.Unavailable("Brreg is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brreg {Path} transport error", Path);
            return new RolesTotalCountResult.Unavailable(
                $"Could not contact Brreg for {Path}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Brreg {Path} timed out", Path);
            return new RolesTotalCountResult.Unavailable(
                $"Brreg did not respond within the timeout for {Path}");
        }
    }
}
