// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.OpenCorporates;

/// <summary>
/// OpenCorporates API configuration. One commercial API token unlocks coverage of all
/// supported jurisdictions (Spain/Italy/Serbia for Plan 21 Bølge 4 today). Free tier is
/// 500 calls/month; paid tiers start at $99/month.
/// </summary>
public sealed class OpenCorporatesOptions
{
    public const string SectionName = "Bronnoysund:International:OpenCorporates";

    public string BaseUrl { get; set; } = "https://api.opencorporates.com/";

    public string ApiToken { get; set; } = string.Empty;

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiToken);
}
