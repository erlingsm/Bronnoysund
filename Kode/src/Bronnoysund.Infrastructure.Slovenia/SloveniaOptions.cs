// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Slovenia;

/// <summary>
/// Slovenia (AJPES restPrsInfo) configuration. Credentials are not a header but injected
/// inside each request body under an <c>ident</c> object (proprietary AJPES scheme). The
/// service is VTA-prepaid — each lookup deducts EUR-denominated points from the account.
/// </summary>
public sealed class SloveniaOptions
{
    public const string SectionName = "Bronnoysund:International:Slovenia";

    public string BaseUrl { get; set; } = "https://wwwa.ajpes.si/restPrsInfo/";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Data tier: minimal / narrow / extended / protected. Higher tier = more
    /// fields + higher VTA cost.</summary>
    public string Tier { get; set; } = "extended";

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
