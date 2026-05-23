// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Dtos;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Application.UseCases.LookupCompany;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bronnoysund.Lookup.ViewModels;

/// <summary>
/// Delt ViewModel for selskaps-oppslag. Brukes fra både MAUI (Hybrid) og Blazor Web.
/// Bygger på CommunityToolkit.Mvvm sine source generators (ObservableProperty + RelayCommand).
/// </summary>
public sealed partial class CompanyLookupViewModel(LookupCompanyHandler handler) : ObservableObject
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
            ErrorMessage = "Skriv inn et organisasjonsnummer.";
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
                    StatusMessage = "Funnet i Brønnøysundregistrene.";
                    break;
                case CompanyLookupResult.NotFound nf:
                    ErrorMessage = $"Ingen virksomhet med organisasjonsnummer {nf.OrganizationNumber} ble funnet.";
                    break;
                case CompanyLookupResult.InvalidInput inv:
                    ErrorMessage = inv.Message;
                    break;
                case CompanyLookupResult.Unavailable u:
                    ErrorMessage = $"Brønnøysundregistrene er midlertidig utilgjengelig: {u.Message}";
                    break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
