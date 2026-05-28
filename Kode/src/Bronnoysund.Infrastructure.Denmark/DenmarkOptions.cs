// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Denmark;

/// <summary>
/// Denmark (CVR via virk.dk Elasticsearch endpoint) configuration. Erhvervsstyrelsen
/// issues username + password after an e-mail signup with ~3 weeks lead time (the
/// agreement is 3-year, auto-renewing). Authenticated with HTTP Basic to a public
/// Elasticsearch 6.x cluster.
/// </summary>
public sealed class DenmarkOptions
{
    public const string SectionName = "Bronnoysund:International:Denmark";

    // HTTPS by default — CVR's distribution endpoint accepts both http and https since
    // 2024, and sending Basic Auth credentials over plaintext (the original http://
    // default) would expose username + password on any intermediate hop. Override in
    // App Config only if Erhvervsstyrelsen rolls back TLS support.
    public string BaseUrl { get; set; } = "https://distribution.virk.dk/";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
