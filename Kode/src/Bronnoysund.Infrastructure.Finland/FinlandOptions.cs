// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Finland;

/// <summary>
/// Finland (PRH/YTJ) configuration. PRH's open-data endpoint requires no credentials, so
/// only the BaseUrl and identifying UserAgent are configurable — both ship with sensible
/// defaults that match the live production endpoint. Bind via the config section name
/// <see cref="SectionName"/> (Azure App Config or appsettings).
/// </summary>
public sealed class FinlandOptions
{
    public const string SectionName = "Bronnoysund:International:Finland";

    public string BaseUrl { get; set; } = "https://avoindata.prh.fi/opendata-ytj-api/v3/";

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
