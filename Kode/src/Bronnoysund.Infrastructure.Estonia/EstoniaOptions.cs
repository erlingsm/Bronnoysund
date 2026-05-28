// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Estonia;

/// <summary>
/// Estonia (e-Äriregister / RIK) XML-API configuration. The lihtandmed_v2 SOAP-style
/// service is free but requires a "requisites contract" signed with RIK; once approved
/// they issue username + password injected into each request body. Until those are
/// configured the provider returns Unavailable with a descriptive reason rather than
/// crashing the host.
/// </summary>
public sealed class EstoniaOptions
{
    public const string SectionName = "Bronnoysund:International:Estonia";

    public string BaseUrl { get; set; } = "https://ariregxmlv6.rik.ee/";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
