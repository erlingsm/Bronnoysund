// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

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
internal sealed class ParallelCompanyDataAggregator(
    ICompanyProvider company,
    IRolesProvider roles,
    IAnnualReportProvider annualReport,
    IBeneficialOwnerProvider beneficialOwner,
    IDebtRegisterProvider debt,
    IBankruptcyProvider bankruptcy,
    ISubUnitsProvider subUnits,
    ILogger<ParallelCompanyDataAggregator> log) : ICompanyDataAggregator
{
    public async Task<AggregatedCompanyResponse> AggregateAsync(OrganizationNumber org, CancellationToken ct)
    {
        var errors = new List<RegistryError>();

        var coreTask        = SafeRequiredAsync("core",        () => company.LookupAsync(org, ct), errors);
        var rolesTask       = SafeOptionalAsync("roles",       () => roles.GetRolesAsync(org, ct), errors);
        var annualTask      = SafeOptionalAsync("annualReport",() => annualReport.GetLatestAsync(org, ct), errors);
        var beneficialTask  = SafeOptionalAsync("beneficial",  () => beneficialOwner.GetAsync(org, ct), errors);
        var debtTask        = SafeOptionalAsync("debt",        () => debt.GetAsync(org, ct), errors);
        var bankruptcyTask  = SafeOptionalAsync("bankruptcy",  () => bankruptcy.GetAsync(org, ct), errors);
        var subUnitsTask    = SafeOptionalAsync("subUnits",    () => subUnits.GetSubUnitsAsync(org, ct), errors);

        await Task.WhenAll(coreTask, rolesTask, annualTask, beneficialTask, debtTask, bankruptcyTask, subUnitsTask);

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
            Errors: errors);
    }

    public Task<CompanyLookupResult> CoreOnlyAsync(OrganizationNumber org, CancellationToken ct)
        => company.LookupAsync(org, ct);

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

    private void RecordError(string registryName, Exception ex, List<RegistryError> errors)
    {
        log.LogWarning(ex, "Aggregator provider {Registry} failed", registryName);
        lock (errors)
        {
            errors.Add(new RegistryError(registryName, ex.Message));
        }
    }
}
