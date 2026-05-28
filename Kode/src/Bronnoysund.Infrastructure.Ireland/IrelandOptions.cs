// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Ireland;

/// <summary>
/// Ireland (CRO via opendata.cro.ie CKAN datastore) configuration. The default
/// <see cref="CompaniesResourceId"/> targets the live "Company Records" CSV resource
/// verified 2026-05-28 — CKAN keeps resource IDs stable across data refreshes, but if CRO
/// publishes a new resource we can override here without code changes. No credentials
/// required for Spor A; a future Spor B (live REST with email_address:api_key) would add
/// <c>LiveApiEmail</c> and <c>LiveApiKey</c> fields.
/// </summary>
public sealed class IrelandOptions
{
    public const string SectionName = "Bronnoysund:International:Ireland";

    public string BaseUrl { get; set; } = "https://opendata.cro.ie/";

    public string CompaniesResourceId { get; set; } = "3fef41bc-b8f4-4b10-8434-ce51c29b1bba";

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
