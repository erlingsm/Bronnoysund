// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Croatia;

public sealed class CroatiaOptions
{
    public const string SectionName = "Bronnoysund:International:Croatia";
    public string BaseUrl { get; set; } = "https://sudreg-data.gov.hr/";
    public string TokenUrl { get; set; } = "https://sudreg-data.gov.hr/api/oauth/token";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
