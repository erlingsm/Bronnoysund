// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Poland;

/// <summary>
/// Poland configuration. Covers both the public KRS open API (no auth, juridical persons)
/// and the CEIDG v3 API (Bearer token, sole proprietors). When the CEIDG bearer is not
/// configured, NIP/REGON lookups that would dispatch to CEIDG return Unavailable; KRS
/// lookups always work as long as Justisministeriet keeps the open API up.
/// </summary>
public sealed class PolandOptions
{
    public const string SectionName = "Bronnoysund:International:Poland";

    public string KrsBaseUrl { get; set; } = "https://api-krs.ms.gov.pl/";

    public string CeidgBaseUrl { get; set; } = "https://dane.biznes.gov.pl/";

    public string CeidgBearerToken { get; set; } = string.Empty;

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool CeidgIsConfigured => !string.IsNullOrWhiteSpace(CeidgBearerToken);
}
