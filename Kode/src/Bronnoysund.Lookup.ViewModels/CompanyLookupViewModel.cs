// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Dtos;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Application.UseCases.LookupCompany;
using Bronnoysund.Lookup.ViewModels.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Localization;

namespace Bronnoysund.Lookup.ViewModels;

/// <summary>Shared lookup view-model used by MAUI Blazor Hybrid and Blazor Web.</summary>
public sealed partial class CompanyLookupViewModel(
    LookupCompanyHandler handler,
    IStringLocalizer<SharedResources> localizer) : ObservableObject
{
    [ObservableProperty]
    public partial string OrgNumberInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial CompanyResponse? Found { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [RelayCommand]
    public async Task LookupAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(OrgNumberInput))
        {
            ErrorMessage = localizer["EnterOrgNumber"];
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = null;
        Found = null;

        try
        {
            var result = await handler.HandleAsync(new LookupCompanyQuery(OrgNumberInput), ct);
            switch (result)
            {
                case CompanyLookupResult.Found f:
                    Found = f.Company;
                    StatusMessage = localizer["FoundInRegistry"];
                    break;
                case CompanyLookupResult.NotFound nf:
                    ErrorMessage = localizer["NotFoundForOrgNumber", nf.OrganizationNumber];
                    break;
                case CompanyLookupResult.InvalidInput inv:
                    ErrorMessage = inv.Message;
                    break;
                case CompanyLookupResult.Unavailable u:
                    ErrorMessage = localizer["RegistryUnavailable", u.Message];
                    break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
