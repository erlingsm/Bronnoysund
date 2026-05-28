// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Globalization;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IEntityChangesProvider"/> backed by the Kiota-generated
/// <see cref="BrregClient"/>. Fans out the three Brreg "oppdateringer" feeds in
/// parallel and isolates per-feed errors so the UI gets partial results when only
/// one feed is failing.
/// </summary>
internal sealed class BrregEntityChangesProvider(
    BrregClient client,
    ILogger<BrregEntityChangesProvider> logger) : IEntityChangesProvider
{
    public async Task<EntityChangesResponse> GetChangesAsync(OrganizationNumber org, int pageSize, CancellationToken ct)
    {
        var sizeString = pageSize.ToString(CultureInfo.InvariantCulture);

        var entityTask = LoadEntityFeedAsync(org, sizeString, ct);
        var subUnitTask = LoadSubUnitFeedAsync(org, sizeString, ct);
        var roleTask = LoadRoleFeedAsync(org, sizeString, ct);

        await Task.WhenAll(entityTask, subUnitTask, roleTask).ConfigureAwait(false);

        return new EntityChangesResponse(
            OrganizationNumber: org.Value,
            EntityFeed: entityTask.Result,
            SubUnitFeed: subUnitTask.Result,
            RoleFeed: roleTask.Result);
    }

    private async Task<EntityChangesFeed> LoadEntityFeedAsync(OrganizationNumber org, string size, CancellationToken ct)
    {
        try
        {
            var page = await client.Enhetsregisteret.Api.Oppdateringer.Enheter
                .GetAsync(cfg =>
                {
                    cfg.QueryParameters.Organisasjonsnummer = [org.Value];
                    cfg.QueryParameters.Size = size;
                }, ct).ConfigureAwait(false);

            var items = (page?.Embedded?.OppdaterteEnheter ?? [])
                .Select(e => new EntityChange(
                    Timestamp: ParseDate(e.Dato),
                    ChangeType: e.Endringstype ?? string.Empty,
                    UpdateId: e.Oppdateringsid is { } id ? (long)id : null))
                .ToList();

            return new EntityChangesFeed(items);
        }
        catch (Exception ex) when (ShouldIsolate(ex, ct))
        {
            logger.LogWarning(ex, "Brreg entity-updates feed failed for {OrgNumber}", org.Value);
            return new EntityChangesFeed([], DescribeError(ex, "/oppdateringer/enheter"));
        }
    }

    private async Task<EntityChangesFeed> LoadSubUnitFeedAsync(OrganizationNumber org, string size, CancellationToken ct)
    {
        try
        {
            var page = await client.Enhetsregisteret.Api.Oppdateringer.Underenheter
                .GetAsync(cfg =>
                {
                    cfg.QueryParameters.Organisasjonsnummer = [org.Value];
                    cfg.QueryParameters.Size = size;
                }, ct).ConfigureAwait(false);

            var items = (page?.Embedded?.OppdaterteUnderenheter ?? [])
                .Select(e => new EntityChange(
                    Timestamp: ParseDate(e.Dato),
                    ChangeType: e.Endringstype ?? string.Empty,
                    UpdateId: e.Oppdateringsid is { } id ? (long)id : null))
                .ToList();

            return new EntityChangesFeed(items);
        }
        catch (Exception ex) when (ShouldIsolate(ex, ct))
        {
            logger.LogWarning(ex, "Brreg sub-unit-updates feed failed for {OrgNumber}", org.Value);
            return new EntityChangesFeed([], DescribeError(ex, "/oppdateringer/underenheter"));
        }
    }

    private async Task<EntityChangesFeed> LoadRoleFeedAsync(OrganizationNumber org, string size, CancellationToken ct)
    {
        try
        {
            var list = await client.Enhetsregisteret.Api.Oppdateringer.Roller
                .GetAsync(cfg =>
                {
                    cfg.QueryParameters.Organisasjonsnummer = [org.Value];
                    cfg.QueryParameters.Size = size;
                }, ct).ConfigureAwait(false);

            var items = (list ?? [])
                .Select(r => new EntityChange(
                    Timestamp: ParseDate(r.Time),
                    ChangeType: r.Source ?? "RoleUpdated",
                    UpdateId: null))
                .ToList();

            return new EntityChangesFeed(items);
        }
        catch (Exception ex) when (ShouldIsolate(ex, ct))
        {
            logger.LogWarning(ex, "Brreg role-updates feed failed for {OrgNumber}", org.Value);
            return new EntityChangesFeed([], DescribeError(ex, "/oppdateringer/roller"));
        }
    }

    /// <summary>
    /// Decides whether an exception should be demoted to a per-feed error or rethrown.
    /// Caller-driven cancellation (<paramref name="ct"/> is signalled) propagates as
    /// real cancellation so callers higher up can treat it as a clean abort. Server-side
    /// timeouts and transport errors are isolated as feed-errors so one bad feed cannot
    /// poison the other two.
    /// </summary>
    private static bool ShouldIsolate(Exception ex, CancellationToken ct) =>
        ex switch
        {
            TaskCanceledException when ct.IsCancellationRequested => false,
            ApiException => true,
            HttpRequestException => true,
            TaskCanceledException => true,
            Polly.CircuitBreaker.BrokenCircuitException => true,
            _ => false,
        };

    private static string DescribeError(Exception ex, string path) => ex switch
    {
        ApiException api => $"Brreg returned HTTP {api.ResponseStatusCode} for {path}",
        Polly.CircuitBreaker.BrokenCircuitException => "Brreg is temporarily unavailable.",
        TaskCanceledException => $"Brreg did not respond within the timeout for {path}",
        _ => $"Could not contact Brreg for {path}: {ex.Message}",
    };

    private static DateTimeOffset? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
            ? dt
            : null;
    }
}
