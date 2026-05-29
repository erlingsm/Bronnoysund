// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Exceptions;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;

namespace Bronnoysund.Infrastructure.Aggregation;

/// <summary>
/// Phase 4 aggregator: fan out to every registered provider in parallel via Task.WhenAll.
/// Per-provider exceptions are caught and surfaced as RegistryError entries in the response
/// instead of failing the entire aggregation — one slow or broken registry must not block
/// the rest. RegistryNotAvailableException (stub providers) is downgraded silently because
/// it is the expected signal for "not implemented yet" rather than a runtime fault.
/// </summary>
/// <remarks>
/// Iteration 8 added four optional enrichment providers (<see cref="ISubUnitDetailsProvider"/>,
/// <see cref="ILegalRolesProvider"/>, <see cref="IVoluntaryOrganizationProvider"/>,
/// <see cref="IEntityChangesProvider"/>). They are only fanned out when the caller passes
/// <see cref="AggregatedScope.IncludeEnrichment"/> (or Full) so the default WebApi path keeps
/// the lightweight CoreOnly payload. The Blazor Lookup page still drives these directly via
/// <c>LoadEnrichmentAsync</c> — the aggregator integration is for WebApi one-shot consumers.
/// </remarks>
internal sealed class ParallelCompanyDataAggregator(
    ICompanyProviderRegistry registry,
    IRolesProvider roles,
    IAnnualReportProvider annualReport,
    IBeneficialOwnerProvider beneficialOwner,
    IDebtRegisterProvider debt,
    IBankruptcyProvider bankruptcy,
    ISubUnitsProvider subUnits,
    ISubUnitDetailsProvider subUnitDetails,
    ILegalRolesProvider legalRoles,
    IVoluntaryOrganizationProvider voluntary,
    IEntityChangesProvider entityChanges,
    ILogger<ParallelCompanyDataAggregator> log) : ICompanyDataAggregator
{
    private const int EntityChangesPageSize = 20;

    // The Norwegian core registry routed via the country registry. Picking ICompanyProvider
    // directly from DI resolves to the last-registered implementation, which is one of the
    // Plan 21 international providers — Brreg ends up bypassed and lookups for OrganizationNumber
    // get rejected by the wrong country adapter.
    private ICompanyProvider NorwegianCompany =>
        registry.GetForCountry("NO")
            ?? throw new InvalidOperationException("Norwegian ICompanyProvider is not registered.");

    public async Task<AggregatedCompanyResponse> AggregateAsync(
        OrganizationNumber org,
        AggregatedScope scope,
        CancellationToken ct)
    {
        var errors = new List<RegistryError>();
        var company = NorwegianCompany;

        var coreTask        = SafeRequiredAsync("core",        () => company.LookupAsync(org, ct), errors);
        var rolesTask       = SafeOptionalAsync("roles",       () => roles.GetRolesAsync(org, ct), errors);
        var annualTask      = SafeOptionalAsync("annualReport",() => annualReport.GetLatestAsync(org, ct), errors);
        var beneficialTask  = SafeOptionalAsync("beneficial",  () => beneficialOwner.GetAsync(org, ct), errors);
        var debtTask        = SafeOptionalAsync("debt",        () => debt.GetAsync(org, ct), errors);
        var bankruptcyTask  = SafeOptionalAsync("bankruptcy",  () => bankruptcy.GetAsync(org, ct), errors);
        var subUnitsTask    = SafeOptionalAsync("subUnits",    () => subUnits.GetSubUnitsAsync(org, ct), errors);

        // Enrichment ports are opt-in. When scope=CoreOnly we never even await the providers,
        // matching the legacy behaviour and keeping the WebApi default cheap.
        var includeEnrichment = scope is AggregatedScope.IncludeEnrichment or AggregatedScope.Full;
        var subUnitDetailsTask = includeEnrichment
            ? SafeResultAsync<SubUnitLookupResult, SubUnitDetailsResponse>(
                "subUnitDetails",
                () => subUnitDetails.LookupAsync(org, ct),
                static r => r is SubUnitLookupResult.Found f ? f.SubUnit : null,
                static r => r is SubUnitLookupResult.Unavailable u ? u.Message : null,
                errors)
            : Task.FromResult<SubUnitDetailsResponse?>(null);

        var legalRolesTask = includeEnrichment
            ? SafeResultAsync<LegalRolesLookupResult, LegalRolesResponse>(
                "legalRoles",
                () => legalRoles.GetLegalRolesAsync(org, ct),
                static r => r is LegalRolesLookupResult.Found f ? f.Roles : null,
                static r => r is LegalRolesLookupResult.Unavailable u ? u.Message : null,
                errors)
            : Task.FromResult<LegalRolesResponse?>(null);

        var voluntaryTask = includeEnrichment
            ? SafeResultAsync<VoluntaryOrganizationLookupResult, VoluntaryOrganizationResponse>(
                "voluntary",
                () => voluntary.LookupAsync(org, ct),
                static r => r is VoluntaryOrganizationLookupResult.Found f ? f.Organization : null,
                static r => r is VoluntaryOrganizationLookupResult.Unavailable u ? u.Message : null,
                errors)
            : Task.FromResult<VoluntaryOrganizationResponse?>(null);

        var changesTask = includeEnrichment
            ? SafeRequiredAsync("changes", () => entityChanges.GetChangesAsync(org, EntityChangesPageSize, ct), errors)
            : Task.FromResult<EntityChangesResponse?>(null);

        await Task.WhenAll(
            coreTask, rolesTask, annualTask, beneficialTask, debtTask, bankruptcyTask, subUnitsTask,
            subUnitDetailsTask, legalRolesTask, voluntaryTask, changesTask);

        var core = coreTask.Result switch
        {
            CompanyLookupResult.Found f => f.Company,
            CompanyLookupResult.NotFound nf => throw new InvalidOperationException(
                $"Aggregation called on non-existent organization {nf.OrganizationNumber}. Call CoreOnlyAsync first."),
            CompanyLookupResult.InvalidInput inv => throw new InvalidOperationException(
                $"Aggregation called with invalid input: {inv.Message}"),
            CompanyLookupResult.Unavailable u => throw new InvalidOperationException(
                $"Core registry unavailable: {u.Message}"),
            _ => throw new InvalidOperationException("Unexpected core result type."),
        };

        return new AggregatedCompanyResponse(
            Core: core,
            Roles: rolesTask.Result,
            LatestAnnualReport: annualTask.Result,
            BeneficialOwners: beneficialTask.Result,
            Debt: debtTask.Result,
            Bankruptcy: bankruptcyTask.Result,
            SubUnits: subUnitsTask.Result,
            Errors: errors,
            SubUnitDetails: subUnitDetailsTask.Result,
            LegalRoles: legalRolesTask.Result,
            Voluntary: voluntaryTask.Result,
            Changes: changesTask.Result);
    }

    public Task<CompanyLookupResult> CoreOnlyAsync(OrganizationNumber org, CancellationToken ct)
        => NorwegianCompany.LookupAsync(org, ct);

    // Two variants exist because some provider methods return Task<T?> while
    // ICompanyProvider.LookupAsync returns Task<T> (the core is mandatory). Both demote
    // runtime failures to a RegistryError entry; stub providers throwing
    // RegistryNotAvailableException are silently treated as "no data".
    private async Task<T?> SafeOptionalAsync<T>(string registryName, Func<Task<T?>> op, List<RegistryError> errors)
        where T : class
    {
        try { return await op(); }
        catch (RegistryNotAvailableException) { return null; }
        catch (Exception ex) { RecordError(registryName, ex, errors); return null; }
    }

    private async Task<T?> SafeRequiredAsync<T>(string registryName, Func<Task<T>> op, List<RegistryError> errors)
        where T : class
    {
        try { return await op(); }
        catch (RegistryNotAvailableException) { return null; }
        catch (Exception ex) { RecordError(registryName, ex, errors); return null; }
    }

    // Demote discriminated-union results (Found/NotFound/Unavailable) to the same
    // RegistryError pattern: Unavailable -> RegistryError, NotFound/NotRegistered -> null
    // (a meaningful business "no data" answer), Found -> the payload.
    private async Task<TPayload?> SafeResultAsync<TResult, TPayload>(
        string registryName,
        Func<Task<TResult>> op,
        Func<TResult, TPayload?> selectPayload,
        Func<TResult, string?> selectUnavailableMessage,
        List<RegistryError> errors)
        where TPayload : class
    {
        try
        {
            var result = await op();
            var unavailable = selectUnavailableMessage(result);
            if (unavailable is not null)
            {
                lock (errors)
                {
                    errors.Add(new RegistryError(registryName, unavailable));
                }
                return null;
            }
            return selectPayload(result);
        }
        catch (RegistryNotAvailableException) { return null; }
        catch (Exception ex) { RecordError(registryName, ex, errors); return null; }
    }

    private void RecordError(string registryName, Exception ex, List<RegistryError> errors)
    {
        log.LogWarning(ex, "Aggregator provider {Registry} failed", registryName);
        lock (errors)
        {
            errors.Add(new RegistryError(registryName, ex.Message));
        }
    }
}
