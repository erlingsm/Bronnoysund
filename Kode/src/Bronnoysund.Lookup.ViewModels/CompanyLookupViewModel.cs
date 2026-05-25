// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Dtos;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Application.UseCases.LookupCompany;
using Bronnoysund.Lookup.Application.UseCases.SearchCompaniesByName;
using Bronnoysund.Lookup.ViewModels.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Localization;

namespace Bronnoysund.Lookup.ViewModels;

/// <summary>
/// Shared lookup view-model used by MAUI Blazor Hybrid and Blazor Web. Drives both
/// org-number lookups (single + aggregated multi-registry) and name searches.
/// </summary>
public sealed partial class CompanyLookupViewModel(
    LookupAggregatedCompanyHandler aggregatedHandler,
    SearchCompaniesByNameHandler searchHandler,
    IStringLocalizer<SharedResources> localizer) : ObservableObject
{
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

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<CompanySearchHit> SearchHits { get; set; } = [];

    [ObservableProperty]
    public partial int SearchTotalElements { get; set; }

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
            var result = await aggregatedHandler.HandleAsync(new LookupCompanyQuery(OrgNumberInput), ct);
            switch (result)
            {
                case AggregatedLookupResult.Found f:
                    Found = f.Data.Core;
                    Aggregated = f.Data;
                    StatusMessage = localizer["FoundInRegistry"];
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

    /// <summary>Called from the search-result list when the user clicks a hit. Reuses the standard lookup flow.</summary>
    public async Task SelectHitAsync(CompanySearchHit hit, CancellationToken ct)
    {
        OrgNumberInput = hit.OrganizationNumber;
        await LookupAsync(ct);
    }

    private void ResetTransientState()
    {
        ErrorMessage = null;
        StatusMessage = null;
        Found = null;
        Aggregated = null;
        SearchHits = [];
        SearchTotalElements = 0;
    }
}
