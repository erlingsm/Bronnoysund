// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;

namespace Bronnoysund.Infrastructure.OpenCorporates;

/// <summary>
/// Spain (RMC) via OpenCorporates. Spain's native registers are closed gates; OpenCorporates
/// provides a freemium fasade that is the realistic v1 option (see Plan 21/Spain.md).
/// </summary>
internal sealed class SpainCompanyProvider(OpenCorporatesClient oc) : ICompanyProvider
{
    public string CountryCode => "ES";

    public bool IsConfigured => oc.IsConfigured;

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not SpanishNif es)
        {
            return new CompanyLookupResult.InvalidInput(
                $"SpainCompanyProvider only accepts Spanish NIF (got {id.CountryCode}:{id.Value}).");
        }
        return await oc.LookupAsync("es", es.Value, "ES", ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Italy (Registro Imprese / InfoCamere) via OpenCorporates. Telemaco direct access is
/// pay-per-call and bureaucratically heavy; OpenCorporates is the practical fasade.
/// </summary>
internal sealed class ItalyCompanyProvider(OpenCorporatesClient oc) : ICompanyProvider
{
    public string CountryCode => "IT";

    public bool IsConfigured => oc.IsConfigured;

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not ItalianFiscalCode it)
        {
            return new CompanyLookupResult.InvalidInput(
                $"ItalyCompanyProvider only accepts Italian P.IVA (got {id.CountryCode}:{id.Value}).");
        }
        return await oc.LookupAsync("it", it.Value, "IT", ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Serbia (APR) via OpenCorporates. Serbia is not in the EU Open Data scheme; APR's
/// own web-service requires a signed contract + qualified e-signature. OpenCorporates is
/// the only zero-friction option until that contract is in place.
/// </summary>
internal sealed class SerbiaCompanyProvider(OpenCorporatesClient oc) : ICompanyProvider
{
    public string CountryCode => "RS";

    public bool IsConfigured => oc.IsConfigured;

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        var (idValue, ok) = id switch
        {
            SerbianMaticniBroj mb => (mb.Value, true),
            SerbianPib pib => (pib.Value, true),
            _ => (string.Empty, false),
        };
        if (!ok)
        {
            return new CompanyLookupResult.InvalidInput(
                $"SerbiaCompanyProvider only accepts Serbian matični broj or PIB (got {id.CountryCode}:{id.Value}).");
        }
        return await oc.LookupAsync("rs", idValue, "RS", ct).ConfigureAwait(false);
    }
}
