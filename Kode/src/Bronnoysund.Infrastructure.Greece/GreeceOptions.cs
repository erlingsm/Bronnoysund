// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Greece;

public sealed class GreeceOptions
{
    public const string SectionName = "Bronnoysund:International:Greece";
    public string BaseUrl { get; set; } = "https://opendata-api.businessportal.gr/opendata/";
    public string ApiKey { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
