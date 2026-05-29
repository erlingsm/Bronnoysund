// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Latvia;

/// <summary>
/// Stub <see cref="ICompanyProvider"/> for Latvia. The free path requires a daily
/// CSV-dump importer + local store that is intentionally out of scope until a Latvian
/// user-base materialises. The wiring is in place so adding the importer later does not
/// touch the composition root — flip <see cref="LatviaOptions.LocalIndexPath"/> in App
/// Config and replace this stub with a real provider that reads from that path.
/// </summary>
internal sealed class UrCompanyProvider(
    IOptions<LatviaOptions> options,
    ILogger<UrCompanyProvider> logger) : ICompanyProvider
{
    public string CountryCode => "LV";

    public bool IsConfigured => options.Value.IsConfigured;

    public Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not LatvianRegistrationNumber lv)
        {
            return Task.FromResult<CompanyLookupResult>(new CompanyLookupResult.InvalidInput(
                $"UrCompanyProvider only accepts Latvian registration numbers (got {id.CountryCode}:{id.Value})."));
        }

        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            logger.LogInformation("Latvia lookup for {Number} returned Unavailable — no local index configured", lv.Value);
            return Task.FromResult<CompanyLookupResult>(new CompanyLookupResult.Unavailable(
                "Latvia (Uzņēmumu reģistrs) requires a bulk CSV-import pipeline. Set Bronnoysund:International:Latvia:LocalIndexPath to a populated index, or upgrade to Lursoft Spor C for live data."));
        }

        // Local-index lookup placeholder — implementation deferred per Plan 21/Latvia.md.
        return Task.FromResult<CompanyLookupResult>(new CompanyLookupResult.Unavailable(
            "Latvia local-index lookup not implemented yet — see Plan 21/Latvia.md for the importer design."));
    }
}
