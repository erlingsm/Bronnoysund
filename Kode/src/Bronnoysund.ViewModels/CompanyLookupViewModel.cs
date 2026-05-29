// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Application.UseCases.LookupCompany;
using Bronnoysund.Application.UseCases.SearchCompaniesByName;
using Bronnoysund.Domain;
using Bronnoysund.ViewModels.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Localization;

namespace Bronnoysund.ViewModels;

/// <summary>
/// Shared lookup view-model used by MAUI Blazor Hybrid and Blazor Web. Drives both
/// org-number lookups (single + aggregated multi-registry) and name searches.
/// </summary>
public sealed partial class CompanyLookupViewModel(
    LookupAggregatedCompanyHandler aggregatedHandler,
    SearchCompaniesByNameHandler searchHandler,
    IStringLocalizer<SharedResources> localizer,
    ILegalRolesProvider legalRolesProvider,
    IVoluntaryOrganizationProvider voluntaryProvider,
    IEntityChangesProvider changesProvider) : ObservableObject, IDisposable
{
    /// <summary>
    /// Cancels in-flight enrichment fan-out when the user triggers a new lookup before the
    /// previous one finished. Without this, a quick A→B click sequence could race so that
    /// Hit A's enrichment results overwrite Hit B's VoluntaryOrganization/LegalRoles/Changes.
    /// </summary>
    private CancellationTokenSource? _enrichmentCts;

    [ObservableProperty]
    public partial string OrgNumberInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NameQueryInput { get; set; } = string.Empty;

    /// <summary>True when the user is in name-search mode; false for direct org-number lookup.</summary>
    [ObservableProperty]
    public partial bool IsNameSearchMode { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>Core company payload — the four MVP fields plus the additional open Brreg fields.</summary>
    [ObservableProperty]
    public partial CompanyResponse? Found { get; set; }

    /// <summary>Full aggregated payload including roles, sub-units, bankruptcy, errors per provider.</summary>
    [ObservableProperty]
    public partial AggregatedCompanyResponse? Aggregated { get; set; }

    /// <summary>Frivillighetsregister-treff for the looked-up entity. Null when not registered or before lookup.</summary>
    [ObservableProperty]
    public partial VoluntaryOrganizationResponse? VoluntaryOrganization { get; set; }

    /// <summary>Legal roles the entity holds in other companies. Null before lookup or when none.</summary>
    [ObservableProperty]
    public partial LegalRolesResponse? LegalRoles { get; set; }

    /// <summary>Aggregated change-log for the entity (entity + sub-units + roles). Null before lookup.</summary>
    [ObservableProperty]
    public partial EntityChangesResponse? Changes { get; set; }

    /// <summary>
    /// Per-enrichment-feed errors (Voluntary/LegalRoles Unavailable). Separate from
    /// <see cref="Aggregated"/>.Errors which carries aggregator-fan-out errors.
    /// Changes feed-errors are rendered inline per panel and not duplicated here.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<RegistryError> EnrichmentErrors { get; set; } = [];

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<CompanySearchHit> SearchHits { get; set; } = [];

    [ObservableProperty]
    public partial int SearchTotalElements { get; set; }

    /// <summary>
    /// When the user has explicitly picked a country in the Lookup picker (Plan 26 B2) this
    /// holds the ISO 3166-1 alpha-2 code. Null means "auto" — let <c>ICountryDetector</c> route
    /// from the raw input. Persists across lookups within the same session so the picker
    /// label stays in sync with the selection.
    /// </summary>
    [ObservableProperty]
    public partial string? SelectedCountryHint { get; set; }

    private string EffectiveOrgNumberInput() =>
        Application.International.CountryRouting.ApplyPrefix(OrgNumberInput, SelectedCountryHint);

    [RelayCommand]
    public async Task LookupAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(OrgNumberInput))
        {
            ErrorMessage = localizer["EnterOrgNumber"];
            return;
        }

        IsBusy = true;
        ResetTransientState();

        try
        {
            var result = await aggregatedHandler.HandleAsync(new LookupCompanyQuery(EffectiveOrgNumberInput()), ct);
            switch (result)
            {
                case AggregatedLookupResult.Found f:
                    Found = f.Data.Core;
                    Aggregated = f.Data;
                    StatusMessage = localizer["FoundInRegistry"];
                    _enrichmentCts = new CancellationTokenSource();
                    using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _enrichmentCts.Token))
                    {
                        await LoadEnrichmentAsync(f.Data.Core.OrganizationNumber, linked.Token);
                    }
                    break;
                case AggregatedLookupResult.NotFound nf:
                    ErrorMessage = localizer["NotFoundForOrgNumber", nf.OrganizationNumber];
                    break;
                case AggregatedLookupResult.InvalidInput inv:
                    ErrorMessage = inv.Message;
                    break;
                case AggregatedLookupResult.Unavailable u:
                    ErrorMessage = localizer["RegistryUnavailable", u.Message];
                    break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchByNameAsync(CancellationToken ct)
    {
        IsBusy = true;
        ResetTransientState();

        try
        {
            var result = await searchHandler.HandleAsync(new SearchCompaniesByNameQuery(NameQueryInput), ct);
            switch (result)
            {
                case SearchCompaniesByNameResult.Found f:
                    SearchHits = f.Result.Hits;
                    SearchTotalElements = f.Result.TotalElements;
                    if (SearchHits.Count == 0)
                    {
                        ErrorMessage = localizer["NoHitsForName"];
                    }
                    break;
                case SearchCompaniesByNameResult.InvalidInput inv:
                    ErrorMessage = inv.Message;
                    break;
                case SearchCompaniesByNameResult.Unavailable u:
                    ErrorMessage = localizer["RegistryUnavailable", u.Message];
                    break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Called from the search-result list when the user clicks a hit. Reuses the standard
    /// lookup flow but preserves the hit list across the call so the master-detail layout
    /// (hits on the left, selected company on the right) keeps both panes visible. Without
    /// this stash-and-restore, LookupAsync's ResetTransientState would clear SearchHits.
    /// </summary>
    public async Task SelectHitAsync(CompanySearchHit hit, CancellationToken ct)
    {
        var preservedHits = SearchHits;
        var preservedTotal = SearchTotalElements;

        OrgNumberInput = hit.OrganizationNumber;
        await LookupAsync(ct);

        SearchHits = preservedHits;
        SearchTotalElements = preservedTotal;
    }

    private void ResetTransientState()
    {
        // Cancel any in-flight enrichment fan-out so its results cannot overwrite the
        // next lookup's state. Dispose + null so the next LoadEnrichmentAsync allocates fresh.
        if (_enrichmentCts is not null)
        {
            try { _enrichmentCts.Cancel(); }
            catch (ObjectDisposedException) { /* race with Dispose() — safe to ignore */ }
            _enrichmentCts.Dispose();
            _enrichmentCts = null;
        }

        ErrorMessage = null;
        StatusMessage = null;
        Found = null;
        Aggregated = null;
        VoluntaryOrganization = null;
        LegalRoles = null;
        Changes = null;
        EnrichmentErrors = [];
        SearchHits = [];
        SearchTotalElements = 0;
    }

    /// <summary>
    /// Fan-out the three additional per-entity endpoints (Frivillighetsregister, legal-roles,
    /// change-log) after a successful core lookup. NotFound/NotRegistered are silenced as
    /// "no badge / no table"; Unavailable surfaces in <see cref="EnrichmentErrors"/> so the
    /// UI can render a warning. Changes carries per-feed errors inside the response itself
    /// and is rendered inline by the Lookup page — not duplicated to EnrichmentErrors.
    /// </summary>
    private async Task LoadEnrichmentAsync(string orgnr, CancellationToken ct)
    {
        if (!OrganizationNumber.TryCreate(orgnr, out var org, out _))
        {
            return;
        }

        var voluntaryTask = voluntaryProvider.LookupAsync(org, ct);
        var legalTask = legalRolesProvider.GetLegalRolesAsync(org, ct);
        var changesTask = changesProvider.GetChangesAsync(org, pageSize: 20, ct);

        await Task.WhenAll(voluntaryTask, legalTask, changesTask).ConfigureAwait(false);

        var errors = new List<RegistryError>();

        switch (voluntaryTask.Result)
        {
            case VoluntaryOrganizationLookupResult.Found vf:
                VoluntaryOrganization = vf.Organization;
                break;
            case VoluntaryOrganizationLookupResult.Unavailable vu:
                VoluntaryOrganization = null;
                errors.Add(new RegistryError("Frivillighetsregisteret", vu.Message));
                break;
            default:
                VoluntaryOrganization = null;
                break;
        }

        switch (legalTask.Result)
        {
            case LegalRolesLookupResult.Found lf:
                LegalRoles = lf.Roles;
                break;
            case LegalRolesLookupResult.Unavailable lu:
                LegalRoles = null;
                errors.Add(new RegistryError("LegalRoles", lu.Message));
                break;
            default:
                LegalRoles = null;
                break;
        }

        Changes = changesTask.Result;
        EnrichmentErrors = errors;
    }

    public void Dispose()
    {
        _enrichmentCts?.Cancel();
        _enrichmentCts?.Dispose();
        _enrichmentCts = null;
    }
}
