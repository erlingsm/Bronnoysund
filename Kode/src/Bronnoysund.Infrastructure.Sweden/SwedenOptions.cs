// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Sweden;

/// <summary>
/// Sweden (Bolagsverket "Värdefulla datamängder" REST API) configuration. The endpoint is
/// free but credentials are issued after a kundanmälan (~1-3 days). OAuth2
/// client-credentials flow exchanges <see cref="ClientId"/> + <see cref="ClientSecret"/>
/// for a Bearer token cached in-process until 60 s before expiry.
/// </summary>
public sealed class SwedenOptions
{
    public const string SectionName = "Bronnoysund:International:Sweden";

    public string BaseUrl { get; set; } = "https://api.bolagsverket.se/";

    public string TokenUrl { get; set; } = "https://portal.api.bolagsverket.se/oauth2/token";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
