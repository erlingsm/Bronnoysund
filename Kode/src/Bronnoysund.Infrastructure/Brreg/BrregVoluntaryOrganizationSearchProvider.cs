// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Web;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Infrastructure.Brreg.Generated;
using Bronnoysund.Infrastructure.Brreg.Generated.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Date = Microsoft.Kiota.Abstractions.Date;

namespace Bronnoysund.Infrastructure.Brreg;

/// <summary>
/// Adapter for <see cref="IVoluntaryOrganizationSearchProvider"/>.
/// </summary>
/// <remarks>
/// Unlike the single-entity sibling <see cref="BrregVoluntaryOrganizationProvider"/>, the
/// search endpoint returns a concrete <c>List&lt;FrivilligOrganisasjonInnfoert&gt;</c> (no
/// composed-type wrapper) so Kiota deserializes it cleanly. We still hand-extract the
/// <c>_links.next.href</c> cursor from <see cref="Links.AdditionalData"/> because the spec
/// doesn't strongly-type that node — defensive fallback returns <c>null</c> when the shape
/// drifts so callers see "no more pages" instead of an exception.
/// </remarks>
internal sealed class BrregVoluntaryOrganizationSearchProvider(
    BrregClient client,
    ILogger<BrregVoluntaryOrganizationSearchProvider> logger) : IVoluntaryOrganizationSearchProvider
{
    private const int MinSize = 1;
    private const int MaxSize = 100;

    public async Task<VoluntaryOrganizationSearchResult> SearchAsync(
        VoluntaryOrganizationSearchQuery query, CancellationToken ct)
    {
        if (query.Size < MinSize || query.Size > MaxSize)
        {
            return new VoluntaryOrganizationSearchResult.InvalidInput(
                $"Size must be between {MinSize} and {MaxSize} (got {query.Size}).");
        }

        try
        {
            var page = await client.Frivillighetsregisteret.Api.FrivilligeOrganisasjoner
                .GetAsync(cfg =>
                {
                    cfg.QueryParameters.Size = query.Size;
                    cfg.QueryParameters.Spraak = "NOB";
                    if (!string.IsNullOrWhiteSpace(query.SearchAfter))
                    {
                        cfg.QueryParameters.SearchAfter = query.SearchAfter;
                    }
                }, ct).ConfigureAwait(false);

            var items = (page?.Embedded?.FrivilligeOrganisasjoner ?? [])
                .Select(Map)
                .OfType<VoluntaryOrganizationResponse>()
                .ToList();

            var nextCursor = ExtractNextCursor(page?.Links?.AdditionalData);

            return new VoluntaryOrganizationSearchResult.Found(
                new VoluntaryOrganizationSearchResponse(items, nextCursor));
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret search returned HTTP {Status}", ex.ResponseStatusCode);
            return new VoluntaryOrganizationSearchResult.Unavailable(
                $"Frivillighetsregisteret returned HTTP {ex.ResponseStatusCode} for /frivillige-organisasjoner");
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret search circuit open");
            return new VoluntaryOrganizationSearchResult.Unavailable("Frivillighetsregisteret is temporarily unavailable.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret search transport error");
            return new VoluntaryOrganizationSearchResult.Unavailable(
                $"Could not contact Frivillighetsregisteret for /frivillige-organisasjoner: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Frivillighetsregisteret search timed out");
            return new VoluntaryOrganizationSearchResult.Unavailable(
                "Frivillighetsregisteret did not respond within the timeout for /frivillige-organisasjoner");
        }
    }

    private static VoluntaryOrganizationResponse? Map(FrivilligOrganisasjonInnfoert f)
    {
        if (string.IsNullOrWhiteSpace(f.Organisasjonsnummer))
        {
            return null;
        }

        var primary = (f.IcnpoKategorier ?? [])
            .OrderBy(c => c.Rekkefoelge ?? int.MaxValue)
            .FirstOrDefault();

        return new VoluntaryOrganizationResponse(
            OrganizationNumber: f.Organisasjonsnummer,
            Status: f.FrivilligOrganisasjonsstatus ?? "UKJENT",
            FirstRegisteredDate: ToDateOnly(f.FoersteGangInnfoert),
            RegisteredDate: ToDateOnly(f.InnfoertDato),
            PrimaryIcnpoCategoryNumber: primary?.IcnpoNummer,
            PrimaryIcnpoCategoryName: primary?.Navn,
            ParticipatesInGrasrotandel: f.Grasrotandel?.DeltarI == true,
            AccountNumber: NullIfEmpty(f.Kontonummer));
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static DateOnly? ToDateOnly(Date? kiotaDate) =>
        kiotaDate is null ? null : new DateOnly(kiotaDate.Value.Year, kiotaDate.Value.Month, kiotaDate.Value.Day);

    /// <summary>
    /// Pulls the <c>?searchAfter=...</c> value out of <c>_links.next.href</c>. The shape of
    /// AdditionalData varies by Kiota version (UntypedObject vs JsonElement vs raw dictionary);
    /// we accept any of them and return <c>null</c> when the structure isn't what we expected.
    /// </summary>
    /// <remarks>
    /// TODO: when the Brreg spec is regenerated with strongly-typed <c>Links.Next.Href</c>,
    /// replace the <c>AdditionalData</c> traversal with <c>page?.Links?.Next?.Href</c> directly
    /// — this manual unpacking only exists because the current spec keeps <c>_links</c> in
    /// untyped <c>AdditionalData</c>.
    /// </remarks>
    private static string? ExtractNextCursor(IDictionary<string, object>? additional)
    {
        if (additional is null || !additional.TryGetValue("next", out var nextObj) || nextObj is null)
        {
            return null;
        }

        var href = ReadHref(nextObj);
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        // Brreg returns either absolute (https://data.brreg.no/...) or relative
        // (/frivillighetsregisteret/api/...) URLs. Uri.TryCreate handles the absolute case;
        // for relative URLs we strip the query part manually since Uri exposes Query only on
        // absolute URIs.
        var query = string.Empty;
        if (Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var uri))
        {
            query = uri.IsAbsoluteUri
                ? uri.Query.TrimStart('?')
                : ExtractRelativeQuery(href);
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var parsed = HttpUtility.ParseQueryString(query);
        return NullIfEmpty(parsed["searchAfter"]);
    }

    private static string ExtractRelativeQuery(string href)
    {
        var idx = href.IndexOf('?', StringComparison.Ordinal);
        return idx < 0 ? string.Empty : href[(idx + 1)..];
    }

    private static string? ReadHref(object value)
    {
        // Best-effort traversal across the shapes Kiota uses for AdditionalData on _links.next.
        // Kiota 1.x usually wraps untyped JSON in UntypedObject + UntypedString; older or
        // different serializers may surface raw IDictionary or JsonElement.
        switch (value)
        {
            case UntypedObject untyped:
                {
                    var props = untyped.GetValue();
                    if (props.TryGetValue("href", out var hrefNode))
                    {
                        return ReadString(hrefNode);
                    }
                    return null;
                }
            case IDictionary<string, object> dict when dict.TryGetValue("href", out var href):
                return ReadString(href);
            case System.Text.Json.JsonElement el when el.ValueKind == System.Text.Json.JsonValueKind.Object
                && el.TryGetProperty("href", out var hrefEl):
                return hrefEl.GetString();
            default:
                return null;
        }
    }

    private static string? ReadString(object? node) => node switch
    {
        UntypedString us => us.GetValue(),
        string s => s,
        _ => node?.ToString(),
    };
}
