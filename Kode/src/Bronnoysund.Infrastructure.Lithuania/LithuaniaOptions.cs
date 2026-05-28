// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Lithuania;

/// <summary>
/// Lithuania (data.gov.lt JAR dataset) configuration. data.gov.lt exposes a public CKAN-
/// style API over the monthly JAR dump; <see cref="JarDatasetId"/> defaults to the live
/// dataset slug verified 2026-05-28. A future Spor C live API would add an
/// <c>ApiKey</c> field once a contract with VĮ Registrų centras is in place.
/// </summary>
public sealed class LithuaniaOptions
{
    public const string SectionName = "Bronnoysund:International:Lithuania";

    public string BaseUrl { get; set; } = "https://get.data.gov.lt/";

    public string JarDatasetId { get; set; } =
        "lietuvos-respublikos-juridiniu-asmenu-registre-iregistruoti-juridiniai-asmenys";

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
