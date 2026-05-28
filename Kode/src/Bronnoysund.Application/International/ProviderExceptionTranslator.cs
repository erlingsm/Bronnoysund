// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text.Json;
using System.Xml;
using Bronnoysund.Application.Results;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace Bronnoysund.Application.International;

/// <summary>
/// Shared exception-to-<see cref="CompanyLookupResult"/> translator for international
/// provider adapters. Mirrors the policy that <c>BrregCompanyProvider</c> implements
/// inline so every <see cref="Bronnoysund.Application.Ports.ICompanyProvider"/> behaves
/// the same way on transient failures.
/// </summary>
/// <remarks>
/// Covers: Polly's BrokenCircuitException (resilience pipeline tripped), JsonException
/// (malformed JSON from upstream), XmlException (malformed SOAP/XML), HttpRequestException
/// (DNS / TLS / socket-level failure), TaskCanceledException (timeout, ignoring callers'
/// own cancellations). Each one is translated to Unavailable with a provider-specific
/// message. The action is allowed to throw OperationCanceledException for caller-driven
/// cancellation — that propagates unchanged.
/// </remarks>
public static class ProviderExceptionTranslator
{
    public static async Task<CompanyLookupResult> CatchUpstreamAsync(
        Func<Task<CompanyLookupResult>> action,
        string providerName,
        string lookupId,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "{Provider} circuit open for {Id}", providerName, lookupId);
            return new CompanyLookupResult.Unavailable($"{providerName} is temporarily unavailable.");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "{Provider} returned malformed JSON for {Id}", providerName, lookupId);
            return new CompanyLookupResult.Unavailable($"{providerName} returned a malformed JSON response.");
        }
        catch (XmlException ex)
        {
            logger.LogWarning(ex, "{Provider} returned malformed XML for {Id}", providerName, lookupId);
            return new CompanyLookupResult.Unavailable($"{providerName} returned a malformed XML response.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "{Provider} transport error for {Id}", providerName, lookupId);
            return new CompanyLookupResult.Unavailable($"Could not contact {providerName}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Provider} timeout for {Id}", providerName, lookupId);
            return new CompanyLookupResult.Unavailable($"{providerName} did not respond within the timeout.");
        }
    }
}
